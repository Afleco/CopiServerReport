using System.Diagnostics;
using System.Management;
using System.Runtime.InteropServices;
using LibreHardwareMonitor.Hardware;
using Microsoft.Win32.SafeHandles;

namespace CopicanariasServerReport.Services
{
    public static class SmartService
    {
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

        private class LhmDiskData
        {
            public string Name { get; set; } = "";
            public int? Temperature { get; set; }
            public int? HealthPercent { get; set; }
            public int? HoursUsed { get; set; }
        }

        public static async Task<List<DiskInfo>> ObtenerDiscosAsync(Action<string> log)
        {
            var disks = new List<DiskInfo>();
            log($">>> ⚡ Iniciando motor LibreHardwareMonitor con verificación S.M.A.R.T. nativa...\n");

            await Task.Run(() =>
            {
                var lhmData = ExtactLHMData(log);
                var smartByIndex = new Dictionary<int, SmartAtributos>();
                LoadWMITelemetry(smartByIndex);

                // ── Mapeo de tipos de bus modernos para esquivar el falso "SCSI" ──
                var modernBus = new Dictionary<int, string>();
                try
                {
                    using var sBus = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT DeviceId, BusType FROM MSFT_PhysicalDisk");
                    foreach (ManagementObject obj in sBus.Get())
                    {
                        if (int.TryParse(obj["DeviceId"]?.ToString(), out int devId))
                        {
                            int busType = Convert.ToInt32(obj["BusType"] ?? 0);
                            // 17 = NVMe, 11 = SATA, 7 = USB, 8 = RAID
                            if (busType == 17) modernBus[devId] = "NVMe";
                            else if (busType == 11) modernBus[devId] = "SATA";
                            else if (busType == 7) modernBus[devId] = "USB";
                        }
                    }
                }
                catch { /* Ignoramos si la API no está disponible */ }

                using var s = new ManagementObjectSearcher("SELECT Model, Status, InterfaceType, Size, Index FROM Win32_DiskDrive");
                foreach (ManagementObject d in s.Get())
                {
                    using (d)
                    {
                        string model = d["Model"]?.ToString()?.Trim() ?? "Desconocido";
                        string pnpState = d["Status"]?.ToString() ?? "Desconocido";
                        
                        long sizeBytes = 0; try { sizeBytes = Convert.ToInt64(d["Size"] ?? 0L); } catch { }
                        int diskIndex = 0; try { diskIndex = Convert.ToInt32(d["Index"] ?? 0); } catch { }

                        // ── ASIGNACIÓN INTELIGENTE DE INTERFAZ ──
                        string type = "Desconocido";
                        string oldWMIType = d["InterfaceType"]?.ToString() ?? "";

                        // 1. Priorizamos la API moderna (inmune al falso SCSI)
                        if (modernBus.TryGetValue(diskIndex, out string realType))
                            type = realType;
                        // 2. Si falla, miramos si el modelo dice explícitamente NVMe
                        else if (model.IndexOf("NVMe", StringComparison.OrdinalIgnoreCase) >= 0)
                            type = "NVMe";
                        // 3. Fallback a lo que diga WMI viejo
                        else
                            type = oldWMIType.ToUpper().Contains("USB") ? "USB" : oldWMIType;

                        var disk = new DiskInfo
                        {
                            Model = model,
                            Type = type,
                            SizeGB = sizeBytes / 1073741824.0
                        };

                        if (disk.Type == "USB")
                            disk.State = "N/A (Dispositivo extraíble)";
                        else
                            disk.State = pnpState.ToUpper() == "OK" ? "Operativo (Conectado)" : $"Error de sistema ({pnpState})";

                        // ── PASO 1: LHM ──
                        var lhmDisk = lhmData.FirstOrDefault(x =>
                            x.Name.IndexOf(model, StringComparison.OrdinalIgnoreCase) >= 0 ||
                            model.IndexOf(x.Name, StringComparison.OrdinalIgnoreCase) >= 0);

                        if (lhmDisk != null)
                        {
                            disk.Temperature = lhmDisk.Temperature;
                            disk.HoursUsed = lhmDisk.HoursUsed;
                            if (lhmDisk.HealthPercent.HasValue)
                            {
                                disk.HealthPercent = lhmDisk.HealthPercent;
                                disk.HasHealthData = true;
                            }
                            lhmData.Remove(lhmDisk);
                        }

                        // ── PASO 2 y 3: FALLBACK + VERIFICACIÓN S.M.A.R.T REAL ──
                        bool imminentSmartFailure = false;

                        if (smartByIndex.TryGetValue(diskIndex, out var attrs))
                        {
                            if (!disk.Temperature.HasValue) disk.Temperature = attrs.Temperatura;
                            if (!disk.HoursUsed.HasValue) disk.HoursUsed = attrs.HorasEncendido;
                            if (!disk.HasHealthData && attrs.TieneSalud)
                            {
                                disk.HealthPercent = attrs.PorcentajeSalud;
                                disk.HasHealthData = true;
                            }
                            if (attrs.imminentFailure) imminentSmartFailure = true; // El Chivato SATA
                        }

                        var nvmeAttrs = LeerNvmeDirecto(diskIndex);
                        if (nvmeAttrs != null)
                        {
                            // Si logramos leerlo por el protocolo NVMe, ES un NVMe garantizado.
                            disk.Type = "NVMe"; 

                            if (!disk.Temperature.HasValue) disk.Temperature = nvmeAttrs.Temperatura;
                            if (!disk.HoursUsed.HasValue) disk.HoursUsed = nvmeAttrs.HorasEncendido;
                            if (!disk.HasHealthData && nvmeAttrs.TieneSalud)
                            {
                                disk.HealthPercent = nvmeAttrs.PorcentajeSalud;
                                disk.HasHealthData = true;
                            }
                            if (nvmeAttrs.imminentFailure) imminentSmartFailure = true; // El Chivato NVMe
                        }

                        // ── RESOLUCIÓN DEL ESTADO S.M.A.R.T. ──
                        if (imminentSmartFailure)
                        {
                            disk.State = "ALERTA (Fallo S.M.A.R.T. de Hardware)";
                        }
                        else if (disk.Type == "USB" && (disk.HasHealthData || disk.Temperature.HasValue))
                        {
                            disk.State = "Operativo (Conectado)";
                        }

                        // Inferencia extra: Si el hardware dice "OK" pero la salud es bajísima
                        if (!imminentSmartFailure && disk.HasHealthData && disk.HealthPercent <= 20)
                        {
                            disk.State = "ALERTA (Desgaste Crítico)";
                        }

                        disks.Add(disk);
                    }
                }
            });

            printLogicalDisks(disks, log);
            return disks;
        }

        private static List<LhmDiskData> ExtactLHMData(Action<string> log)
        {
            var results = new List<LhmDiskData>();
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

                    var tempSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Temperature && s.Value.HasValue);
                    if (tempSensor != null) dto.Temperature = (int)Math.Round(tempSensor.Value.Value);

                    var healthSensor = hw.Sensors.FirstOrDefault(s =>
                        (s.SensorType == SensorType.Level || s.SensorType == SensorType.Data) && s.Value.HasValue &&
                        (s.Name.IndexOf("Life", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("Percentage Used", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("wear", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("spare", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("endurance", StringComparison.OrdinalIgnoreCase) >= 0 ||
                         s.Name.IndexOf("health", StringComparison.OrdinalIgnoreCase) >= 0));

                    if (healthSensor != null)
                    {
                        float rawValue = healthSensor.Value.Value;
                        if (healthSensor.Name.Equals("Percentage Used", StringComparison.OrdinalIgnoreCase) ||
                            healthSensor.Name.Equals("Wear", StringComparison.OrdinalIgnoreCase))
                            dto.HealthPercent = (int)Math.Round(100f - rawValue);
                        else
                            dto.HealthPercent = (int)Math.Round(rawValue);
                    }

                    var hourSensor = hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Factor && s.Value.HasValue && s.Name.IndexOf("power on hour", StringComparison.OrdinalIgnoreCase) >= 0)
                                  ?? hw.Sensors.FirstOrDefault(s => s.SensorType == SensorType.Data && s.Value.HasValue && s.Name.IndexOf("power on hour", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (hourSensor != null) dto.HoursUsed = (int)Math.Round(hourSensor.Value.Value);

                    results.Add(dto);
                }
            }
            catch (Exception ex)
            {
                log($">>> ⚠️ LHM bloqueado o sin permisos: {ex.Message}. Usando nativo...\n");
            }
            finally { computer?.Close(); }

            return results;
        }

        private static void printLogicalDisks(List<DiskInfo> discos, Action<string> log)
        {
            foreach (var d in discos)
            {
                log($"    · {d.Model} [{d.Type}, {d.SizeGB:F0} GB] — {d.State}\n");
                bool hayDetalle = d.Temperature.HasValue || d.HoursUsed.HasValue || d.HasHealthData;

                if (hayDetalle)
                {
                    var p = new List<string>();
                    if (d.Temperature.HasValue) p.Add($"Temp: {d.Temperature}°C");
                    if (d.HoursUsed.HasValue) p.Add($"Encendido: {d.HoursUsed:N0}h");
                    if (d.HasHealthData) p.Add($"Salud SSD: {d.HealthPercent}%");
                    log($"      S.M.A.R.T.: {string.Join("  |  ", p)}\n");
                }
                else if (d.Type == "USB" || d.State.Contains("N/A"))
                    log($"      S.M.A.R.T.: No aplica o bloqueado por puente USB.\n");
                else
                    log($"      S.M.A.R.T.: Bloqueado por fabricante/firmware.\n");
            }
        }

        private static void LoadWMITelemetry(Dictionary<int, SmartAtributos> smartPorIndice)
        {
            try
            {
                using var s1 = new ManagementObjectSearcher(@"root\Microsoft\Windows\Storage", "SELECT DeviceId, Temperature, PowerOnHours, Wear FROM MSFT_StorageReliabilityCounter");
                foreach (ManagementObject d in s1.Get())
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
                // ── LEER ESTADO S.M.A.R.T. DE SATA/ATA ──
                using var s2 = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, PredictFailure FROM MSStorageDriver_FailurePredictStatus");
                int idx = 0;
                foreach (ManagementObject d in s2.Get())
                {
                    int id = ExtraerIndiceDisco(d["InstanceName"]?.ToString() ?? "", idx++);
                    if (!smartPorIndice.TryGetValue(id, out var attrs)) smartPorIndice[id] = attrs = new SmartAtributos();
                    try { attrs.imminentFailure = (bool)d["PredictFailure"]; } catch { }
                }

                using var s3 = new ManagementObjectSearcher(@"root\wmi", "SELECT InstanceName, VendorSpecific FROM MSStorageDriver_FailurePredictData");
                idx = 0;
                foreach (ManagementObject d in s3.Get())
                {
                    int id = ExtraerIndiceDisco(d["InstanceName"]?.ToString() ?? "", idx++);
                    if (!smartPorIndice.TryGetValue(id, out var attrs)) smartPorIndice[id] = attrs = new SmartAtributos();
                    try
                    {
                        byte[] data = (byte[])d["VendorSpecific"];
                        for (int i = 2; i + 11 < data.Length; i += 12)
                        {
                            int code = data[i]; if (code == 0) continue;
                            int valorNorm = data[i + 3], rawLow = data[i + 5], rawHigh = data[i + 6];
                            switch (code)
                            {
                                // recorremos los IDs para encontrar los parámetros que nos importan. Temperatura (194, 190) - Horas de Encendido (9) - Salud del SSD / Vida Útil (202, 231, 169...)
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
                        // ── LEER EL BYTE "CRITICAL WARNING" DEL NVME ──
                        imminentFailure = Marshal.ReadByte(pOut, 48 + 0) > 0,
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
            public bool imminentFailure { get; set; }
            public int? Temperatura { get; set; }
            public int? HorasEncendido { get; set; }
            public int? PorcentajeSalud { get; set; }
            public bool TieneSalud { get; set; }
        }
    }
}