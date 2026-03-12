using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace CopicanariasServerReport.Services
{
    public static class TelemetriaService
    {
        // Recoge toda la telemetría del sistema de forma síncrona.
        // Se debe llamar siempre desde Task.Run para no bloquear la UI.
        public static void RecopilarTelemetria(DatosServidor reporte)
        {
            RecopilarSistemaOperativo(reporte);
            RecopilarRAM(reporte);
            RecopilarDiscosLogicos(reporte);
            RecopilarAntivirus(reporte);
            RecopilarRed(reporte);
            RecopilarUnidadesRed(reporte);
            RecopilarDrivers(reporte);
            RecopilarEstadoBackup(reporte);
        }

        // ── Sistema Operativo ────────────────────────────────────────
        private static void RecopilarSistemaOperativo(DatosServidor reporte)
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject o in s.Get())
                    using (o)
                    {
                        reporte.SistemaOperativo = o["Caption"]?.ToString() ?? Environment.OSVersion.ToString();
                        break;
                    }
            }
            catch { reporte.SistemaOperativo = Environment.OSVersion.ToString(); }
        }

        // ── RAM ──────────────────────────────────────────────────────
        private static void RecopilarRAM(DatosServidor reporte)
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject cs in s.Get())
                    using (cs)
                    {
                        reporte.MemoriaRAM = $"{Math.Round(Convert.ToInt64(cs["TotalPhysicalMemory"]) / (1024.0 * 1024.0 * 1024.0))} GB";
                        break;
                    }
            }
            catch { }
        }

        // ── Discos lógicos (volúmenes fijos + extraíbles montados) ───
        private static void RecopilarDiscosLogicos(DatosServidor reporte)
        {
            reporte.DiscosLogicos.Clear();
            foreach (var d in DriveInfo.GetDrives()
                .Where(x => x.IsReady &&
                           (x.DriveType == DriveType.Fixed ||
                            x.DriveType == DriveType.Removable)))
            {
                double total = d.TotalSize / 1073741824.0;
                double libre = d.AvailableFreeSpace / 1073741824.0;
                reporte.DiscosLogicos.Add(new DiscoLogicoInfo
                {
                    Letra = d.Name,
                    TotalGB = total,
                    LibreGB = libre,
                    PorcentajeLibre = total > 0 ? (libre / total) * 100 : 0
                });
            }
        }

        // ── Antivirus ────────────────────────────────────────────────
        // Estrategia en dos capas:
        //   1) SecurityCenter2       — Windows 10/11 de escritorio
        //   2) MSFT_MpComputerStatus — Windows Defender en Server y Win10/11
        private static void RecopilarAntivirus(DatosServidor reporte)
        {
            reporte.AntivirusNombre = "";
            reporte.AntivirusEstado = "";
            reporte.AntivirusRuta = "";
            bool encontrado = false;

            // ── Capa 1: SecurityCenter2 (solo disponible en ediciones de escritorio) ──
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\SecurityCenter2",
                    "SELECT displayName, productState, pathToSignedProductExe FROM AntivirusProduct");
                foreach (ManagementObject av in s.Get())
                    using (av)
                    {
                        reporte.AntivirusNombre = av["displayName"]?.ToString() ?? "";
                        reporte.AntivirusRuta = av["pathToSignedProductExe"]?.ToString() ?? "";
                        try
                        {
                            uint state = Convert.ToUInt32(av["productState"] ?? 0u);
                            bool activo = ((state >> 12) & 0xF) == 1;
                            bool alDia = ((state >> 4) & 0xF) != 10;
                            reporte.AntivirusEstado = activo
                                ? (alDia ? "Activo y actualizado" : "Activo — Definiciones desactualizadas")
                                : "Deshabilitado ⚠️";
                        }
                        catch { reporte.AntivirusEstado = "Activo (estado no determinado)"; }
                        encontrado = true;
                        break;
                    }
            }
            catch { /* namespace no disponible en Windows Server — continuar */ }

            if (encontrado) return;

            // ── Capa 2: MSFT_MpComputerStatus (Defender en Server y Win10/11) ──
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\Microsoft\\Windows\\Defender",
                    "SELECT AMProductVersion, AMRunningMode, AntivirusEnabled, " +
                    "AntivirusSignatureAge FROM MSFT_MpComputerStatus");
                foreach (ManagementObject mp in s.Get())
                    using (mp)
                    {
                        string version = mp["AMProductVersion"]?.ToString() ?? "";
                        string modo = mp["AMRunningMode"]?.ToString() ?? "";
                        bool avActivo = false;
                        try { avActivo = Convert.ToBoolean(mp["AntivirusEnabled"] ?? false); } catch { }
                        uint sigAge = 0;
                        try { sigAge = Convert.ToUInt32(mp["AntivirusSignatureAge"] ?? 0u); } catch { }

                        reporte.AntivirusNombre = string.IsNullOrEmpty(version)
                            ? "Windows Defender"
                            : $"Windows Defender (v{version})";

                        if (!avActivo)
                            reporte.AntivirusEstado = "Deshabilitado ⚠️";
                        else if (modo == "Passive Mode" || modo == "SxS Passive Mode")
                            reporte.AntivirusEstado = "Pasivo (otro AV activo en el sistema)";
                        else
                            reporte.AntivirusEstado = sigAge > 7
                                ? $"Activo — Definiciones con {sigAge} días de antigüedad ⚠️"
                                : "Activo y actualizado";

                        encontrado = true;
                        break;
                    }
            }
            catch { }

            // ── Capa 3: búsqueda de servicios de AV de terceros (Windows Server sin SecurityCenter2) ──
            if (!encontrado)
                encontrado = BuscarAvTerceros(reporte);

            if (!encontrado)
            {
                reporte.AntivirusNombre = "No detectado";
                reporte.AntivirusEstado = "No fue posible consultar el estado del antivirus";
            }
        }

        // Detecta el AV de terceros mediante:
        //   A) Proveedores AMSI registrados (HKLM\SOFTWARE\Microsoft\AMSI\Providers)
        //      Cualquier AV compatible con Server 2016+ escribe su GUID aqui.
        //      El nombre se resuelve desde HKLM\SOFTWARE\Classes\CLSID\{GUID}.
        //      No requiere lista hardcodeada: detecta cualquier AV registrado.
        //   B) Claves de desinstalacion (fallback para AV sin registro AMSI).
        private static bool BuscarAvTerceros(DatosServidor reporte)
        {
            // ── Capa A: proveedores AMSI ──────────────────────────────────────────
            try
            {
                using var amsiKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\AMSI\Providers");
                if (amsiKey != null)
                {
                    foreach (string guid in amsiKey.GetSubKeyNames())
                    {
                        string nombre = "";
                        try
                        {
                            using var clsid = Registry.LocalMachine.OpenSubKey(
                                @"SOFTWARE\Classes\CLSID\" + guid);
                            nombre = clsid?.GetValue(null)?.ToString() ?? "";
                        }
                        catch { }

                        if (string.IsNullOrEmpty(nombre) ||
                            nombre.IndexOf("Windows Defender", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;

                        reporte.AntivirusNombre = nombre;
                        reporte.AntivirusEstado = "Activo (registrado como proveedor AMSI)";
                        return true;
                    }
                }
            }
            catch { }

            // ── Capa B: claves de desinstalacion (fallback para AV sin AMSI) ──────
            string[] uninstallPaths = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };
            string[] keywords = { "antivirus", "endpoint security", "endpoint protection",
                                   "internet security", "total security", "antimalware" };
            try
            {
                foreach (string path in uninstallPaths)
                {
                    using var uninstall = Registry.LocalMachine.OpenSubKey(path);
                    if (uninstall == null) continue;

                    foreach (string sub in uninstall.GetSubKeyNames())
                    {
                        using var app = uninstall.OpenSubKey(sub);
                        if (app == null) continue;

                        string displayName = app.GetValue("DisplayName")?.ToString() ?? "";
                        if (string.IsNullOrEmpty(displayName)) continue;
                        if (displayName.IndexOf("Windows Defender", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;

                        string lower = displayName.ToLower();
                        foreach (string kw in keywords)
                        {
                            if (lower.Contains(kw))
                            {
                                string ver = app.GetValue("DisplayVersion")?.ToString() ?? "";
                                reporte.AntivirusNombre = string.IsNullOrEmpty(ver)
                                    ? displayName : $"{displayName} (v{ver})";
                                reporte.AntivirusEstado = "Instalado (estado de servicio no determinado)";
                                return true;
                            }
                        }
                    }
                }
            }
            catch { }

            return false;
        }

        // ── Interfaces de red activas ────────────────────────────────
        private static void RecopilarRed(DatosServidor reporte)
        {
            reporte.InterfacesRed.Clear();
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT Name, NetConnectionID, Speed, AdapterType FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                foreach (ManagementObject red in s.Get())
                    using (red)
                    {
                        string nombre = red["Name"]?.ToString() ?? "Desconocido";
                        string connId = red["NetConnectionID"]?.ToString() ?? "";
                        long velBps = 0;
                        try { velBps = Convert.ToInt64(red["Speed"] ?? 0L); } catch { }
                        string velStr = velBps > 0 ? $"{velBps / 1_000_000} Mbps" : "N/A";
                        string tipoAd = red["AdapterType"]?.ToString() ?? "";
                        string tipoStr = (tipoAd.Contains("Wireless") || connId.ToLower().Contains("wi-fi") || connId.ToLower().Contains("wifi"))
                            ? "Wi-Fi" : tipoAd.Contains("Ethernet") ? "Ethernet" : "Otro";

                        reporte.InterfacesRed.Add(new RedInfo
                        {
                            Nombre = nombre,
                            Tipo = tipoStr,
                            Velocidad = velStr,
                            Estado = "Conectado"
                        });
                    }
            }
            catch { }
        }

        // ── Unidades de red mapeadas ─────────────────────────────────
        private static void RecopilarUnidadesRed(DatosServidor reporte)
        {
            reporte.UnidadesRed.Clear();
            try
            {
                foreach (var drive in DriveInfo.GetDrives()
                    .Where(d => d.DriveType == DriveType.Network))
                {
                    // Obtener la ruta UNC mediante la API de Windows
                    string ruta = ObtenerRutaRed(drive.Name.TrimEnd('\\'));
                    reporte.UnidadesRed.Add(new UnidadRedInfo
                    {
                        Letra = drive.Name.TrimEnd('\\'),
                        Ruta = string.IsNullOrEmpty(ruta) ? "(ruta no disponible)" : ruta
                    });
                }
            }
            catch { }
        }

        // Llama a WNetGetConnection para resolver la letra de unidad a ruta UNC.
        // Funciona en cualquier hilo ya que consulta directamente la API de red de Windows.
        private static string ObtenerRutaRed(string letraSinBarra)
        {
            var sb = new System.Text.StringBuilder(300);
            int len = sb.Capacity;
            int ret = WNetGetConnection(letraSinBarra, sb, ref len);
            return ret == 0 ? sb.ToString() : "";
        }

        [System.Runtime.InteropServices.DllImport("mpr.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
        private static extern int WNetGetConnection(string localName, System.Text.StringBuilder remoteName, ref int length);

        // ── Drivers con error (delegado a DriverService) ─────────────
        private static void RecopilarDrivers(DatosServidor reporte)
        {
            reporte.Drivers.Clear();
            reporte.Drivers.AddRange(DriverService.Escanear());
        }

        // ── Estado de Backup de Windows ──────────────────────────────
        // Estrategia en tres capas (primera que devuelva datos gana):
        //   1) MSFT_WBSummary   — Windows Server Backup (feature instalado en Server)
        //   2) Event Log        — Microsoft-Windows-Backup/Operational (Server y Win10/11)
        //                         EventID 4 = completado OK, EventID 5 = error
        //   3) ActionHistory    — Registro backup nativo de Win10/11 desktop (no existe en Server)
        private static void RecopilarEstadoBackup(DatosServidor reporte)
        {
            reporte.EstadoBackup = "No configurado";
            reporte.FechaUltimoBackup = "--/--/----";

            // ── Capa 1: MSFT_WBSummary (feature Windows Server Backup instalado) ──
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\Microsoft\\Windows\\Backup",
                    "SELECT LastSuccessfulBackupTime, LastBackupResultHR FROM MSFT_WBSummary");
                foreach (ManagementObject summary in s.Get())
                    using (summary)
                    {
                        string fecha = summary["LastSuccessfulBackupTime"]?.ToString();
                        if (!string.IsNullOrEmpty(fecha))
                        {
                            DateTime t = ManagementDateTimeConverter.ToDateTime(fecha);
                            reporte.FechaUltimoBackup = t.ToString("dd/MM/yyyy HH:mm");
                            uint hr = 0;
                            try { hr = Convert.ToUInt32(summary["LastBackupResultHR"] ?? 0u); } catch { }
                            reporte.EstadoBackup = hr == 0 ? "OK" : $"Error (0x{hr:X8})";
                        }
                        else
                        {
                            reporte.EstadoBackup = "Configurado — Sin backups previos";
                        }
                        return;
                    }
            }
            catch { /* namespace no existe si el feature no está instalado */ }

            // ── Capa 2: Event Log Microsoft-Windows-Backup/Operational ──────────
            // Funciona en Windows Server Y en Win10/11 sin necesitar el feature WMI.
            // EventID 1 = backup iniciado (ignorado)
            // EventID 4 = backup completado con éxito
            // EventID 5 = backup completado con error
            try
            {
                var query = new EventLogQuery(
                    "Microsoft-Windows-Backup/Operational",
                    PathType.LogName,
                    "*[System[(EventID=4 or EventID=5)]]");

                using var reader = new EventLogReader(query);
                DateTime mejorFecha = DateTime.MinValue;
                bool ultimoFueError = false;

                EventRecord record;
                while ((record = reader.ReadEvent()) != null)
                    using (record)
                    {
                        if (record.TimeCreated.HasValue && record.TimeCreated.Value > mejorFecha)
                        {
                            mejorFecha = record.TimeCreated.Value;
                            ultimoFueError = (record.Id == 5);
                        }
                    }

                if (mejorFecha > DateTime.MinValue)
                {
                    reporte.FechaUltimoBackup = mejorFecha.ToString("dd/MM/yyyy HH:mm");
                    reporte.EstadoBackup = ultimoFueError
                        ? "Error en el último backup ⚠️"
                        : "OK";
                    return;
                }

                // El log existe pero no tiene eventos de resultado → configurado, nunca ejecutado
                reporte.EstadoBackup = "Configurado — Sin backups previos";
                return;
            }
            catch { /* El log no existe si Windows Backup no está habilitado en ninguna forma */ }

            // ── Capa 3: ActionHistory en registro (backup nativo Win10/11 desktop) ──
            // Esta clave NO existe en Windows Server — es el último recurso para equipos de escritorio.
            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WindowsBackup\ActionHistory");
                if (baseKey != null)
                {
                    DateTime mejorFecha = DateTime.MinValue;
                    bool hayError = false;

                    foreach (string subNombre in baseKey.GetSubKeyNames())
                    {
                        using var sub = baseKey.OpenSubKey(subNombre);
                        if (sub == null) continue;

                        byte[] ftBytes = sub.GetValue("ActionItemDate") as byte[];
                        if (ftBytes != null && ftBytes.Length == 8)
                        {
                            long ft = BitConverter.ToInt64(ftBytes, 0);
                            if (ft > 0)
                            {
                                DateTime dt = DateTime.FromFileTime(ft);
                                if (dt > mejorFecha) mejorFecha = dt;
                            }
                        }

                        object resObj = sub.GetValue("ActionResultCode");
                        if (resObj != null)
                            try { if (Convert.ToInt32(resObj) != 0) hayError = true; } catch { }
                    }

                    if (mejorFecha > DateTime.MinValue)
                    {
                        reporte.FechaUltimoBackup = mejorFecha.ToString("dd/MM/yyyy HH:mm");
                        reporte.EstadoBackup = hayError ? "Completado con errores ⚠️" : "OK";
                        return;
                    }

                    reporte.EstadoBackup = "Configurado — Sin backups previos";
                }
            }
            catch { }
        }

        // ── Java: detección local + consulta online (Adoptium API) ───
        public static async Task RecopilarJavaAsync(DatosServidor reporte, Action<string> log, HttpClient http)
        {
            reporte.VersionJava = "No instalado / No detectado";
            reporte.JavaAlDia = false; // false hasta que se confirme lo contrario
            reporte.JavaVersionOnline = "";
            string versionInstalada = "";

            string[] registryPaths =
            {
                @"SOFTWARE\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\JavaSoft\JDK",
                @"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\WOW6432Node\JavaSoft\JDK"
            };

            foreach (var keyPath in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;
                    var ver = key.GetValue("CurrentVersion")?.ToString();
                    if (!string.IsNullOrEmpty(ver))
                    {
                        versionInstalada = ver;
                        reporte.VersionJava = $"Instalado — Versión {ver}";
                        break;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(versionInstalada))
            {
                log("    · Java: No detectado en el registro.\n");
                return;
            }

            // Consulta a la API de Adoptium para la última LTS disponible
            try
            {
                log("    · Java: Consultando última versión LTS disponible online...\n");
                string json = await http.GetStringAsync("https://api.adoptium.net/v3/info/available_releases");
                using var doc = JsonDocument.Parse(json);
                int ltsOnline = doc.RootElement.GetProperty("most_recent_lts").GetInt32();
                reporte.JavaVersionOnline = $"Java {ltsOnline} LTS (Fuente: Adoptium)";

                // Extraer major version instalada (1.8 → 8, 11 → 11, etc.)
                int majorInstalado = 0;
                var partes = versionInstalada.Split('.');
                if (partes[0] == "1" && partes.Length > 1)
                    int.TryParse(partes[1], out majorInstalado);
                else
                    int.TryParse(partes[0], out majorInstalado);

                if (majorInstalado >= ltsOnline)
                {
                    reporte.JavaAlDia = true;
                    reporte.VersionJava += " ✅";
                }
                else
                {
                    reporte.JavaAlDia = false;
                    reporte.VersionJava += $" ⚠️ (disponible Java {ltsOnline} LTS)";
                }

                log($"    · Java instalado: {versionInstalada} | LTS disponible: Java {ltsOnline}\n");
            }
            catch
            {
                reporte.JavaVersionOnline = "No se pudo verificar (sin conexión o error de red)";
                log("    · Java: No se pudo consultar versión online.\n");
            }
        }
    }
}