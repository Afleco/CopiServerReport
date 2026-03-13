using System.Management;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace CopicanariasServerReport.Services
{
    public static class SmartService
    {
        // ── Funciones nativas de Windows para acceso directo a hardware ──
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern SafeFileHandle CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode, IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool DeviceIoControl(SafeFileHandle hDevice, uint dwIoControlCode, IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize, out uint lpBytesReturned, IntPtr lpOverlapped);

        [StructLayout(LayoutKind.Sequential)]
        private struct STORAGE_PROPERTY_QUERY
        {
            public uint PropertyId;
            public uint QueryType;
            public uint ProtocolType;
            public uint DataType;
            public uint ProtocolDataRequestValue;
            public uint ProtocolDataRequestSubValue;
            public uint ProtocolDataOffset;
            public uint ProtocolDataLength;
            public uint FixedProtocolReturnData;
            public uint ProtocolDataRequestSubValue2;
            public uint ProtocolDataRequestSubValue3;
            public uint Reserved;
        }

        public static async Task<List<DiscoInfo>> ObtenerDiscosAsync(Action<string> log)
        {
            var discos = new List<DiscoInfo>();
            string errorMsg = null;

            await Task.Run(() =>
            {
                var smartPorIndice = new Dictionary<int, SmartAtributos>();

                // ── CAPA 1: API Moderna (NVMe estándar de Microsoft) ──
                try
                {
                    var scope = new ManagementScope(@"root\Microsoft\Windows\Storage");
                    var query = new ObjectQuery("SELECT DeviceId, Temperature, PowerOnHours, Wear FROM MSFT_StorageReliabilityCounter");
                    using var searcher = new ManagementObjectSearcher(scope, query);
                    foreach (ManagementObject d in searcher.Get())
                    {
                        using (d)
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
                }
                catch { }

                // ── CAPA 2: API Clásica WMI (ATA/SATA) ──
                try
                {
                    var scope = new ManagementScope(@"root\wmi");
                    var query = new ObjectQuery("SELECT * FROM MSStorageDriver_FailurePredictData");
                    using var searcher = new ManagementObjectSearcher(scope, query);
                    int fallbackIdx = 0;
                    foreach (ManagementObject d in searcher.Get())
                        using (d)
                        {
                            string instanceName = d["InstanceName"]?.ToString() ?? "";
                            int diskIdx = ExtraerIndiceDisco(instanceName, fallbackIdx++);
                            if (!smartPorIndice.TryGetValue(diskIdx, out var attrs))
                            {
                                attrs = new SmartAtributos();
                                smartPorIndice[diskIdx] = attrs;
                            }
                            try
                            {
                                byte[] data = (byte[])d["VendorSpecific"];
                                for (int i = 2; i + 11 < data.Length; i += 12)
                                {
                                    int id = data[i];
                                    if (id == 0) continue;
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

                // ── CAPA 3: Win32_DiskDrive + TÚNEL DIRECTO NVMe ──
                try
                {
                    using var s = new ManagementObjectSearcher("SELECT Model, Status, InterfaceType, Size, Index FROM Win32_DiskDrive");
                    foreach (ManagementObject d in s.Get())
                        using (d)
                        {
                            string modelo = d["Model"]?.ToString()?.Trim() ?? "Desconocido";
                            string tipo = d["InterfaceType"]?.ToString() ?? "Desconocido";
                            string estado = d["Status"]?.ToString() ?? "Desconocido";
                            long sizeBytes = 0; try { sizeBytes = Convert.ToInt64(d["Size"] ?? 0L); } catch { }
                            int diskIndex = 0; try { diskIndex = Convert.ToInt32(d["Index"] ?? 0); } catch { }

                            var disco = new DiscoInfo
                            {
                                Modelo = modelo,
                                Tipo = tipo,
                                Estado = estado.ToUpper() == "OK" ? "OK (Saludable)" : $"ALERTA ({estado})",
                                TamanoGB = sizeBytes / 1073741824.0
                            };

                            if (smartPorIndice.TryGetValue(diskIndex, out var attrs))
                            {
                                disco.Temperatura = attrs.Temperatura;
                                disco.HorasEncendido = attrs.HorasEncendido;
                                disco.PorcentajeSalud = attrs.PorcentajeSalud;
                                disco.TieneDatosSalud = attrs.TieneSalud;
                            }

                            // INTENTO DIRECTO SI WMI FALLÓ 
                            bool hayDetalle = disco.Temperatura.HasValue || disco.HorasEncendido.HasValue || disco.TieneDatosSalud;
                            if (!hayDetalle)
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

                            discos.Add(disco);
                        }
                }
                catch (Exception ex) { errorMsg = ex.Message; }
            });

            if (errorMsg != null) log($">>> ⚠️  No se pudieron leer los discos. Error: {errorMsg}\n");

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
                else
                {
                    log($"      S.M.A.R.T.: Bloqueado por fabricante/firmware.\n");
                }
            }
            return discos;
        }

        private static SmartAtributos LeerNvmeDirecto(int index)
        {
            string path = $@"\\.\PhysicalDrive{index}";
            using var hDrive = CreateFile(path, 0xC0000000, 3, IntPtr.Zero, 3, 0, IntPtr.Zero);

            // Si no hay acceso o el disco no responde, salimos 
            if (hDrive.IsInvalid) return null;

            var query = new STORAGE_PROPERTY_QUERY
            {
                PropertyId = 50, // StorageDeviceProtocolSpecificProperty
                QueryType = 0,   // PropertyStandardQuery
                ProtocolType = 2, // ProtocolTypeNvme
                DataType = 2,     // NVMeDataTypeLogPage
                ProtocolDataRequestValue = 2, // Log Page 2 = Información de Salud SMART
                ProtocolDataRequestSubValue = 0,
                ProtocolDataOffset = 40,
                ProtocolDataLength = 512,
                FixedProtocolReturnData = 0,
                ProtocolDataRequestSubValue2 = 0,
                ProtocolDataRequestSubValue3 = 0,
                Reserved = 0
            };

            int inputSize = Marshal.SizeOf(query);
            int outputSize = 48 + 512;
            IntPtr pIn = Marshal.AllocHGlobal(inputSize);
            IntPtr pOut = Marshal.AllocHGlobal(outputSize);

            try
            {
                Marshal.StructureToPtr(query, pIn, false);
                for (int i = 0; i < outputSize; i++) Marshal.WriteByte(pOut, i, 0);

                // IOCTL_STORAGE_QUERY_PROPERTY
                bool ok = DeviceIoControl(hDrive, 0x2D1400, pIn, (uint)inputSize, pOut, (uint)outputSize, out uint ret, IntPtr.Zero);

                if (ok && ret >= 48 + 512)
                {
                    var attrs = new SmartAtributos();

                    // Byte 1-2 (offset 48): Temperatura en Kelvin a Celsius
                    byte tLow = Marshal.ReadByte(pOut, 48 + 1);
                    byte tHigh = Marshal.ReadByte(pOut, 48 + 2);
                    attrs.Temperatura = ((tHigh << 8) | tLow) - 273;

                    // Byte 5 (offset 48): Porcentaje de salud (100 - % de uso)
                    attrs.PorcentajeSalud = 100 - Marshal.ReadByte(pOut, 48 + 5);
                    attrs.TieneSalud = true;

                    // Byte 128 (offset 48): Horas de encendido
                    attrs.HorasEncendido = Marshal.ReadInt32(pOut, 48 + 128);

                    return attrs;
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pIn);
                Marshal.FreeHGlobal(pOut);
            }
            return null;
        }

        private static int ExtraerIndiceDisco(string instanceName, int fallback)
        {
            int lastUnderscore = instanceName.LastIndexOf('_');
            if (lastUnderscore >= 0 && lastUnderscore < instanceName.Length - 1)
                if (int.TryParse(instanceName.Substring(lastUnderscore + 1), out int idx))
                    return idx;
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