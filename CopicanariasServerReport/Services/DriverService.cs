using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace CopicanariasServerReport.Services
{
    public static class DriverService
    {
        // Escanea todos los dispositivos con error y enriquece la info con datos
        // del driver instalado (proveedor, versión). Se puede llamar de forma
        // independiente (escaneo al arranque) o desde la telemetría del PDF.
        public static async Task<List<DriverInfo>> ScanAsync(Action<string> log)
        {
            var drivers = new List<DriverInfo>();

            await Task.Run(() =>
            {
                // ── Paso 1: dispositivos con error (código ≠ 0) ─────────────
                var failedDevices = new Dictionary<string, DriverInfo>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "SELECT Name, Manufacturer, ConfigManagerErrorCode, DeviceID " +
                        "FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");
                    foreach (ManagementObject d in s.Get())
                        using (d)
                        {
                            int code = 0;
                            try { code = Convert.ToInt32(d["ConfigManagerErrorCode"] ?? 0); } catch { }

                            var info = new DriverInfo
                            {
                                Name = d["Name"]?.ToString()?.Trim() ?? "Dispositivo desconocido",
                                Manufacturer = d["Manufacturer"]?.ToString()?.Trim() ?? "Desconocido",
                                ErrorCode = code,
                                ErrorDescription = DescribeError(code)
                            };

                            string id = d["DeviceID"]?.ToString() ?? "";
                            failedDevices[info.Name] = info;
                            drivers.Add(info);
                        }
                }
                catch { }

                if (drivers.Count == 0) return; // nada que enriquecer

                // ── Paso 2: cruzar con Win32_PnPSignedDriver para obtener
                //    proveedor y versión del driver actualmente instalado ────
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "SELECT DeviceName, DriverProviderName, DriverVersion " +
                        "FROM Win32_PnPSignedDriver WHERE DeviceName IS NOT NULL");
                    foreach (ManagementObject drv in s.Get())
                        using (drv)
                        {
                            string name = drv["DeviceName"]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(name)) continue;

                            if (failedDevices.TryGetValue(name, out var info))
                            {
                                string ver = drv["DriverVersion"]?.ToString() ?? "";
                                string provider = drv["DriverProviderName"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(ver))
                                {
                                    info.DriverVersion = ver;
                                    info.DriverProvider = !string.IsNullOrEmpty(provider) ? provider : "Desconocido";
                                    info.HasDriver = true;
                                }
                            }
                        }
                }
                catch { /* Win32_PnPSignedDriver puede no estar disponible — la info de error ya es válida */ }
            });

            // ── Log de resultados ─────────────────────────────────────────
            if (drivers.Count == 0)
            {
                log("    ✅ Todos los controladores funcionan correctamente.\n");
            }
            else
            {
                log($"    ⚠️  {drivers.Count} dispositivo(s) con problemas:\n");
                foreach (var d in drivers)
                {
                    string driver = d.HasDriver ? $"Driver: {d.DriverVersion}" : "Sin driver instalado";
                    log($"    · {d.Name}\n");
                    log($"      Código {d.ErrorCode}: {d.ErrorDescription} — {driver}\n");
                }
            }

            return drivers;
        }

        // Versión síncrona para usar dentro de Task.Run en la telemetría del PDF
        public static List<DriverInfo> Scan()
        {
            var resultado = new List<DriverInfo>();
            var tarea = ScanAsync(_ => { }); // log vacío: en telemetría no usamos log
            tarea.Wait();
            return tarea.Result;
        }

        // ── Mapeo de códigos de error de Windows a descripciones en español ──
        // Fuente: https://learn.microsoft.com/en-us/windows-hardware/drivers/install/cm-prob-xxx
        public static string DescribeError(int code) => code switch
        {
            1 => "Configuración incorrecta del dispositivo",
            3 => "Driver posiblemente dañado",
            10 => "El dispositivo no puede iniciarse",
            12 => "Recursos insuficientes del sistema",
            14 => "Requiere reinicio del equipo",
            18 => "Reinstalar los drivers del dispositivo",
            19 => "Error en el registro del sistema",
            22 => "Dispositivo deshabilitado manualmente",
            24 => "Dispositivo no detectado en el sistema",
            28 => "Drivers no instalados",
            29 => "Dispositivo deshabilitado en la BIOS",
            31 => "El dispositivo no funciona correctamente",
            32 => "El servicio del driver no pudo iniciarse",
            33 => "Windows no puede determinar los recursos",
            37 => "El driver devolvió un error al iniciarse",
            38 => "Driver anterior aún en memoria (reiniciar)",
            39 => "Driver dañado o archivos faltantes",
            40 => "Sin acceso al dispositivo",
            41 => "Windows no pudo cargar el driver",
            43 => "El dispositivo reportó un error (código 43)",
            45 => "Dispositivo no conectado actualmente",
            47 => "Preparado para extracción segura (Desconéctelo físicamente)",
            48 => "Bloqueado por políticas del sistema",
            52 => "Problema con la firma digital del driver",
            _ => $"Error desconocido (código {code})"
        };
    }
}