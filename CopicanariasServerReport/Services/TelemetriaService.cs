using System.Diagnostics;
using System.Management;
using System.Text.Json;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace CopicanariasServerReport.Services
{
    public static class TelemetriaService
    {
        // Mantenemos el parámetro log opcional para no romper Form1.cs
        public static void RecopilarTelemetria(DatosServidor reporte, Action<string> log = null)
        {
            RecopilarSistemaOperativo(reporte);
            RecopilarUsuarioActivo(reporte);
            RecopilarRAM(reporte);
            RecopilarDiscosLogicos(reporte);
            RecopilarAntivirus(reporte);
            RecopilarRed(reporte);
            RecopilarUnidadesRed(reporte);
            RecopilarDrivers(reporte);

            // Solo perdemos tiempo abriendo el cmd y buscando backups si es técnico DF
            if (reporte.EsTecnicoDf)
            {
                RecopilarEstadoBackup(reporte);
            }
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

        // ── Usuario de la sesión real (Ignorando permisos de Administrador) ──
        private static void RecopilarUsuarioActivo(DatosServidor reporte)
        {
            try
            {
                // Le preguntamos a Windows quién está en la sesión física del monitor
                using var s = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in s.Get())
                    using (obj)
                    {
                        string fullUser = obj["UserName"]?.ToString();
                        if (!string.IsNullOrEmpty(fullUser))
                        {
                            // Suele venir como "NOMBREEQUIPO\Alejandro", lo partimos para quedarnos solo con "Alejandro"
                            var partes = fullUser.Split('\\');
                            reporte.UsuarioActivo = partes.Length > 1 ? partes[1] : fullUser;
                            return;
                        }
                    }
            }
            catch { }

            // Si por algún motivo de seguridad Windows bloquea la consulta, usamos el plan B (mostramos el administrador que ejecuta el programa, aunque no sea el usuario real)
            reporte.UsuarioActivo = Environment.UserName;
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
        private static void RecopilarAntivirus(DatosServidor reporte)
        {
            reporte.AntivirusNombre = "";
            reporte.AntivirusEstado = "";
            reporte.AntivirusRuta = "";
            bool encontrado = false;

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
                            reporte.AntivirusEstado = activo ? (alDia ? "Activo y actualizado" : "Activo — Desactualizado") : "Deshabilitado ⚠️";
                        }
                        catch { reporte.AntivirusEstado = "Activo (estado no determinado)"; }
                        encontrado = true;
                        break;
                    }
            }
            catch { }

            if (!encontrado)
            {
                encontrado = BuscarAvTerceros(reporte);
            }

            if (!encontrado)
            {
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "root\\Microsoft\\Windows\\Defender",
                        "SELECT AMProductVersion, AMRunningMode, AntivirusEnabled, AntivirusSignatureAge FROM MSFT_MpComputerStatus");
                    foreach (ManagementObject mp in s.Get())
                        using (mp)
                        {
                            string version = mp["AMProductVersion"]?.ToString() ?? "";
                            string modo = mp["AMRunningMode"]?.ToString() ?? "";
                            bool avActivo = false;
                            try { avActivo = Convert.ToBoolean(mp["AntivirusEnabled"] ?? false); } catch { }

                            reporte.AntivirusNombre = $"Windows Defender (v{version})";
                            if (!avActivo || modo.Contains("Passive"))
                                reporte.AntivirusEstado = "Pasivo / Deshabilitado ⚠️";
                            else
                                reporte.AntivirusEstado = "Activo y actualizado";

                            encontrado = true;
                            break;
                        }
                }
                catch { }
            }

            if (!encontrado)
            {
                reporte.AntivirusNombre = "No detectado";
                reporte.AntivirusEstado = "No fue posible consultar el estado del antivirus";
            }
        }

        private static bool BuscarAvTerceros(DatosServidor reporte)
        {
            string[] keywords = { "antivirus", "endpoint", "security", "antimalware",
                                  "panda", "eset", "kaspersky", "sophos", "bitdefender",
                                  "symantec", "mcafee", "trellix", "sentinel", "crowdstrike", "malwarebytes" };
            string[] uninstallPaths = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

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
                        if (displayName.IndexOf("Windows Defender", StringComparison.OrdinalIgnoreCase) >= 0) continue;

                        string lower = displayName.ToLower();
                        if (keywords.Any(kw => lower.Contains(kw)))
                        {
                            string ver = app.GetValue("DisplayVersion")?.ToString() ?? "";
                            reporte.AntivirusNombre = string.IsNullOrEmpty(ver) ? displayName : $"{displayName} (v{ver})";
                            reporte.AntivirusEstado = "Instalado (detectado en sistema)";
                            return true;
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
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        private static void RecopilarUnidadesRed(DatosServidor reporte)
        {
            reporte.UnidadesRed.Clear();
            try
            {
                using var usersKey = Registry.Users;
                foreach (string sid in usersKey.GetSubKeyNames())
                {
                    if (sid.EndsWith("_Classes") || sid.Length < 15) continue;
                    string networkPath = $@"{sid}\Network";
                    using var networkKey = usersKey.OpenSubKey(networkPath);
                    if (networkKey != null)
                    {
                        foreach (string letra in networkKey.GetSubKeyNames())
                        {
                            using var driveKey = networkKey.OpenSubKey(letra);
                            if (driveKey != null)
                            {
                                string rutaRemota = driveKey.GetValue("RemotePath")?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(rutaRemota))
                                {
                                    string letraMayuscula = letra.ToUpper() + ":";
                                    if (!reporte.UnidadesRed.Any(u => u.Letra == letraMayuscula && u.Ruta == rutaRemota))
                                    {
                                        var infoRed = new UnidadRedInfo { Letra = letraMayuscula, Ruta = rutaRemota };
                                        if (GetDiskFreeSpaceEx(rutaRemota, out ulong freeBytesAvail, out ulong totalBytes, out ulong totalFreeBytes))
                                        {
                                            infoRed.TotalGB = totalBytes / 1073741824.0;
                                            infoRed.LibreGB = freeBytesAvail / 1073741824.0;
                                            if (infoRed.TotalGB > 0)
                                            {
                                                infoRed.PorcentajeLibre = (infoRed.LibreGB / infoRed.TotalGB) * 100;
                                                int barrasLlenas = Math.Max(0, Math.Min(10, (int)Math.Round((100 - infoRed.PorcentajeLibre) / 10.0)));
                                                infoRed.UsoVisual = $"[{new string('█', barrasLlenas)}{new string('░', 10 - barrasLlenas)}]";
                                            }
                                        }
                                        else { infoRed.UsoVisual = "[No accesible]"; }
                                        reporte.UnidadesRed.Add(infoRed);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        private static void RecopilarDrivers(DatosServidor reporte)
        {
            reporte.Drivers.Clear();
            reporte.Drivers.AddRange(DriverService.Escanear());
        }

        // ── Estado de Backup de Windows ──────────────────────────────
        private static void RecopilarEstadoBackup(DatosServidor reporte)
        {
            reporte.EstadoBackup = "No configurado";
            reporte.FechaUltimoBackup = "--/--/----";

            try
            {
                string cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

                // Obligar a usar el CMD nativo de 64 bits si estamos en una app de 32 bits
                if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
                {
                    cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "sysnative", "cmd.exe");
                }

                var psi = new ProcessStartInfo
                {
                    FileName = cmdPath,
                    Arguments = "/c wbadmin get versions",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var proc = Process.Start(psi);
                string output = proc.StandardOutput.ReadToEnd();
                proc.WaitForExit();

                string ultimaFecha = "";
                string[] lineas = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string linea in lineas)
                {
                    string lower = linea.ToLower();

                    // FIX: Evitamos leer la línea del "Destino de copia de seguridad"
                    if (lower.Contains("backup time:") || lower.Contains("hora de copia") || lower.Contains("hora de la copia"))
                    {
                        var partes = linea.Split(new[] { ':' }, 2);
                        if (partes.Length == 2)
                        {
                            ultimaFecha = partes[1].Trim();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(ultimaFecha))
                {
                    reporte.FechaUltimoBackup = ultimaFecha;
                    reporte.EstadoBackup = "OK (vía wbadmin)";
                    return;
                }
            }
            catch { }

            // ── Métodos de respaldo originales ──
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
                            reporte.FechaUltimoBackup = ManagementDateTimeConverter.ToDateTime(fecha).ToString("dd/MM/yyyy HH:mm");
                            uint hr = 0;
                            try { hr = Convert.ToUInt32(summary["LastBackupResultHR"] ?? 0u); } catch { }
                            reporte.EstadoBackup = hr == 0 ? "OK" : $"Error (0x{hr:X8})";
                        }
                        return;
                    }
            }
            catch { }

            try
            {
                using var baseKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\WindowsBackup\ActionHistory");
                if (baseKey != null)
                {
                    DateTime mejorFecha = DateTime.MinValue;
                    foreach (string subNombre in baseKey.GetSubKeyNames())
                    {
                        using var sub = baseKey.OpenSubKey(subNombre);
                        byte[] ftBytes = sub?.GetValue("ActionItemDate") as byte[];
                        if (ftBytes != null && ftBytes.Length == 8)
                        {
                            DateTime dt = DateTime.FromFileTime(BitConverter.ToInt64(ftBytes, 0));
                            if (dt > mejorFecha) mejorFecha = dt;
                        }
                    }
                    if (mejorFecha > DateTime.MinValue)
                    {
                        reporte.FechaUltimoBackup = mejorFecha.ToString("dd/MM/yyyy HH:mm");
                        reporte.EstadoBackup = "OK";
                    }
                }
            }
            catch { }
        }

        // ── Java Desktop (JRE 8): detección local + consulta online en java.com ───
        public static async Task RecopilarJavaAsync(DatosServidor reporte, Action<string> log, HttpClient http)
        {
            reporte.VersionJava = "No instalado / No detectado";
            reporte.JavaAlDia = false;
            reporte.JavaVersionOnline = "";
            string versionInstalada = "";
            int updateLocal = 0;

            string[] registryPaths = {
                @"SOFTWARE\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment"
            };

            // 1. Buscamos en el registro las subclaves que tengan el formato "1.8.0_XXX"
            foreach (var keyPath in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;

                    foreach (string subName in key.GetSubKeyNames())
                    {
                        var match = Regex.Match(subName, @"1\.8\.0_(\d+)");
                        if (match.Success)
                        {
                            int updateNum = int.Parse(match.Groups[1].Value);
                            // Nos quedamos siempre con el Update más alto por si hay restos viejos
                            if (updateNum > updateLocal)
                            {
                                updateLocal = updateNum;
                                versionInstalada = subName; // Ej: 1.8.0_481
                            }
                        }
                    }
                }
                catch { }
            }

            if (updateLocal == 0)
            {
                log?.Invoke("    · Java: No detectado JRE 8 en el registro.\n");
                return;
            }

            reporte.VersionJava = $"Java 8 Update {updateLocal}";

            // 2. Consultar online leyendo directamente la página oficial de Java
            try
            {
                log?.Invoke("    · Java: Consultando última versión en java.com...\n");

                // Descargamos el código fuente de la página de descargas de java.com
                string html = await http.GetStringAsync("https://www.java.com/es/download/");

                // Buscamos el texto literal "Versión 8 Update XXX" o "Version 8 Update XXX"
                var matchOnline = Regex.Match(html, @"Versi(?:ó|o)n 8 Update (\d+)");

                if (matchOnline.Success)
                {
                    int updateOnline = int.Parse(matchOnline.Groups[1].Value);
                    reporte.JavaVersionOnline = $"Java 8 Update {updateOnline} (Fuente: java.com)";

                    if (updateLocal >= updateOnline)
                    {
                        reporte.JavaAlDia = true;
                        reporte.VersionJava += " ✅";
                    }
                    else
                    {
                        reporte.JavaAlDia = false;
                        reporte.VersionJava += $" ⚠️ (disponible Update {updateOnline})";
                    }

                    log?.Invoke($"    · Java instalado: Update {updateLocal} | Disponible en web: Update {updateOnline}\n");
                }
                else
                {
                    reporte.JavaVersionOnline = "No se pudo encontrar la versión en java.com";
                    log?.Invoke("    · Java: Cambio en el diseño de java.com, no se encontró el Update.\n");
                }
            }
            catch
            {
                reporte.JavaVersionOnline = "No se pudo verificar (sin conexión o error de red)";
                log?.Invoke("    · Java: No se pudo consultar versión online.\n");
            }
        }
    }
}