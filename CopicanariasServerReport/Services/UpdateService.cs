using Microsoft.Win32;
using WUApiLib;

namespace CopicanariasServerReport.Services
{
    public static class UpdateService
    {
        // ── Método para ANALIZAR actualizaciones pendientes ──────────────
        public static async Task AnalizarAsync(ServerData report, Action<string> log)
        {
            Exception capturedError = null;

            // Delegamos el análisis pesado a un hilo de fondo gestionado por Windows (MTA)
            await Task.Run(() =>
            {
                try
                {
                    report.IsUpdatesExecuted = true;
                    report.ImportantUpdates = 0;
                    report.OptionalUpdates = 0;
                    report.UpdateNames.Clear();

                    var session = new UpdateSession();
                    var searcher = session.CreateUpdateSearcher();
                    var result = searcher.Search("IsInstalled=0 and DeploymentAction=*");

                    // Listas temporales solo para organizar el log visual
                    var importants = new List<string>();
                    var optionals = new List<string>();

                    foreach (IUpdate upd in result.Updates)
                    {
                        report.UpdateNames.Add(upd.Title); // Mantenemos la lista intacta para el PDF
                        if (upd.AutoSelectOnWebSites)
                        {
                            report.ImportantUpdates++;
                            importants.Add(upd.Title);
                        }
                        else
                        {
                            report.OptionalUpdates++;
                            optionals.Add(upd.Title);
                        }
                    }

                    // ── Detección de reinicio pendiente ──────────────
                    report.IsRestartRequired = false;

                    try
                    {
                        using var key = Registry.LocalMachine.OpenSubKey(
                            @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                        if (key != null) report.IsRestartRequired = true;
                    }
                    catch { }

                    if (!report.IsRestartRequired)
                    {
                        try
                        {
                            var sysInfo = new WUApiLib.SystemInformation();
                            report.IsRestartRequired = sysInfo.RebootRequired;
                        }
                        catch { }
                    }

                    // ── Impresión del Log en pantalla ──────────────
                    if (report.ImportantUpdates == 0 && report.OptionalUpdates == 0)
                    {
                        log(">>> ✅ Sistema al día. No hay actualizaciones pendientes.\n");
                    }
                    else
                    {
                        log($">>> ⚠️  Pendientes: {report.ImportantUpdates} importantes | {report.OptionalUpdates} opcionales\n");

                        if (importants.Count > 0)
                        {
                            log("\n    🔴 IMPORTANTE:\n");
                            foreach (var nombre in importants) log($"      · {nombre}\n");
                        }

                        if (optionals.Count > 0)
                        {
                            log("\n    🔵 OPCIONAL:\n");
                            foreach (var nombre in optionals) log($"      · {nombre}\n");
                        }
                        log("\n"); // Espacio al final
                    }

                    if (report.IsRestartRequired)
                        log(">>> 🔁 REINICIO PENDIENTE — Se requiere reiniciar el equipo.\n");
                    else if (report.ImportantUpdates > 0 || report.OptionalUpdates > 0)
                        log(">>> ℹ️  Puedes instalarlas pulsando en 'Instalar Actualizaciones'.\n");

                }
                catch (Exception ex)
                {
                    capturedError = ex;
                }
            });

            if (capturedError != null)
            {
                log(">>> ⚠️  No se pudo analizar Windows Update.\n");
                log($"    Causa: {capturedError.Message}\n");

                // Si es el error de Windows Update no disponible, damos una pista real
                if (capturedError.Message.Contains("0x80240438"))
                {
                    log("    El servicio de Windows Update está ocupado o reiniciándose. Reintenta en unos segundos.\n");
                }
            }
        }

        // ── Método para DESCARGAR E INSTALAR actualizaciones ──────────────
        public static async Task InstallAsync(ServerData reporte, Action<string> log)
        {
            Exception capturedError = null;

            // Delegamos la instalación a un hilo de fondo gestionado por Windows (MTA)
            await Task.Run(() =>
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
                    installer.AllowSourcePrompts = false; // Sin ventanas emergentes

                    var installResult = installer.Install();

                    log($">>> ✅ Proceso de instalación finalizado. (Código: {installResult.ResultCode})\n");

                    if (installResult.RebootRequired)
                    {
                        reporte.IsRestartRequired = true;
                        log(">>> 🔁 REINICIO NECESARIO: Debes reiniciar el servidor para aplicar los cambios.\n");
                    }
                }
                catch (Exception ex)
                {
                    capturedError = ex;
                }
            });

            if (capturedError != null)
            {
                log($">>> ❌ Ocurrió un error durante el proceso de Windows Update:\n    {capturedError.Message}\n");
            }
        }
    }
}