using Microsoft.Win32;
using WUApiLib;

namespace CopicanariasServerReport.Services
{
    public static class UpdateService
    {
        // Analiza el estado de Windows Update y escribe los resultados en 'reporte'.
        // WUApiLib es COM/STA: se lanza en un hilo STA explícito para evitar
        // fallos en máquinas cuyo ThreadPool usa apartamento MTA.
        public static async Task AnalizarAsync(DatosServidor reporte, Action<string> log)
        {
            Exception errorCapturado = null;

            await Task.Run(() =>
            {
                Exception hiloError = null;
                var hilo = new Thread(() =>
                {
                    try
                    {
                        reporte.UpdatesEjecutado = true;
                        reporte.UpdatesImportantes = 0;
                        reporte.UpdatesOpcionales = 0;
                        reporte.NombresUpdates.Clear();

                        var session = new UpdateSession();
                        var searcher = session.CreateUpdateSearcher();
                        var result = searcher.Search("IsInstalled=0 and DeploymentAction=*");

                        foreach (IUpdate upd in result.Updates)
                        {
                            reporte.NombresUpdates.Add(upd.Title);
                            if (upd.AutoSelectOnWebSites) reporte.UpdatesImportantes++;
                            else reporte.UpdatesOpcionales++;
                        }

                        // ── Detección de reinicio pendiente ──────────────
                        reporte.RequiereReinicio = false;

                        // Método 1: clave de registro 
                        try
                        {
                            using var key = Registry.LocalMachine.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                            if (key != null) reporte.RequiereReinicio = true;
                        }
                        catch { }

                        // Método 2: ISystemInformation de WUApiLib (respaldo)
                        if (!reporte.RequiereReinicio)
                        {
                            try
                            {
                                var sysInfo = new WUApiLib.SystemInformation();
                                reporte.RequiereReinicio = sysInfo.RebootRequired;
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex) { hiloError = ex; }
                });

                hilo.SetApartmentState(ApartmentState.STA);
                hilo.Start();
                hilo.Join();
                if (hiloError != null) errorCapturado = hiloError;
            });

            if (errorCapturado != null)
            {
                log(">>> ⚠️  No se pudo analizar Windows Update.\n");
                log("    Asegúrate de ejecutar la aplicación como administrador.\n");
                return;
            }

            if (reporte.UpdatesImportantes == 0 && reporte.UpdatesOpcionales == 0)
            {
                log(">>> ✅ Sistema al día. No hay actualizaciones pendientes.\n");
            }
            else
            {
                log($">>> ⚠️  Pendientes: {reporte.UpdatesImportantes} importantes | {reporte.UpdatesOpcionales} opcionales\n");
                int mostradas = 0;
                foreach (var nombre in reporte.NombresUpdates)
                {
                    if (mostradas++ >= 5) break;
                    log($"    · {nombre}\n");
                }
                if (reporte.NombresUpdates.Count > 5)
                    log($"    ... y {reporte.NombresUpdates.Count - 5} más\n");
            }

            if (reporte.RequiereReinicio)
                log(">>> 🔁 REINICIO PENDIENTE — Se requiere reiniciar el equipo.\n");
            else
                log(">>> ✅ No se requiere reinicio.\n");
        }
    }
}