using Microsoft.Win32;
using WUApiLib;

namespace CopicanariasServerReport.Services
{
    public static class UpdateService
    {
        // Analiza el estado de Windows Update y escribe los resultados en 'reporte'.
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

                        // Listas temporales solo para organizar el log visual
                        var importantes = new List<string>();
                        var opcionales = new List<string>();

                        foreach (IUpdate upd in result.Updates)
                        {
                            reporte.NombresUpdates.Add(upd.Title); // Mantenemos la lista intacta para el PDF
                            if (upd.AutoSelectOnWebSites)
                            {
                                reporte.UpdatesImportantes++;
                                importantes.Add(upd.Title);
                            }
                            else
                            {
                                reporte.UpdatesOpcionales++;
                                opcionales.Add(upd.Title);
                            }
                        }

                        // ── Detección de reinicio pendiente ──────────────
                        reporte.RequiereReinicio = false;

                        try
                        {
                            using var key = Registry.LocalMachine.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                            if (key != null) reporte.RequiereReinicio = true;
                        }
                        catch { }

                        if (!reporte.RequiereReinicio)
                        {
                            try
                            {
                                var sysInfo = new WUApiLib.SystemInformation();
                                reporte.RequiereReinicio = sysInfo.RebootRequired;
                            }
                            catch { }
                        }

                        // ── Impresión del Log en pantalla ──────────────
                        if (reporte.UpdatesImportantes == 0 && reporte.UpdatesOpcionales == 0)
                        {
                            log(">>> ✅ Sistema al día. No hay actualizaciones pendientes.\n");
                        }
                        else
                        {
                            log($">>> ⚠️  Pendientes: {reporte.UpdatesImportantes} importantes | {reporte.UpdatesOpcionales} opcionales\n");

                            if (importantes.Count > 0)
                            {
                                log("\n    🔴 IMPORTANTE:\n");
                                foreach (var nombre in importantes) log($"      · {nombre}\n");
                            }

                            if (opcionales.Count > 0)
                            {
                                log("\n    🔵 OPCIONAL:\n");
                                foreach (var nombre in opcionales) log($"      · {nombre}\n");
                            }
                            log("\n"); // Espacio al final
                        }

                        if (reporte.RequiereReinicio)
                            log(">>> 🔁 REINICIO PENDIENTE — Se requiere reiniciar el equipo.\n");
                        else if (reporte.UpdatesImportantes > 0 || reporte.UpdatesOpcionales > 0)
                            log(">>> ℹ️  Puedes instalarlas pulsando en 'Instalar Actualizaciones'.\n");

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
            }
        }

        // ── Método para DESCARGAR E INSTALAR actualizaciones ──────────────
        public static async Task InstalarAsync(DatosServidor reporte, Action<string> log)
        {
            Exception errorCapturado = null;

            await Task.Run(() =>
            {
                Exception hiloError = null;
                var hilo = new Thread(() =>
                {
                    try
                    {
                        var session = new UpdateSession();
                        var searcher = session.CreateUpdateSearcher();

                        log(">>> 🔎 Buscando paquetes para instalar...\n");
                        var searchResult = searcher.Search("IsInstalled=0 and DeploymentAction=*");

                        if (searchResult.Updates.Count == 0)
                        {
                            log("    · No hay actualizaciones que instalar en este momento.\n");
                            return;
                        }

                        // 1. Preparar la descarga
                        UpdateCollection updatesToDownload = new UpdateCollection();
                        foreach (IUpdate update in searchResult.Updates)
                        {
                            if (!update.EulaAccepted) update.AcceptEula();
                            updatesToDownload.Add(update);
                        }

                        log($">>> ⬇️ Descargando {updatesToDownload.Count} actualización(es) (Puede tardar varios minutos)...\n");
                        var downloader = session.CreateUpdateDownloader();
                        downloader.Updates = updatesToDownload;
                        var downloadResult = downloader.Download();

                        if (downloadResult.ResultCode != OperationResultCode.orcSucceeded && downloadResult.ResultCode != OperationResultCode.orcSucceededWithErrors)
                        {
                            log($"    ❌ Error durante la descarga. Código de resultado: {downloadResult.ResultCode}\n");
                            return;
                        }

                        // 2. Preparar la instalación
                        UpdateCollection updatesToInstall = new UpdateCollection();
                        foreach (IUpdate update in searchResult.Updates)
                        {
                            if (update.IsDownloaded)
                            {
                                updatesToInstall.Add(update);
                            }
                        }

                        if (updatesToInstall.Count == 0)
                        {
                            log("    ⚠️ Se descargaron, pero ninguna está lista para instalarse.\n");
                            return;
                        }

                        log($">>> ⚙️ Instalando {updatesToInstall.Count} actualización(es). Por favor, no cierres el programa...\n");
                        var installer = session.CreateUpdateInstaller();
                        installer.Updates = updatesToInstall;

                        // Le decimos que no lance prompts visuales pidiendo medios de instalación
                        installer.AllowSourcePrompts = false;

                        var installResult = installer.Install();

                        log($">>> ✅ Proceso de instalación finalizado. (Código: {installResult.ResultCode})\n");

                        if (installResult.RebootRequired)
                        {
                            reporte.RequiereReinicio = true;
                            log(">>> 🔁 REINICIO OBLIGATORIO: Debes reiniciar el servidor para aplicar los cambios.\n");
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
                log($">>> ❌ Ocurrió un error grave durante la instalación:\n    {errorCapturado.Message}\n");
            }
        }
    }
}