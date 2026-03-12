using System;
using System.IO;
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

        // ── Discos lógicos (todos los volúmenes fijos montados) ───────
        private static void RecopilarDiscosLogicos(DatosServidor reporte)
        {
            reporte.DiscosLogicos.Clear();
            foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
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
        private static void RecopilarAntivirus(DatosServidor reporte)
        {
            reporte.AntivirusNombre = "Windows Defender (Predeterminado)";
            reporte.AntivirusEstado = "Activo";
            reporte.AntivirusRuta = "";
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\SecurityCenter2",
                    "SELECT displayName, productState, pathToSignedProductExe FROM AntivirusProduct");
                foreach (ManagementObject av in s.Get())
                    using (av)
                    {
                        reporte.AntivirusNombre = av["displayName"]?.ToString() ?? reporte.AntivirusNombre;
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
                        catch { reporte.AntivirusEstado = "Monitorizando"; }
                        break; // primer AV registrado
                    }
            }
            catch { /* Windows Server no tiene SecurityCenter2 — se queda con Defender por defecto */ }
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
        // MSFT_WBSummary contiene directamente la fecha del último backup
        // exitoso y su código de resultado. Es más fiable que MSFT_WBJob,
        // que solo representa trabajos activos/programados y suele estar vacío.
        private static void RecopilarEstadoBackup(DatosServidor reporte)
        {
            reporte.EstadoBackup = "No configurado";
            reporte.FechaUltimoBackup = "--/--/----";
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\Microsoft\\Windows\\Backup",
                    "SELECT LastSuccessfulBackupTime, LastBackupResultHR FROM MSFT_WBSummary");
                foreach (ManagementObject summary in s.Get())
                    using (summary)
                    {
                        try
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
                                // El servicio existe pero nunca se ha ejecutado
                                reporte.EstadoBackup = "Configurado — Sin backups previos";
                            }
                        }
                        catch { }
                        break;
                    }
            }
            catch { /* El namespace no existe si Windows Backup no está habilitado */ }
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