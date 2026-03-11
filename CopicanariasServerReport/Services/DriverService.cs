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
        public static async Task<List<DriverInfo>> EscanearAsync(Action<string> log)
        {
            var drivers = new List<DriverInfo>();

            await Task.Run(() =>
            {
                // ── Paso 1: dispositivos con error (código ≠ 0) ─────────────
                var dispositivosConError = new Dictionary<string, DriverInfo>(StringComparer.OrdinalIgnoreCase);
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "SELECT Name, Manufacturer, ConfigManagerErrorCode, DeviceID " +
                        "FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");
                    foreach (ManagementObject d in s.Get())
                        using (d)
                        {
                            int codigo = 0;
                            try { codigo = Convert.ToInt32(d["ConfigManagerErrorCode"] ?? 0); } catch { }

                            var info = new DriverInfo
                            {
                                Nombre = d["Name"]?.ToString()?.Trim() ?? "Dispositivo desconocido",
                                Fabricante = d["Manufacturer"]?.ToString()?.Trim() ?? "Desconocido",
                                CodigoError = codigo,
                                DescripcionError = DescribirError(codigo)
                            };

                            string id = d["DeviceID"]?.ToString() ?? "";
                            dispositivosConError[info.Nombre] = info;
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
                            string nombre = drv["DeviceName"]?.ToString()?.Trim() ?? "";
                            if (string.IsNullOrEmpty(nombre)) continue;

                            if (dispositivosConError.TryGetValue(nombre, out var info))
                            {
                                string ver = drv["DriverVersion"]?.ToString() ?? "";
                                string proveedor = drv["DriverProviderName"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(ver))
                                {
                                    info.VersionDriver = ver;
                                    info.ProveedorDriver = !string.IsNullOrEmpty(proveedor) ? proveedor : "Desconocido";
                                    info.TieneDriver = true;
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
                    string driver = d.TieneDriver ? $"Driver: {d.VersionDriver}" : "Sin driver instalado";
                    log($"    · {d.Nombre}\n");
                    log($"      Código {d.CodigoError}: {d.DescripcionError} — {driver}\n");
                }
            }

            return drivers;
        }

        // Versión síncrona para usar dentro de Task.Run en la telemetría del PDF
        public static List<DriverInfo> Escanear()
        {
            var resultado = new List<DriverInfo>();
            var tarea = EscanearAsync(_ => { }); // log vacío: en telemetría no necesitamos log
            tarea.Wait();
            return tarea.Result;
        }

        // ── Mapeo de códigos de error de Windows a descripciones en español ──
        // Fuente: https://learn.microsoft.com/en-us/windows-hardware/drivers/install/cm-prob-xxx
        public static string DescribirError(int codigo) => codigo switch
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
            48 => "Bloqueado por políticas del sistema",
            52 => "Problema con la firma digital del driver",
            _ => $"Error desconocido (código {codigo})"
        };
    }
}