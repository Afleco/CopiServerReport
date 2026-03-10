using System.Management;
using WUApiLib;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void pictureBoxLogo_Click(object sender, EventArgs e)
        {

        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }

        private void lblTituloCabecera_Click(object sender, EventArgs e)
        {

        }

        private void btnCleanTemp_Click(object sender, EventArgs e)
        {
            // 1. Escribimos en la consola que empezamos
            rtbLog.AppendText("\n>>> Iniciando limpieza de archivos temporales...\n");

            // 2. Definimos las rutas a limpiar
            string userTempPath = System.IO.Path.GetTempPath(); // Carpeta Temp del usuario
            string windowsTempPath = @"C:\Windows\Temp";        // Carpeta Temp del sistema

            // 3. Llamamos a nuestra función de limpieza (la creamos más abajo)
            int archivosBorrados = 0;
            archivosBorrados += LimpiarDirectorio(userTempPath);
            archivosBorrados += LimpiarDirectorio(windowsTempPath);

            // 4. Avisamos que terminamos
            rtbLog.AppendText($">>> Limpieza completada. Archivos/Carpetas eliminados: {archivosBorrados}\n");
        }

        // Función auxiliar que hace el trabajo sucio
        private int LimpiarDirectorio(string ruta)
        {
            int contador = 0;

            // Si la ruta no existe, salimos sin hacer nada
            if (!System.IO.Directory.Exists(ruta)) return contador;

            try
            {
                // 1. Primero, borramos todos los archivos sueltos en esta carpeta
                string[] archivos = System.IO.Directory.GetFiles(ruta);
                foreach (string archivo in archivos)
                {
                    try
                    {
                        // Le quitamos el atributo de "Solo Lectura" por si acaso Windows lo protegió
                        System.IO.File.SetAttributes(archivo, System.IO.FileAttributes.Normal);
                        System.IO.File.Delete(archivo);
                        contador++;
                    }
                    catch { /* Ignoramos si el archivo está en uso actual */ }
                }

                // 2. Luego, entramos a cada subcarpeta (Esto se llama Recursividad)
                string[] carpetas = System.IO.Directory.GetDirectories(ruta);
                foreach (string carpeta in carpetas)
                {
                    // Nos llamamos a nosotros mismos para limpiar esa subcarpeta por dentro
                    contador += LimpiarDirectorio(carpeta);

                    // Una vez limpia por dentro, intentamos borrar la carpeta en sí
                    try
                    {
                        System.IO.Directory.Delete(carpeta, false); // false porque ya debería estar vacía
                    }
                    catch { /* Ignoramos si la carpeta aún tiene algún archivo bloqueado dentro */ }
                }
            }
            catch { /* Ignoramos si Windows nos bloquea el acceso a leer la carpeta */ }

            return contador;
        }

        private void btnSmart_Click(object sender, EventArgs e)
        {
            rtbLog.AppendText("\n>>> Iniciando diagnóstico S.M.A.R.T. de discos físicos...\n");

            try
            {
                // 1. Preparamos la consulta al instrumental de Windows (WMI)
                // Le pedimos el modelo, la interfaz (SATA/NVMe) y el estado de salud
                ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Model, Status, InterfaceType FROM Win32_DiskDrive");

                int discosDetectados = 0;

                // 2. Recorremos cada disco duro que Windows encuentre en el servidor
                foreach (ManagementObject disco in searcher.Get())
                {
                    discosDetectados++;

                    // Extraemos los datos, si alguno es nulo ponemos "Desconocido"
                    string modelo = disco["Model"]?.ToString() ?? "Disco Desconocido";
                    string tipo = disco["InterfaceType"]?.ToString() ?? "Desconocido";
                    string estado = disco["Status"]?.ToString() ?? "Desconocido";

                    // Escribimos en la consola el nombre del disco
                    rtbLog.AppendText($"- Disco {discosDetectados} [{tipo}]: {modelo}\n");

                    // Evaluamos el estado S.M.A.R.T.
                    // Si el firmware del disco detecta que se va a romper pronto, el Status cambiará a "Pred Fail" o "Error"
                    if (estado.ToUpper() == "OK")
                    {
                        rtbLog.AppendText("  [+] Estado S.M.A.R.T.: OK (Saludable)\n");
                    }
                    else
                    {
                        rtbLog.AppendText($"  [-] ALERTA S.M.A.R.T.: El disco reporta problemas ({estado})\n");
                    }
                }

                if (discosDetectados == 0)
                {
                    rtbLog.AppendText(">>> ADVERTENCIA: No se detectaron discos físicos compatibles.\n");
                }
            }
            catch (Exception ex)
            {
                // Si hay algún error de permisos o el servicio WMI está caído en el servidor
                rtbLog.AppendText($">>> ERROR al leer discos: {ex.Message}\n");
            }

            rtbLog.AppendText(">>> Diagnóstico de hardware finalizado.\n");
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            // Desactivamos el botón para que el usuario no le haga clic 20 veces por desesperación
            btnUpdate.Enabled = false;
            rtbLog.AppendText("\n>>> Conectando con Windows Update. Buscando actualizaciones pendientes...\n");
            rtbLog.AppendText(">>> (Por favor, ten paciencia, esto puede tardar varios minutos)\n");

            try
            {
                // Creamos una "tarea en segundo plano" para que la ventana no se congele
                await Task.Run(() =>
                {
                    // 1. Iniciamos sesión en Windows Update
                    UpdateSession updateSession = new UpdateSession();
                    IUpdateSearcher updateSearcher = updateSession.CreateUpdateSearcher();

                    // 2. Buscamos actualizaciones que NO estén instaladas y no estén ocultas
                    ISearchResult searchResult = updateSearcher.Search("IsInstalled=0 and Type='Software' and IsHidden=0");

                    int cantidad = searchResult.Updates.Count;

                    // Como estamos en segundo plano, usamos Invoke para "pedirle permiso" a la ventana para escribir
                    this.Invoke((MethodInvoker)delegate
                    {
                        rtbLog.AppendText($">>> Se encontraron {cantidad} actualizaciones pendientes.\n");
                    });

                    if (cantidad > 0)
                    {
                        // 3. Preparamos las actualizaciones para descargar
                        UpdateCollection updatesToProcess = new UpdateCollection();
                        foreach (IUpdate update in searchResult.Updates)
                        {
                            updatesToProcess.Add(update);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rtbLog.AppendText($"  - {update.Title}\n");
                            });
                        }

                        // 4. Descargamos
                        this.Invoke((MethodInvoker)delegate { rtbLog.AppendText(">>> Descargando actualizaciones...\n"); });
                        IUpdateDownloader downloader = updateSession.CreateUpdateDownloader();
                        downloader.Updates = updatesToProcess;
                        downloader.Download();

                        // 5. Instalamos
                        this.Invoke((MethodInvoker)delegate { rtbLog.AppendText(">>> Instalando actualizaciones (MODO SILENCIOSO)...\n"); });
                        IUpdateInstaller installer = updateSession.CreateUpdateInstaller();
                        installer.Updates = updatesToProcess;

                        IInstallationResult result = installer.Install();

                        this.Invoke((MethodInvoker)delegate
                        {
                            rtbLog.AppendText($">>> Proceso finalizado. Código de resultado: {result.ResultCode}\n");

                            // Comprobamos si Windows pide reiniciar, pero NO lo reiniciamos nosotros
                            if (result.RebootRequired)
                            {
                                rtbLog.AppendText(">>> NOTA: El sistema requiere un reinicio para aplicar los cambios.\n");
                                rtbLog.AppendText(">>> REINICIO OMITIDO SEGÚN PROTOCOLO DE LA EMPRESA.\n");
                            }
                        });
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            rtbLog.AppendText(">>> El servidor está completamente actualizado.\n");
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($">>> ERROR en Windows Update: {ex.Message}\n");
            }
            finally
            {
                // Volvemos a activar el botón al terminar
                btnUpdate.Enabled = true;
            }
        }

        private void btnReport_Click(object sender, EventArgs e)
        {
            // QuestPDF requiere declarar que usamos la licencia gratuita (Community)
            QuestPDF.Settings.License = LicenseType.Community;

            // Preparamos la ruta donde se guardará: En el Escritorio del usuario actual
            string rutaEscritorio = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string fecha = DateTime.Now.ToString("dd-MM-yyyy_HH-mm");
            string rutaArchivo = System.IO.Path.Combine(rutaEscritorio, $"Mantenimiento_Copicanarias_{fecha}.pdf");

            try
            {
                rtbLog.AppendText("\n>>> Generando informe PDF...\n");

                Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        // Configuración de la página (A4, márgenes blancos)
                        page.Size(PageSizes.A4);
                        page.Margin(2, Unit.Centimetre);
                        page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Segoe UI"));

                        // 1. CABECERA
                        page.Header().Column(col =>
                        {
                            col.Item().Text("INFORME DE MANTENIMIENTO DE SERVIDOR")
                                .SemiBold().FontSize(18).FontColor(Colors.Blue.Darken2);
                            col.Item().Text("Grupo Copicanarias").FontSize(14).FontColor(Colors.Grey.Medium);
                            col.Item().LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                        });

                        // 2. CONTENIDO (Aquí volcamos lo que dice la consola negra)
                        page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                        {
                            column.Spacing(15);
                            column.Item().Text($"Fecha y Hora de ejecución: {DateTime.Now:dd/MM/yyyy HH:mm}");
                            column.Item().Text("Registro completo de operaciones:").SemiBold();

                            // Metemos el texto en un cuadro gris para que parezca código/log
                            column.Item().Background(Colors.Grey.Lighten4).Padding(15)
                                  .Text(rtbLog.Text).FontFamily("Consolas").FontSize(9);
                        });

                        // 3. PIE DE PÁGINA
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

                // Ordenamos a Windows que abra el PDF automáticamente para verlo
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = rutaArchivo,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($">>> ERROR al generar el PDF: {ex.Message}\n");
            }
        }
    }
}
