using System.Management;
using WUApiLib;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System;
using System.Diagnostics.Eventing.Reader;
using System.Threading.Tasks; // Necesario para las tareas asíncronas

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        // ALMACÉN DE DATOS
        DatosServidor miReporte = new DatosServidor();

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e) { }
        private void pictureBoxLogo_Click(object sender, EventArgs e) { }
        private void Form1_Load_1(object sender, EventArgs e) { }
        private void lblTituloCabecera_Click(object sender, EventArgs e) { }

        // =========================================================
        // EVENTOS DE LOS BOTONES (Ahora solo llaman a las tareas)
        // =========================================================
        private void btnCleanTemp_Click(object sender, EventArgs e) { ProcesoLimpieza(); }
        private void btnSmart_Click(object sender, EventArgs e) { ProcesoSmart(); }
        private async void btnUpdate_Click(object sender, EventArgs e) { await ProcesoUpdates(); }
        private void btnReport_Click(object sender, EventArgs e) { ProcesoPDF(); }

        // EL NUEVO BOTÓN AUTOMÁTICO
        private async void btnAuto_Click(object sender, EventArgs e)
        {
            rtbLog.AppendText("\n======================================================\n");
            rtbLog.AppendText(">>> ⚡ INICIANDO MANTENIMIENTO AUTOMÁTICO COMPLETO ⚡ <<<\n");
            rtbLog.AppendText("======================================================\n");

            // Ejecutamos paso a paso, esperando a que los procesos largos terminen
            ProcesoLimpieza();
            ProcesoSmart();
            await ProcesoUpdates(); // ¡Aquí el programa se espera pacientemente a que Windows termine!
            ProcesoPDF();

            rtbLog.AppendText("\n>>> ✅ MANTENIMIENTO AUTOMÁTICO FINALIZADO CON ÉXITO.\n");
        }


        // =========================================================
        // LÓGICA INTERNA DE LOS PROCESOS (Refactorizada)
        // =========================================================

        private void ProcesoLimpieza()
        {
            rtbLog.AppendText("\n>>> [1/4] Iniciando limpieza de archivos temporales...\n");

            string userTempPath = System.IO.Path.GetTempPath();
            string windowsTempPath = @"C:\Windows\Temp";

            int archivosBorrados = 0;
            long bytesLiberados = 0;

            var resUser = LimpiarDirectorio(userTempPath);
            archivosBorrados += resUser.Archivos;
            bytesLiberados += resUser.Bytes;

            var resWin = LimpiarDirectorio(windowsTempPath);
            archivosBorrados += resWin.Archivos;
            bytesLiberados += resWin.Bytes;

            miReporte.ArchivosBorrados = archivosBorrados;
            miReporte.BytesLiberados = bytesLiberados;

            double mbLiberados = bytesLiberados / 1024.0 / 1024.0;
            rtbLog.AppendText($">>> Limpieza completada. Archivos: {archivosBorrados} | Liberado: {mbLiberados:F2} MB\n");
        }

        private void ProcesoSmart()
        {
            rtbLog.AppendText("\n>>> [2/4] Iniciando diagnóstico S.M.A.R.T. de discos...\n");

            try
            {
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Model, Status, InterfaceType FROM Win32_DiskDrive");
                int discosDetectados = 0;
                miReporte.Discos.Clear();

                foreach (ManagementObject disco in searcher.Get())
                {
                    discosDetectados++;
                    string modelo = disco["Model"]?.ToString() ?? "Disco Desconocido";
                    string tipo = disco["InterfaceType"]?.ToString() ?? "Desconocido";
                    string estado = disco["Status"]?.ToString() ?? "Desconocido";

                    rtbLog.AppendText($"- Disco {discosDetectados} [{tipo}]: {modelo}\n");

                    string estadoFinal = "";
                    if (estado.ToUpper() == "OK")
                    {
                        rtbLog.AppendText("  [+] Estado S.M.A.R.T.: OK (Saludable)\n");
                        estadoFinal = "OK (Saludable)";
                    }
                    else
                    {
                        rtbLog.AppendText($"  [-] ALERTA S.M.A.R.T.: El disco reporta problemas ({estado})\n");
                        estadoFinal = $"ALERTA ({estado})";
                    }

                    miReporte.Discos.Add(new DiscoInfo { Modelo = modelo, Tipo = tipo, Estado = estadoFinal });
                }

                if (discosDetectados == 0) rtbLog.AppendText(">>> ADVERTENCIA: No se detectaron discos físicos.\n");
            }
            catch (Exception ex) { rtbLog.AppendText($">>> ERROR al leer discos: {ex.Message}\n"); }
        }

        private async Task ProcesoUpdates()
        {
            rtbLog.AppendText("\n>>> [3/4] Conectando con Windows Update...\n");
            rtbLog.AppendText(">>> (Por favor, ten paciencia, esto puede tardar varios minutos)\n");

            try
            {
                await Task.Run(() =>
                {
                    miReporte.UpdatesEjecutado = true;
                    miReporte.NombresUpdates.Clear();
                    miReporte.UpdatesPendientes = 0;
                    miReporte.UpdatesInstalados = 0;
                    miReporte.RequiereReinicio = false;

                    UpdateSession updateSession = new UpdateSession();
                    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();
                    ISearchResult searchResult = updateSearcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0");

                    int cantidad = searchResult.Updates.Count;
                    miReporte.UpdatesPendientes = cantidad;

                    this.Invoke((MethodInvoker)delegate { rtbLog.AppendText($">>> Se encontraron {cantidad} actualizaciones pendientes.\n"); });

                    if (cantidad > 0)
                    {
                        UpdateCollection updatesToProcess = new UpdateCollection();
                        foreach (IUpdate update in searchResult.Updates)
                        {
                            updatesToProcess.Add(update);
                            miReporte.NombresUpdates.Add(update.Title);
                            this.Invoke((MethodInvoker)delegate { rtbLog.AppendText($"  - {update.Title}\n"); });
                        }

                        this.Invoke((MethodInvoker)delegate { rtbLog.AppendText(">>> Descargando actualizaciones...\n"); });
                        IUpdateDownloader downloader = updateSession.CreateUpdateDownloader();
                        downloader.Updates = updatesToProcess;
                        downloader.Download();

                        this.Invoke((MethodInvoker)delegate { rtbLog.AppendText(">>> Instalando actualizaciones (MODO SILENCIOSO)...\n"); });
                        IUpdateInstaller installer = updateSession.CreateUpdateInstaller();
                        installer.Updates = updatesToProcess;

                        IInstallationResult result = installer.Install();

                        this.Invoke((MethodInvoker)delegate
                        {
                            miReporte.UpdatesInstalados = cantidad;
                            rtbLog.AppendText($">>> Proceso finalizado. Código de resultado: {result.ResultCode}\n");

                            if (result.RebootRequired)
                            {
                                miReporte.RequiereReinicio = true;
                                rtbLog.AppendText(">>> NOTA: El sistema requiere un reinicio.\n");
                            }
                        });
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate { rtbLog.AppendText(">>> El servidor está completamente actualizado.\n"); });
                    }
                });
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($">>> ERROR en Windows Update: {ex.Message}\n");
            }
        }

        private void ProcesoPDF()
        {
            QuestPDF.Settings.License = LicenseType.Community;
            string rutaEscritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fecha = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            string rutaArchivo = System.IO.Path.Combine(rutaEscritorio, $"Auditoria_Copicanarias_{fecha}.pdf");

            try
            {
                rtbLog.AppendText("\n>>> [4/4] Generando informe PDF Profesional...\n");

                // --- DATOS DEL SISTEMA ---
                try
                {
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem"))
                    {
                        foreach (ManagementObject os in searcher.Get()) { miReporte.SistemaOperativo = os["Caption"]?.ToString() ?? Environment.OSVersion.ToString(); break; }
                    }
                    using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                    {
                        foreach (ManagementObject cs in searcher.Get()) { miReporte.MemoriaRAM = $"{Math.Round(Convert.ToInt64(cs["TotalPhysicalMemory"]) / (1024.0 * 1024.0 * 1024.0))} GB"; break; }
                    }
                    System.IO.DriveInfo driveC = new System.IO.DriveInfo("C");
                    if (driveC.IsReady)
                    {
                        double totalGB = driveC.TotalSize / (1024.0 * 1024.0 * 1024.0);
                        double libreGB = driveC.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
                        miReporte.PorcentajeLibreC = (libreGB / totalGB) * 100;
                        miReporte.EspacioDiscoC = $"{libreGB:F1} GB libres de {totalGB:F1} GB";
                    }

                    // COPIAS DE SEGURIDAD
                    try
                    {
                        EventLogQuery query = new EventLogQuery("Microsoft-Windows-Backup/Operational", PathType.LogName, "*[System[(EventID=4 or EventID=5 or EventID=14)]]");
                        query.ReverseDirection = true;
                        using (EventLogReader reader = new EventLogReader(query))
                        {
                            EventRecord record = reader.ReadEvent();
                            if (record != null)
                            {
                                miReporte.FechaUltimoBackup = record.TimeCreated?.ToString("dd/MM/yyyy HH:mm") ?? "Desconocida";
                                miReporte.EstadoBackup = (record.Id == 4 || record.Id == 14) ? "OK (Completada)" : "Fallida/Error";
                            }
                            else { miReporte.EstadoBackup = "Sin registros"; miReporte.FechaUltimoBackup = "N/A"; }
                        }
                    }
                    catch { miReporte.EstadoBackup = "No configurado / App Externa"; miReporte.FechaUltimoBackup = "N/A"; }
                }
                catch { }

                // --- IMAGEN ---
                System.Drawing.Bitmap logoBmp = Properties.Resources.copicanariasicon;
                byte[] bytesImagen;
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    logoBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    bytesImagen = ms.ToArray();
                }

                // --- DOCUMENTO ---
                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(PageSizes.A4);
                        page.Margin(1.5f, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Segoe UI"));

                        page.Header().Column(colHeader =>
                        {
                            colHeader.Item().Row(row =>
                            {
                                row.RelativeItem().Column(colTextos =>
                                {
                                    colTextos.Item().Text("INFORME DE AUDITORÍA Y MANTENIMIENTO").SemiBold().FontSize(18).FontColor(Colors.Blue.Darken4);
                                    colTextos.Item().Text("Grupo Copicanarias").FontSize(14).FontColor(Colors.Grey.Darken1);
                                    colTextos.Item().Text("Departamento de Sistemas").FontSize(11).FontColor(Colors.Grey.Medium);
                                });
                                row.ConstantItem(100).AlignRight().Height(50).Image(bytesImagen).FitArea();
                            });
                            colHeader.Item().PaddingTop(10).LineHorizontal(1.5f).LineColor(Colors.Grey.Lighten1);
                            colHeader.Item().PaddingTop(3).Text($"Fecha de emisión: {miReporte.FechaHora}").FontSize(8).FontColor(Colors.Grey.Medium).AlignRight();
                        });

                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            // SECCIÓN 1: SISTEMA
                            column.Item().PaddingBottom(5).Text("1. Información General del Sistema").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                            column.Item().PaddingBottom(10);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Nombre del Servidor:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.NombreServidor);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Sistema Operativo:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.SistemaOperativo);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Arquitectura:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.Arquitectura);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Memoria RAM Total:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.MemoriaRAM);

                                var colorEspacio = miReporte.PorcentajeLibreC < 20 ? Colors.Red.Darken2 : Colors.Black;
                                string alertaEspacio = miReporte.PorcentajeLibreC < 20 ? " (¡CRÍTICO!)" : "";
                                string textoEspacio = $"{miReporte.EspacioDiscoC} ({miReporte.PorcentajeLibreC:F1}% disponible){alertaEspacio}";

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Espacio en Disco (C:):").SemiBold();
                                if (miReporte.PorcentajeLibreC < 20) table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(textoEspacio).FontColor(colorEspacio).SemiBold();
                                else table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(textoEspacio).FontColor(colorEspacio);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Última Copia de Seguridad:").SemiBold();
                                var colorBackup = miReporte.EstadoBackup.Contains("OK") ? Colors.Green.Darken2 : (miReporte.EstadoBackup.Contains("Fall") ? Colors.Red.Darken2 : Colors.Grey.Medium);
                                string textoBackup = $"{miReporte.EstadoBackup} ({miReporte.FechaUltimoBackup})";
                                if (miReporte.EstadoBackup.Contains("Fall")) table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(textoBackup).FontColor(colorBackup).SemiBold();
                                else table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(textoBackup).FontColor(colorBackup);

                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Usuario ejecutor:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.UsuarioActivo);
                            });

                            // SECCIÓN 2: LIMPIEZA
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("2. Mantenimiento de Archivos Temporales").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                            column.Item().PaddingBottom(10);

                            string espacioStr = "0 MB";
                            double mb = miReporte.BytesLiberados / 1024.0 / 1024.0;
                            if (mb > 1024) espacioStr = $"{(mb / 1024.0):F2} GB";
                            else espacioStr = $"{mb:F2} MB";

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Estado de Limpieza:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.ArchivosBorrados > 0 ? "Ejecutada" : "No ejecutada o sin basura").FontColor(miReporte.ArchivosBorrados > 0 ? Colors.Green.Darken2 : Colors.Grey.Medium);
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Archivos Eliminados:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.ArchivosBorrados.ToString());
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Espacio Liberado:").SemiBold();
                                table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(espacioStr);
                            });

                            // SECCIÓN 3: DISCOS
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("3. Estado de Salud de Discos (S.M.A.R.T.)").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                            column.Item().PaddingBottom(10);

                            if (miReporte.Discos.Count == 0) column.Item().Text("No se ha ejecutado el análisis de hardware o no se detectaron discos.").FontColor(Colors.Grey.Medium).Italic();
                            else
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns => { columns.RelativeColumn(3); columns.RelativeColumn(1); columns.RelativeColumn(2); });
                                    table.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1).Padding(5).Text("Modelo del Disco").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1).Padding(5).Text("Interfaz").SemiBold();
                                    table.Cell().Background(Colors.Grey.Lighten3).BorderBottom(1).BorderColor(Colors.Grey.Darken1).Padding(5).Text("Estado").SemiBold();

                                    foreach (var disco in miReporte.Discos)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(disco.Modelo);
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(disco.Tipo);
                                        var colorE = disco.Estado.Contains("OK") ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(disco.Estado).FontColor(colorE).SemiBold();
                                    }
                                });
                            }

                            // SECCIÓN 4: WINDOWS UPDATE
                            column.Item().PaddingTop(15).PaddingBottom(5).Text("4. Estado de Actualizaciones (Windows Update)").FontSize(14).SemiBold().FontColor(Colors.Blue.Darken2);
                            column.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
                            column.Item().PaddingBottom(10);

                            if (!miReporte.UpdatesEjecutado) column.Item().Text("No se ha ejecutado la búsqueda de actualizaciones en esta sesión.").FontColor(Colors.Grey.Medium).Italic();
                            else
                            {
                                column.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns => { columns.RelativeColumn(); columns.RelativeColumn(); });
                                    if (miReporte.UpdatesPendientes == 0)
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Estado del Servidor:").SemiBold();
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Actualizado. No hay parches pendientes.").FontColor(Colors.Green.Darken2);
                                    }
                                    else
                                    {
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Actualizaciones Instaladas:").SemiBold();
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.UpdatesInstalados.ToString());
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text("Reinicio Requerido:").SemiBold();
                                        var colorR = miReporte.RequiereReinicio ? Colors.Orange.Darken2 : Colors.Green.Darken2;
                                        table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5).Text(miReporte.RequiereReinicio ? "SÍ (Pendiente por el administrador)" : "NO").FontColor(colorR).SemiBold();

                                        table.Cell().PaddingTop(10).Text("Parches procesados:").SemiBold();
                                        table.Cell();
                                        foreach (string nombreUpdate in miReporte.NombresUpdates)
                                        {
                                            table.Cell().PaddingLeft(10).Text("• " + nombreUpdate).FontSize(9).FontColor(Colors.Grey.Darken2);
                                            table.Cell();
                                        }
                                    }
                                });
                            }
                        });

                        page.Footer().AlignCenter().Text(x =>
                        {
                            x.Span("Página ");
                            x.CurrentPageNumber();
                            x.Span(" de ");
                            x.TotalPages();
                        });
                    });
                }).GeneratePdf(rutaArchivo);

                rtbLog.AppendText($">>> ¡ÉXITO! PDF guardado en el Escritorio.\n");
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = rutaArchivo, UseShellExecute = true });
            }
            catch (Exception ex) { rtbLog.AppendText($">>> ERROR al generar el PDF: {ex.Message}\n"); }
        }

        // FUNCIÓN AUXILIAR RECURSIVA PARA LIMPIEZA
        private (int Archivos, long Bytes) LimpiarDirectorio(string ruta)
        {
            int contadorArchivos = 0; long contadorBytes = 0;
            if (!System.IO.Directory.Exists(ruta)) return (contadorArchivos, contadorBytes);
            try
            {
                foreach (string archivo in System.IO.Directory.GetFiles(ruta))
                {
                    try
                    {
                        long tamano = new System.IO.FileInfo(archivo).Length;
                        System.IO.File.SetAttributes(archivo, System.IO.FileAttributes.Normal);
                        System.IO.File.Delete(archivo);
                        contadorArchivos++; contadorBytes += tamano;
                    }
                    catch { }
                }
                foreach (string carpeta in System.IO.Directory.GetDirectories(ruta))
                {
                    var res = LimpiarDirectorio(carpeta);
                    contadorArchivos += res.Archivos; contadorBytes += res.Bytes;
                    try { System.IO.Directory.Delete(carpeta, false); } catch { }
                }
            }
            catch { }
            return (contadorArchivos, contadorBytes);
        }
    }

    // --- ESTRUCTURAS DE DATOS ---
    public class DiscoInfo
    {
        public string Modelo { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
    }

    public class DatosServidor
    {
        public string NombreServidor { get; set; } = Environment.MachineName;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string Arquitectura { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string UsuarioActivo { get; set; } = Environment.UserName;
        public string FechaHora { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        public string MemoriaRAM { get; set; } = string.Empty;
        public string EspacioDiscoC { get; set; } = string.Empty;
        public double PorcentajeLibreC { get; set; } = 100;

        public string EstadoBackup { get; set; } = "Desconocido";
        public string FechaUltimoBackup { get; set; } = "--/--/----";

        public int ArchivosBorrados { get; set; } = 0;
        public long BytesLiberados { get; set; } = 0;

        public List<DiscoInfo> Discos { get; set; } = new List<DiscoInfo>();

        public bool UpdatesEjecutado { get; set; } = false;
        public int UpdatesPendientes { get; set; } = 0;
        public int UpdatesInstalados { get; set; } = 0;
        public bool RequiereReinicio { get; set; } = false;
        public List<string> NombresUpdates { get; set; } = new List<string>();
    }
}