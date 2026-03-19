using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Win32.SafeHandles;

namespace CopicanariasServerReport.Services
{
    public static class SmartService
    {
        // ── Funciones nativas de Windows (Plan B) ──
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_PROPERTY_QUERY
        {
            public uint PropertyId; public uint QueryType; public uint ProtocolType; public uint DataType;
            public uint ProtocolDataRequestValue; public uint ProtocolDataRequestSubValue; public uint ProtocolDataOffset;
            public uint ProtocolDataLength; public uint FixedProtocolReturnData; public uint ProtocolDataRequestSubValue2;
            public uint ProtocolDataRequestSubValue3; public uint Reserved;
        }

        public static async Task<List<DiscoInfo>> ObtenerDiscosAsync(Action<string> log)
        {
            var discos = new List<DiscoInfo>();

            // Extraemos el exe incrustado a la carpeta temporal
            string smartctlPath = ExtraerSmartctlOculto();
            bool usaSmartctl = !string.IsNullOrEmpty(smartctlPath) && File.Exists(smartctlPath);

            if (usaSmartctl)
                log($">>> ⚡ Motor de diagnóstico avanzado (smartctl) cargado en memoria...\n");
            else
                log($">>> ⚠️ Motor avanzado no disponible. Usando nativo (WMI/NVMe)...\n");

            await Task.Run(async () =>
            {
                var smartPorIndice = new Dictionary<int, SmartAtributos>();

                // Si NO hay smartctl, pre-cargamos la telemetría de Windows por si acaso
                if (!usaSmartctl) CargarTelemetriaWmi(smartPorIndice);

                // ── BASE DE VERDAD: Win32_DiskDrive ──
                using var s = new ManagementObjectSearcher("SELECT Model, Status, InterfaceType, Size, Index FROM Win32_DiskDrive");
                foreach (ManagementObject d in s.Get())
                {
                    using (d)
                    {
                        string modelo = d["Model"]?.ToString()?.Trim() ?? "Desconocido";
                        string tipo = d["InterfaceType"]?.ToString() ?? "Desconocido";
                        string estadoPnp = d["Status"]?.ToString() ?? "Desconocido";

                        long sizeBytes = 0; try { sizeBytes = Convert.ToInt64(d["Size"] ?? 0L); } catch { }
                        int diskIndex = 0; try { diskIndex = Convert.ToInt32(d["Index"] ?? 0); } catch { }

                        var disco = new DiscoInfo
                        {
                            Modelo = modelo,
                            Tipo = tipo.ToUpper().Contains("USB") ? "USB" : tipo,
                            TamanoGB = sizeBytes / 1073741824.0
                        };

                        // Regla para el estado inicial
                        if (disco.Tipo == "USB")
                            disco.Estado = "N/A (Dispositivo extraíble)";
                        else
                            disco.Estado = estadoPnp.ToUpper() == "OK" ? "Operativo (Conectado)" : $"Error de sistema ({estadoPnp})";

                        // ── RADIOGRAFÍA S.M.A.R.T. ──
                        bool smartExtraido = false;

                        if (usaSmartctl)
                        {
                            // Interrogamos al disco con smartctl
                            string json = await EjecutarComandoOcultoAsync(smartctlPath, $"-a -j /dev/pd{diskIndex}");
                            if (!string.IsNullOrEmpty(json))
                            {
                                smartExtraido = ParsearJsonSmartctl(json, disco);
                            }
                        }

                        // Fallback al motor nativo en C# si smartctl falló o no está
                        if (!smartExtraido && !usaSmartctl)
                        {
                            if (smartPorIndice.TryGetValue(diskIndex, out var attrs))
                            {
                                disco.Temperatura = attrs.Temperatura;
                                disco.HorasEncendido = attrs.HorasEncendido;
                                disco.PorcentajeSalud = attrs.PorcentajeSalud;
                                disco.TieneDatosSalud = attrs.TieneSalud;
                            }

                            if (!disco.Temperatura.HasValue && !disco.HorasEncendido.HasValue && !disco.TieneDatosSalud)
                            {
                                var nvmeAttrs = LeerNvmeDirecto(diskIndex);
                                if (nvmeAttrs != null)
                                {
                                    disco.Temperatura = nvmeAttrs.Temperatura;
                                    disco.HorasEncendido = nvmeAttrs.HorasEncendido;
                                    disco.PorcentajeSalud = nvmeAttrs.PorcentajeSalud;
                                    disco.TieneDatosSalud = nvmeAttrs.TieneSalud;
                                }
                            }
                        }

                        discos.Add(disco);
                    }
                }
            });

            ImprimirLogDiscos(discos, log);
            return discos;
        }

        // ════════════════════════════════════════════════════════════════════
        // EXTRACCIÓN DEL EJECUTABLE INCRUSTADO
        // ════════════════════════════════════════════════════════════════════
        private static string ExtraerSmartctlOculto()
        {
            // Lo guardaremos en la carpeta temporal de Windows del usuario
            string tempPath = Path.Combine(Path.GetTempPath(), "smartctl_copicanarias.exe");

            // Si ya lo extrajimos en una ejecución anterior, nos ahorramos el trabajo
            if (File.Exists(tempPath)) return tempPath;

            try
            {
                // El nombre del recurso sigue el formato: EspacioDeNombres.NombreDelArchivo.exe
                string resourceName = "CopicanariasServerReport.smartctl.exe";

                using Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
                if (stream == null) return null; // No se encontró incrustado

                using FileStream fileStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);

                return tempPath;
            }
            catch
            {
                return null;
            }
        }

        // ════════════════════════════════════════════════════════════════════
        // PARSEO DE JSON (Extrae Temp, Horas, Salud, Modelo Interno y Estado)
        // ════════════════════════════════════════════════════════════════════
        private static bool ParsearJsonSmartctl(string json, DiscoInfo disco)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                bool extraidoAlgo = false;

                // 1. INTENTAR RESCATAR EL NOMBRE REAL (Solo si es USB)
                if (disco.Tipo == "USB")
                {
                    if (root.TryGetProperty("model_name", out var modelProp) && !string.IsNullOrWhiteSpace(modelProp.GetString()))
                    {
                        disco.Modelo = modelProp.GetString(); // Cambiamos por el modelo real del chip
                    }
                }

                // 2. TEMPERATURA
                if (root.TryGetProperty("temperature", out var tempProp) && tempProp.TryGetProperty("current", out var curTemp))
                {
                    disco.Temperatura = curTemp.GetInt32();
                    extraidoAlgo = true;
                }

                // 3. HORAS DE ENCENDIDO
                if (root.TryGetProperty("power_on_time", out var powProp) && powProp.TryGetProperty("hours", out var hoursProp))
                {
                    disco.HorasEncendido = hoursProp.GetInt32();
                    extraidoAlgo = true;
                }

                // 4. SALUD % (NVMe)
                if (root.TryGetProperty("nvme_smart_health_information_log", out var nvmeProp) && nvmeProp.TryGetProperty("percentage_used", out var wearProp))
                {
                    disco.PorcentajeSalud = 100 - wearProp.GetInt32();
                    disco.TieneDatosSalud = true;
                    extraidoAlgo = true;
                }
                // SALUD % (SATA Clásico)
                else if (root.TryGetProperty("ata_smart_attributes", out var ataProp) && ataProp.TryGetProperty("table", out var tableProp))
                {
                    foreach (var attr in tableProp.EnumerateArray())
                    {
                        if (attr.TryGetProperty("id", out var idProp) && attr.TryGetProperty("value", out var valProp))
                        {
                            int id = idProp.GetInt32();
                            // IDs comunes de fabricantes para la salud del SSD
                            if (id == 202 || id == 231 || id == 169 || id == 173 || id == 177)
                            {
                                disco.PorcentajeSalud = valProp.GetInt32();
                                disco.TieneDatosSalud = true;
                                extraidoAlgo = true;
                                break;
                            }
                        }
                    }
                }

                // 5. ESTADO S.M.A.R.T. GENERAL
                if (root.TryGetProperty("smart_status", out var statusProp) && statusProp.TryGetProperty("passed", out var passedProp))
                {
                    bool smartOk = passedProp.GetBoolean();

                    if (!smartOk)
                    {
                        // Si S.M.A.R.T. dice que falla, ponemos la alerta obligatoriamente
                        disco.Estado = "ALERTA (Fallo SMART detectado)";
                    }
                    else if (disco.Tipo == "USB")
                    {
                        // Si es un USB inteligente y ha pasado el test, lo ascendemos
                        disco.Estado = "Operativo (Conectado)";
                    }
                }

                return extraidoAlgo;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<string> EjecutarComandoOcultoAsync(string exePath, string argumentos)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exePath,
                Arguments = argumentos,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8
            };
            using var proc = Process.Start(psi);
            if (proc == null) return null;
            string output = await proc.StandardOutput.ReadToEndAsync();
            await proc.WaitForExitAsync();
            return output;
        }

        // ════════════════════════════════════════════════════════════════════
        // HELPERS Y MOTORES NATIVOS
        // ════════════════════════════════════════════════════════════════════
        private static void ImprimirLogDiscos(List<DiscoInfo> discos, Action<string> log)
        {
            foreach (var d in discos)
            {
                log($"    · {d.Modelo} [{d.Tipo}, {d.TamanoGB:F0} GB] — {d.Estado}\n");

                bool hayDetalle = d.Temperatura.HasValue || d.HorasEncendido.HasValue || d.TieneDatosSalud;

                if (hayDetalle)
                {
                    var p = new List<string>();
                    if (d.Temperatura.HasValue) p.Add($"Temp: {d.Temperatura}°C");
                    if (d.HorasEncendido.HasValue) p.Add($"Encendido: {d.HorasEncendido:N0}h");
                    if (d.TieneDatosSalud) p.Add($"Salud SSD: {d.PorcentajeSalud}%");
                    log($"      S.M.A.R.T.: {string.Join("  |  ", p)}\n");
                }
                else if (d.Tipo == "USB" || d.Estado.Contains("N/A"))
                {
                    log($"      S.M.A.R.T.: No aplica o bloqueado por puente USB.\n");
                }
                else
                {
                    log($"      S.M.A.R.T.: Bloqueado por fabricante/firmware.\n");
                }
            }
        }

        private static void CargarTelemetriaWmi(Dictionary<int, SmartAtributos> smartPorIndice)
        {
            try
            {
                using var searcher1 = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT DeviceId, Temperature, PowerOnHours, Wear FROM MSFT_StorageReliabilityCounter");
                foreach (ManagementObject d in searcher1.Get())
                {
                    if (int.TryParse(d["DeviceId"]?.ToString(), out int id))
                    {
                        var attrs = new SmartAtributos();
                        try { attrs.Temperatura = Convert.ToInt32(d["Temperature"]); } catch { }
                        try { attrs.HorasEncendido = Convert.ToInt32(d["PowerOnHours"]); } catch { }
                        try { attrs.PorcentajeSalud = 100 - Convert.ToInt32(d["Wear"]); attrs.TieneSalud = true; } catch { }
                        smartPorIndice[id] = attrs;
                    }
                }
            }
            catch { }

            try
            {
                using var searcher2 = new ManagementObjectSearcher(@"root\wmi", "SELECT * FROM MSStorageDriver_FailurePredictData");
                int fallbackIdx = 0;
                foreach (ManagementObject d in searcher2.Get())
                {
                    string instanceName = d["InstanceName"]?.ToString() ?? "";
                    int diskIdx = ExtraerIndiceDisco(instanceName, fallbackIdx++);
                    if (!smartPorIndice.TryGetValue(diskIdx, out var attrs)) smartPorIndice[diskIdx] = attrs = new SmartAtributos();
                    try
                    {
                        byte[] data = (byte[])d["VendorSpecific"];
                        for (int i = 2; i + 11 < data.Length; i += 12)
                        {
                            int id = data[i]; if (id == 0) continue;
                            int valorNorm = data[i + 3], rawLow = data[i + 5], rawHigh = data[i + 6];
                            switch (id)
                            {
                                case 194: case 190: if (attrs.Temperatura == null) attrs.Temperatura = rawLow; break;
                                case 9: if (attrs.HorasEncendido == null) attrs.HorasEncendido = rawLow + (rawHigh * 256); break;
                                case 202:
                                case 231:
                                case 169:
                                case 173:
                                case 177:
                                    if (!attrs.TieneSalud) { attrs.PorcentajeSalud = valorNorm; attrs.TieneSalud = true; }
                                    break;
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }

        private static SmartAtributos LeerNvmeDirecto(int index)
        {
            string path = $@"\\.\PhysicalDrive{index}";
            using var hDrive = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);
            if (hDrive.IsInvalid) return null;

            var q = new STORAGE_PROPERTY_QUERY { PropertyId = 50, QueryType = 0, ProtocolType = 2, DataType = 2, ProtocolDataRequestValue = 2, ProtocolDataOffset = 40, ProtocolDataLength = 512 };
            int inSz = Marshal.SizeOf(q), outSz = 48 + 512;
            IntPtr pIn = Marshal.AllocHGlobal(inSz), pOut = Marshal.AllocHGlobal(outSz);

            try
            {
                Marshal.StructureToPtr(q, pIn, false);
                for (int i = 0; i < outSz; i++) Marshal.WriteByte(pOut, i, 0);
                if (DeviceIoControl(hDrive, 0x2D1400, pIn, (uint)inSz, pOut, (uint)outSz, out uint ret, IntPtr.Zero) && ret >= 48 + 512)
                {
                    return new SmartAtributos
                    {
                        Temperatura = ((Marshal.ReadByte(pOut, 48 + 2) << 8) | Marshal.ReadByte(pOut, 48 + 1)) - 273,
                        PorcentajeSalud = 100 - Marshal.ReadByte(pOut, 48 + 5),
                        TieneSalud = true,
                        HorasEncendido = Marshal.ReadInt32(pOut, 48 + 128)
                    };
                }
            }
            finally { Marshal.FreeHGlobal(pIn); Marshal.FreeHGlobal(pOut); }
            return null;
        }

        private static int ExtraerIndiceDisco(string instanceName, int fallback)
        {
            int last = instanceName.LastIndexOf('_');
            if (last >= 0 && last < instanceName.Length - 1 && int.TryParse(instanceName.Substring(last + 1), out int idx)) return idx;
            return fallback;
        }

        private sealed class SmartAtributos
        {
            public int? Temperatura { get; set; }
            public int? HorasEncendido { get; set; }
            public int? PorcentajeSalud { get; set; }
            public bool TieneSalud { get; set; }
        }
    }
}