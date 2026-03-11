using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace CopicanariasServerReport.Services
{
    public static class SmartService
    {
        // Consulta Win32_DiskDrive y devuelve la lista de discos físicos
        // con modelo, tipo de interfaz, tamaño y estado S.M.A.R.T. básico.
        // El parámetro 'log' es el callback para escribir mensajes en la UI.
        public static async Task<List<DiscoInfo>> ObtenerDiscosAsync(Action<string> log)
        {
            var discos = new List<DiscoInfo>();
            string errorMsg = null;

            await Task.Run(() =>
            {
                try
                {
                    using var s = new ManagementObjectSearcher(
                        "SELECT Model, Status, InterfaceType, Size FROM Win32_DiskDrive");
                    foreach (ManagementObject d in s.Get())
                        using (d)
                        {
                            string modelo = d["Model"]?.ToString()?.Trim() ?? "Desconocido";
                            string tipo = d["InterfaceType"]?.ToString() ?? "Desconocido";
                            string estado = d["Status"]?.ToString() ?? "Desconocido";
                            long sizeBytes = 0;
                            try { sizeBytes = Convert.ToInt64(d["Size"] ?? 0L); } catch { }
                            double sizeGB = sizeBytes / 1073741824.0;
                            string estadoFinal = estado.ToUpper() == "OK"
                                ? "OK (Saludable)"
                                : $"ALERTA ({estado})";

                            discos.Add(new DiscoInfo
                            {
                                Modelo = modelo,
                                Tipo = tipo,
                                Estado = estadoFinal,
                                TamanoGB = sizeGB
                            });
                        }
                }
                catch (Exception ex) { errorMsg = ex.Message; }
            });

            if (errorMsg != null)
            {
                log(">>> ⚠️  No se pudieron leer los discos del sistema.\n");
                log("    Asegúrate de ejecutar la aplicación como administrador.\n");
            }

            foreach (var d in discos)
                log($"    · {d.Modelo} [{d.Tipo}, {d.TamanoGB:F0} GB] — {d.Estado}\n");

            return discos;
        }
    }
}