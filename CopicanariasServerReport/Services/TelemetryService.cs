using Microsoft.Win32;
using System.Diagnostics;
using System.Management;
using System.Text.RegularExpressions;

namespace CopicanariasServerReport.Services
{
    public static class TelemetryService
    {
        // Mantenemos el parámetro log opcional para no romper Form1.cs
        public static void CollectTelemetry(ServerData report, Action<string> log = null)
        {
            log?.Invoke("\n>>> Extrayendo información de Sistema y Seguridad...\n");

            GetOS(report);
            GetActiveUser(report);
            GetRAM(report);
            GetLogicalDisks(report);

            // Antivirus y su Log en tiempo real
            GetAntivirus(report);
            log?.Invoke($"    · Antivirus: {report.AntivirusName} [{report.AntivirusState}]\n");

            GetNetwork(report);
            GetNetworkDrives(report);

            // Solo perdemos tiempo abriendo el cmd y buscando backups si es técnico DF
            if (report.IsDfTechnician)
            {
                GetBackUpState(report);
                log?.Invoke($"    · Backup Local: {report.BackupState} (Última: {report.LastBackupDate})\n");

                
                GetDfServerVersion(report);
                log?.Invoke($"    · DF-Server: Versión detectada {report.DfServer.Version}\n");
            }
        }

        // ── Sistema Operativo ────────────────────────────────────────
        private static void GetOS(ServerData report)
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject o in s.Get())
                    using (o)
                    {
                        report.OS = o["Caption"]?.ToString() ?? Environment.OSVersion.ToString();
                        break;
                    }
            }
            catch { report.OS = Environment.OSVersion.ToString(); }
        }

        // ── Usuario de la sesión real (Ignorando permisos de Administrador) ──
        private static void GetActiveUser(ServerData reporte)
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT UserName FROM Win32_ComputerSystem");
                foreach (ManagementObject obj in s.Get())
                    using (obj)
                    {
                        string fullUser = obj["UserName"]?.ToString();
                        if (!string.IsNullOrEmpty(fullUser))
                        {
                            var partes = fullUser.Split('\\');
                            reporte.ActiveUser = partes.Length > 1 ? partes[1] : fullUser;
                            return;
                        }
                    }
            }
            catch { }

            reporte.ActiveUser = Environment.UserName;
        }

        // ── RAM ──────────────────────────────────────────────────────
        private static void GetRAM(ServerData report)
        {
            try
            {
                using var s = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject cs in s.Get())
                    using (cs)
                    {
                        report.RAM = $"{Math.Round(Convert.ToInt64(cs["TotalPhysicalMemory"]) / (1024.0 * 1024.0 * 1024.0))} GB";
                        break;
                    }
            }
            catch { }
        }

        // ── Discos lógicos (volúmenes fijos + extraíbles montados) ───
        private static void GetLogicalDisks(ServerData report)
        {
            report.LogicDisks.Clear();
            foreach (var d in DriveInfo.GetDrives()
                .Where(x => x.IsReady &&
                           (x.DriveType == DriveType.Fixed ||
                            x.DriveType == DriveType.Removable)))
            {
                double total = d.TotalSize / 1073741824.0;
                double free = d.AvailableFreeSpace / 1073741824.0;
                report.LogicDisks.Add(new LogicDiskInfo
                {
                    Letter = d.Name,
                    TotalGB = total,
                    FreeGB = free,
                    FreePercent = total > 0 ? (free / total) * 100 : 0
                });
            }
        }

        // ── Antivirus (Detección Multinivel) ─────────────────────────
        private static void GetAntivirus(ServerData report)
        {
            report.AntivirusName = "";
            report.AntivirusState = "";
            report.AntivirusPath = "";
            bool isThirdPartyFound = false;

            // --- FASE 1: Buscar antivirus de terceros en SecurityCenter ---
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\SecurityCenter2",
                    "SELECT displayName, productState FROM AntivirusProduct");

                foreach (ManagementObject av in s.Get())
                {
                    using (av)
                    {
                        string name = av["displayName"]?.ToString() ?? "";

                        // Ignoramos a Defender para dar prioridad a los de terceros
                        if (name.IndexOf("Windows Defender", StringComparison.OrdinalIgnoreCase) >= 0)
                            continue;

                        report.AntivirusName = name;
                        try
                        {
                            uint state = Convert.ToUInt32(av["productState"] ?? 0u);
                            bool active = ((state >> 12) & 0xF) == 1;
                            bool upToDate = ((state >> 4) & 0xF) != 10;
                            report.AntivirusState = active ? (upToDate ? "Activo y actualizado" : "Activo — Desactualizado") : "Deshabilitado ⚠️";
                        }
                        catch { report.AntivirusState = "Activo (estado no determinado)"; }

                        isThirdPartyFound = true;
                        break;
                    }
                }
            }
            catch { }

            // --- FASE 2: Nivel Kernel (Drivers FSFilter Anti-Virus) para EDRs ocultos ---
            if (!isThirdPartyFound)
            {
                isThirdPartyFound = SearchThirdPartyAntivirus(report);
            }

            // --- FASE 3: Si no hay NADA de terceros, comprobamos Windows Defender ---
            if (!isThirdPartyFound)
            {
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "root\\Microsoft\\Windows\\Defender",
                        "SELECT AMProductVersion, AMRunningMode, AntivirusEnabled FROM MSFT_MpComputerStatus");

                    bool isWDefenderFound = false;
                    foreach (ManagementObject mp in s.Get())
                    {
                        using (mp)
                        {
                            string version = mp["AMProductVersion"]?.ToString() ?? "";
                            string mode = mp["AMRunningMode"]?.ToString() ?? "";
                            bool avActive = false;
                            try { avActive = Convert.ToBoolean(mp["AntivirusEnabled"] ?? false); } catch { }

                            report.AntivirusName = $"Windows Defender (v{version})";
                            if (!avActive || mode.Contains("Passive"))
                                report.AntivirusState = "Pasivo / Deshabilitado ⚠️";
                            else
                                report.AntivirusState = "Activo y actualizado";

                            isWDefenderFound = true;
                            break;
                        }
                    }

                    if (!isWDefenderFound)
                    {
                        report.AntivirusName = "No detectado";
                        report.AntivirusState = "No fue posible consultar el estado del antivirus";
                    }
                }
                catch
                {
                    report.AntivirusName = "No detectado";
                    report.AntivirusState = "No fue posible consultar el estado del antivirus";
                }
            }
        }

        private static bool SearchThirdPartyAntivirus(ServerData report)
        {
            // --- NIVEL 1: FSFilter Anti-Virus Drivers (EDRs puros con driver de kernel) ---
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT DisplayName, State FROM Win32_SystemDriver WHERE Group = 'FSFilter Anti-Virus'");
                foreach (ManagementObject driver in s.Get())
                    using (driver)
                    {
                        string displayName = driver["DisplayName"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(displayName)) continue;

                        // Ignoramos a Defender para dar prioridad a los de terceros
                        if (displayName.IndexOf("Windows Defender", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            displayName.Equals("WdFilter", StringComparison.OrdinalIgnoreCase)) continue;

                        string cleanName = displayName
                            .Replace(" Mini-Filter Driver", "").Replace(" Minifilter Driver", "")
                            .Replace(" File System Filter", "").Replace(" Filter Driver", "").Trim();

                        report.AntivirusName = $"{cleanName} (Motor Core EDR)";
                        report.AntivirusState = (driver["State"]?.ToString() ?? "")
                            .Equals("Running", StringComparison.OrdinalIgnoreCase)
                            ? "Activo (Driver en ejecución)" : "Instalado (Driver detenido) ⚠️";
                        return true;
                    }
            }
            catch { }

            // --- NIVEL 2: Servicios por DisplayName (cubre agentes userspace como Malwarebytes EA) ---
            string[] edrBrands = { "threatdown", "malwarebytes", "crowdstrike", "sentinel", "sophos",
                                   "cylance", "carbon black", "trellix", "bitdefender", "eset",
                                   "kaspersky", "symantec", "mcafee", "watchguard", "fortinet",
                                   "forticlient", "trend micro", "cybereason", "cortex",
                                   "check point", "cisco", "panda", "avast", "avg", "avira" };

            // LISTA NEGRA: Palabras que delatan que NO es el motor principal del antivirus
            string[] ignore = { "webadvisor", "vpn", "updater", "installer", "safeconnect",
                                 "management agent", "network", "firewall", "identity", "update service" };

            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT DisplayName, State FROM Win32_Service");
                foreach (ManagementObject svc in s.Get())
                    using (svc)
                    {
                        string displayName = svc["DisplayName"]?.ToString() ?? "";
                        if (string.IsNullOrEmpty(displayName)) continue;

                        string lower = displayName.ToLower();

                        // Aplicamos el filtro de exclusiones ANTES de buscar la marca
                        if (ignore.Any(ig => lower.Contains(ig))) continue;

                        if (edrBrands.Any(kw => lower.Contains(kw)))
                        {
                            string state = svc["State"]?.ToString() ?? "";
                            report.AntivirusName = displayName;
                            report.AntivirusState = state.Equals("Running", StringComparison.OrdinalIgnoreCase)
                                ? "Activo (Servicio en ejecución)" : "Instalado (Servicio detenido) ⚠️";
                            return true;
                        }
                    }
            }
            catch { }

            return false;
        }

        // ── Interfaces de red activas ────────────────────────────────
        private static void GetNetwork(ServerData report)
        {
            report.NetworkInterfaces.Clear();
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT Name, NetConnectionID, Speed, AdapterType FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                foreach (ManagementObject red in s.Get())
                    using (red)
                    {
                        string name = red["Name"]?.ToString() ?? "Desconocido";
                        string connId = red["NetConnectionID"]?.ToString() ?? "";
                        long speedBps = 0;
                        try { speedBps = Convert.ToInt64(red["Speed"] ?? 0L); } catch { }
                        string speedStr = speedBps > 0 ? $"{speedBps / 1_000_000} Mbps" : "N/A";
                        string typeAd = red["AdapterType"]?.ToString() ?? "";
                        string typeStr = (typeAd.Contains("Wireless") || connId.ToLower().Contains("wi-fi") || connId.ToLower().Contains("wifi"))
                            ? "Wi-Fi" : typeAd.Contains("Ethernet") ? "Ethernet" : "Otro";

                        report.NetworkInterfaces.Add(new NetworkInterfaceInfo
                        {
                            Name = name,
                            Type = typeStr,
                            Speed = speedStr,
                            State = "Conectado"
                        });
                    }
            }
            catch { }
        }

        // ── Unidades de red mapeadas ─────────────────────────────────
        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        [return: System.Runtime.InteropServices.MarshalAs(System.Runtime.InteropServices.UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        private static void GetNetworkDrives(ServerData report)
        {
            report.NetworkDrives.Clear();
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
                        foreach (string letter in networkKey.GetSubKeyNames())
                        {
                            using var driveKey = networkKey.OpenSubKey(letter);
                            if (driveKey != null)
                            {
                                string remotePath = driveKey.GetValue("RemotePath")?.ToString() ?? "";
                                if (!string.IsNullOrEmpty(remotePath))
                                {
                                    string caseLetter = letter.ToUpper() + ":";
                                    if (!report.NetworkDrives.Any(u => u.Letter == caseLetter && u.Path == remotePath))
                                    {
                                        var networkInfo = new NetworkDrivesInfo { Letter = caseLetter, Path = remotePath };
                                        if (GetDiskFreeSpaceEx(remotePath, out ulong freeBytesAvail, out ulong totalBytes, out ulong totalFreeBytes))
                                        {
                                            networkInfo.TotalGB = totalBytes / 1073741824.0;
                                            networkInfo.FreeGB = freeBytesAvail / 1073741824.0;
                                            if (networkInfo.TotalGB > 0)
                                            {
                                                networkInfo.FreePercent = (networkInfo.FreeGB / networkInfo.TotalGB) * 100;
                                                int barrasLlenas = Math.Max(0, Math.Min(10, (int)Math.Round((100 - networkInfo.FreePercent) / 10.0)));
                                                networkInfo.VisualUse = $"[{new string('█', barrasLlenas)}{new string('░', 10 - barrasLlenas)}]";
                                            }
                                        }
                                        else { networkInfo.VisualUse = "[No accesible]"; }
                                        report.NetworkDrives.Add(networkInfo);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch { }
        }

        // ── Estado de Backup de Windows ──────────────────────────────
        private static void GetBackUpState(ServerData report)
        {
            report.BackupState = "No configurado";
            report.LastBackupDate = "--/--/----";

            try
            {
                string cmdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");

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

                string lastDate = "";
                string[] lanes = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string lane in lanes)
                {
                    string lower = lane.ToLower();

                    if (lower.Contains("backup time:") || lower.Contains("hora de copia") || lower.Contains("hora de la copia"))
                    {
                        var parts = lane.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            lastDate = parts[1].Trim();
                        }
                    }
                }

                if (!string.IsNullOrEmpty(lastDate))
                {
                    report.LastBackupDate = lastDate;
                    report.BackupState = "OK (vía wbadmin)";
                    return;
                }
            }
            catch { }

            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\Microsoft\\Windows\\Backup",
                    "SELECT LastSuccessfulBackupTime, LastBackupResultHR FROM MSFT_WBSummary");
                foreach (ManagementObject summary in s.Get())
                    using (summary)
                    {
                        string date = summary["LastSuccessfulBackupTime"]?.ToString();
                        if (!string.IsNullOrEmpty(date))
                        {
                            report.LastBackupDate = ManagementDateTimeConverter.ToDateTime(date).ToString("dd/MM/yyyy HH:mm");
                            uint hr = 0;
                            try { hr = Convert.ToUInt32(summary["LastBackupResultHR"] ?? 0u); } catch { }
                            report.BackupState = hr == 0 ? "OK" : $"Error (0x{hr:X8})";
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
                    DateTime betterDate = DateTime.MinValue;
                    foreach (string subName in baseKey.GetSubKeyNames())
                    {
                        using var sub = baseKey.OpenSubKey(subName);
                        byte[] ftBytes = sub?.GetValue("ActionItemDate") as byte[];
                        if (ftBytes != null && ftBytes.Length == 8)
                        {
                            DateTime dt = DateTime.FromFileTime(BitConverter.ToInt64(ftBytes, 0));
                            if (dt > betterDate) betterDate = dt;
                        }
                    }
                    if (betterDate > DateTime.MinValue)
                    {
                        report.LastBackupDate = betterDate.ToString("dd/MM/yyyy HH:mm");
                        report.BackupState = "OK";
                    }
                }
            }
            catch { }
        }

        // ── Java Desktop (JRE 8): detección local + consulta online en java.com ───
        public static async Task FetchJavaAsync(ServerData report, Action<string> log, HttpClient http)
        {
            report.JavaVersion = "No instalado / No detectado";
            report.IsJavaUpToDate = false;
            report.JavaVersionOnline = "";
            string installedVersion = "";
            int updateLocal = 0;

            string[] registryPaths = {
                @"SOFTWARE\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment"
            };

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
                            if (updateNum > updateLocal)
                            {
                                updateLocal = updateNum;
                                installedVersion = subName;
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

            report.JavaVersion = $"Java 8 Update {updateLocal}";

            try
            {
                log?.Invoke("    · Java: Consultando última versión en java.com...\n");
                string html = await http.GetStringAsync("https://www.java.com/es/download/");
                var matchOnline = Regex.Match(html, @"Versi(?:ó|o)n 8 Update (\d+)");

                if (matchOnline.Success)
                {
                    int updateOnline = int.Parse(matchOnline.Groups[1].Value);
                    report.JavaVersionOnline = $"Java 8 Update {updateOnline} (Fuente: java.com)";

                    if (updateLocal >= updateOnline)
                    {
                        report.IsJavaUpToDate = true;
                        report.JavaVersion += " ✅";
                    }
                    else
                    {
                        report.IsJavaUpToDate = false;
                        report.JavaVersion += $" ⚠️ (disponible Update {updateOnline})";
                    }

                    log?.Invoke($"    · Java instalado: Update {updateLocal} | Disponible en web: Update {updateOnline}\n");
                }
                else
                {
                    report.JavaVersionOnline = "No se pudo encontrar la versión en java.com";
                    log?.Invoke("    · Java: Cambio en el diseño de java.com, no se encontró el Update.\n");
                }
            }
            catch
            {
                report.JavaVersionOnline = "No se pudo verificar (sin conexión o error de red)";
                log?.Invoke("    · Java: No se pudo consultar versión online.\n");
            }
        }

        // ── Detección de la versión de DF-Server ─────────────────────
        private static void GetDfServerVersion(ServerData report)
        {
            report.DfServer.Version = "No detectada";

            string[] registryPaths = {
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
                @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
            };

            foreach (var keyPath in registryPaths)
            {
                try
                {
                    using var baseKey = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (baseKey == null) continue;

                    foreach (string subKeyName in baseKey.GetSubKeyNames())
                    {
                        using var subKey = baseKey.OpenSubKey(subKeyName);
                        string displayName = subKey?.GetValue("DisplayName")?.ToString() ?? "";

                        // Si encontramos DF-Server en la lista de programas
                        if (displayName.IndexOf("DF-Server", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            displayName.IndexOf("DFServer", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // INTENTO 1: Leer la versión del registro (si existe)
                            string version = subKey?.GetValue("DisplayVersion")?.ToString();
                            if (!string.IsNullOrWhiteSpace(version))
                            {
                                report.DfServer.Version = version;
                                return;
                            }

                            // INTENTO 2: Registro en blanco -> Buscamos el ejecutable en la ruta del registro
                            string installLocation = subKey?.GetValue("InstallLocation")?.ToString() ?? "";
                            if (string.IsNullOrWhiteSpace(installLocation))
                            {
                                installLocation = @"C:\Program Files (x86)\SIT\DF-SERVER_EVO(Server)";
                            }

                            string pathExe = Path.Combine(installLocation, "DFServer_kernel.exe");
                            string extractedVersion = ExtractFileVersion(pathExe);

                            if (!string.IsNullOrWhiteSpace(extractedVersion))
                            {
                                report.DfServer.Version = extractedVersion;
                                return;
                            }

                            break;
                        }
                    }
                }
                catch { }
            }

            // INTENTO 3 (Fallback): Vamos a la ruta estandar asegurada (Si en un futuro esto cambia, habría que actualizarlo aquí)
            string safetyExePath = @"C:\Program Files (x86)\SIT\DF-SERVER_EVO(Server)\DFServer_kernel.exe";
            string safetyVersion = ExtractFileVersion(safetyExePath);

            if (!string.IsNullOrWhiteSpace(safetyVersion))
            {
                report.DfServer.Version = safetyVersion;
            }
            else
            {
                report.DfServer.Version = "Versión oculta (Registro y archivo sin datos)";
            }
        }

        // ── Método auxiliar para leer el EXE ──
        private static string ExtractFileVersion(string ruta)
        {
            if (!System.IO.File.Exists(ruta)) return null;

            try
            {
                var versionInfo = System.Diagnostics.FileVersionInfo.GetVersionInfo(ruta);
                string versionExe = versionInfo.FileVersion ?? versionInfo.ProductVersion;

                // Si el fabricante le puso versión y no es un valor vacío
                if (!string.IsNullOrWhiteSpace(versionExe) && versionExe.Trim() != "0.0.0.0")
                {
                    return versionExe;
                }

                // Si el archivo existe pero el fabricante olvidó compilar la versión, 
                // extraemos la fecha de creación/modificación del kernel.
                DateTime modificationDate = System.IO.File.GetLastWriteTime(ruta);
                return $"Desconocida (Compilado: {modificationDate:dd/MM/yyyy})";
            }
            catch
            {
                return null;
            }
        }
    }
}