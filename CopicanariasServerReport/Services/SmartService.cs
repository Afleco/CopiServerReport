using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32.SafeHandles;

namespace CopicanariasServerReport.Services
{
    public static class SmartService
    {
        // ── Funciones nativas de Windows (Plan B / Fallback) ──
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

        // ── Patrón Visitor requerido por LibreHardwareMonitor ──
        private class UpdateVisitor : IVisitor
        {
            public void VisitComputer(IComputer computer) { computer.Traverse(this); }
            public void VisitHardware(IHardware hardware)
            {
                hardware.Update();
                foreach (IHardware sub in hardware.SubHardware) sub.Accept(this);
            }
            public void VisitSensor(ISensor sensor) { }
            public void VisitParameter(IParameter parameter) { }
        }

        // DTO temporal para cruzar los datos de LHM con los de Windows
        private class LhmDiskData
        {
            public string Name { get; set; } = "";
            public int? Temperature { get; set; }
            public int? HealthPercent { get; set; }
            public int? HoursUsed { get; set; }
        }

        public static async Task<List<DiscoInfo>> ObtenerDiscosAsync(Action<string> log)
        {
            var discos = new List<DiscoInfo>();
            log($">>> ⚡ Iniciando motor LibreHardwareMonitor...\n");

            await Task.Run(() =>
            {
                // 1. Extraemos los sensores usando LHM
                var datosLhm = ExtraerDatosLHM(log);

                // 2. Pre-cargamos La telemetria de Windows (Plano B)
                var smartPorIndice = new Dictionary<int, SmartAtributos>();
                CargarTelemetriaWmi(smartPorIndice);

                // 3. BASE DE VERDAD: Win32_DiskDrive
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

                        if (disco.Tipo == "USB")
                            disco.Estado = "N/A (Dispositivo extraíble)";
                        else
                            disco.Estado = estadoPnp.ToUpper() == "OK" ? "Operativo (Conectado)" : $"Error de sistema ({estadoPnp})";

                        // ── PASO 1: INTENTAR CON LHM ──
                        var discoLhm = datosLhm.FirstOrDefault(x =>
                            x.Name.IndexOf(modelo, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            modelo.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (discoLhm != null)
                        {
                            disco.Temperatura = discoLhm.Temperature;
                            disco.HorasEncendido = discoLhm.HoursUsed;
                            if (discoLhm.HealthPercent.HasValue)
                            {
                                disco.PorcentajeSalud = discoLhm.HealthPercent;
                                disco.TieneDatosSalud = true;
                            }
                            datosLhm.Remove(discoLhm);
                        }

                        // ── PASO 2: FALLBACK INTELIGENTE (Rellenar los huecos) ──
                        // Si LHM no encontró la temperatura, horas o salud, pedimos ayuda al WMI de Windows
                        if (smartPorIndice.TryGetValue(diskIndex, out var attrs))
                        {
                            if (!disco.Temperatura.HasValue) disco.Temperatura = attrs.Temperatura;
                            if (!disco.HorasEncendido.HasValue) disco.HorasEncendido = attrs.HorasEncendido;
                            if (!disco.TieneDatosSalud && attrs.TieneSalud)
                            {
                                disco.PorcentajeSalud = attrs.PorcentajeSalud;
                                disco.TieneDatosSalud = true;
                            }
                        }

                        // ── PASO 3: TÚNEL DIRECTO NVME (Último recurso) ──
                        if (!disco.Temperatura.HasValue || !disco.HorasEncendido.HasValue || !disco.TieneDatosSalud)
                        {
                            var nvmeAttrs = LeerNvmeDirecto(diskIndex);
                            if (nvmeAttrs != null)
                            {
                                if (!disco.Temperatura.HasValue) disco.Temperatura = nvmeAttrs.Temperatura;
                                if (!disco.HorasEncendido.HasValue) disco.HorasEncendido = nvmeAttrs.HorasEncendido;
                                if (!disco.TieneDatosSalud && nvmeAttrs.TieneSalud)
                                {
                                    disco.PorcentajeSalud = nvmeAttrs.PorcentajeSalud;
                                    disco.TieneDatosSalud = true;
                                }
                            }
                        }

                        // ── ASCENSIÓN DE DISCOS USB ──
                        if (disco.Tipo == "USB" && (disco.TieneDatosSalud || disco.Temperatura.HasValue))
                        {
                            disco.Estado = "Operativo (Conectado)";
                        }

                        discos.Add(disco);
                    }
                }
            });

            ImprimirLogDiscos(discos, log);
            return discos;
        }

        // ════════════════════════════════════════════════════════════════════
        // MOTOR LIBRE HARDWARE MONITOR
        // ════════════════════════════════════════════════════════════════════
        private static List<LhmDiskData> ExtraerDatosLHM(Action<string> log)
        {
            var resultados = new List<LhmDiskData>();
            Computer computer = null;

            try
            {
                computer = new Computer { IsStorageEnabled = true };
                computer.Open();
                computer.Accept(new UpdateVisitor());

                foreach (IHardware hw in computer.Hardware)
                {
                    if (hw.HardwareType != HardwareType.Storage) continue;

                    var dto = new LhmDiskData { Name = hw.Name ?? "Desconocido" };

                    // ── Temperatura ──
                    var tempSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue);
                    if (tempSensor != null) dto.Temperature = (int)Math.Round(tempSensor.Value.Value);

                    // ── Salud / Vida útil (AMPLIADO) ──
                    var healthSensor = hw.Sensors.FirstOrDefault(s =>
                        (s.SensorType == SensorType.Level || s.SensorType == SensorType.Data) && s.Value.HasValue &&
                        (s.Name.IndexOf("Life", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("Percentage Used", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("wear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("spare", StringComparison.OrdinalIgnoreCase) >= 0 || // WD a veces usa Available Spare
                         s.Name.IndexOf("endurance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0));

                    if (healthSensor != null)
                    {
                        float rawValue = healthSensor.Value.Value;
                        // Si el sensor es % de uso, lo invertimos. Si es Available Spare o Life, se queda igual.
                        if (healthSensor.Name.Equals("Percentage Used", StringComparison.OrdinalIgnoreCase) ||
                            healthSensor.Name.Equals("Wear", StringComparison.OrdinalIgnoreCase))
                        {
                            dto.HealthPercent = (int)Math.Round(100f - rawValue);
                        }
                        else
                        {
                            dto.HealthPercent = (int)Math.Round(rawValue);
                        }
                    }

                    // ── Horas de encendido ──
                    var hourSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Factor && s.Value.HasValue && s.Name.IndexOf("power on hour", StringComparison.OrdinalIgnoreCase) >= 0)
                                  ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Value.HasValue && s.Name.IndexOf("power on hour", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (hourSensor != null) dto.HoursUsed = (int)Math.Round(hourSensor.Value.Value);

                    resultados.Add(dto);
                }
            }
            catch (Exception ex)
            {
                log($">>> ⚠️ LHM bloqueado o sin permisos: {ex.Message}. Usando nativo...\n");
            }
            finally
            {
                // MUY IMPORTANTE: Cerrar el computer libera el driver del kernel
                computer?.Close();
            }

            return resultados;
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