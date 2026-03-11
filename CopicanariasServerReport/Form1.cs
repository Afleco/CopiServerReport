using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WUApiLib;

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private DatosServidor miReporte = new DatosServidor();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        public Form1()
        {
            InitializeComponent();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // ── Edita aquí los técnicos del desplegable ──
            cmbTecnico.Items.Add("Técnico 1 (Soporte)");
            cmbTecnico.Items.Add("Técnico 2 (Sistemas)");
            cmbTecnico.Items.Add("Administrador Principal");
            cmbTecnico.SelectedIndex = 0;
        }



        // ─────────────────────────────────────────────────────────
        // UTILIDAD: Habilitar / deshabilitar botones
        // ─────────────────────────────────────────────────────────
        private void SetBotonesHabilitados(bool habilitado)
        {
            btnCleanTemp.Enabled = habilitado;
            btnSmart.Enabled = habilitado;
            btnUpdate.Enabled = habilitado;
            btnAbrirUpdate.Enabled = habilitado;
            btnReport.Enabled = habilitado;
            btnAuto.Enabled = habilitado;
        }

        // ─────────────────────────────────────────────────────────
        // EVENTOS DE BOTONES
        // ─────────────────────────────────────────────────────────
        private async void btnCleanTemp_Click(object sender, EventArgs e) { SetBotonesHabilitados(false); await ProcesoLimpieza(); SetBotonesHabilitados(true); }
        private async void btnSmart_Click(object sender, EventArgs e) { SetBotonesHabilitados(false); await ProcesoSmart(); SetBotonesHabilitados(true); }
        private async void btnUpdate_Click(object sender, EventArgs e) { SetBotonesHabilitados(false); await ProcesoUpdates(); SetBotonesHabilitados(true); }
        private async void btnReport_Click(object sender, EventArgs e) { SetBotonesHabilitados(false); await ProcesoPDF(); SetBotonesHabilitados(true); }

        private void btnAbrirUpdate_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("ms-settings:windowsupdate") { UseShellExecute = true });
        }

        private async void btnAuto_Click(object sender, EventArgs e)
        {
            SetBotonesHabilitados(false);
            rtbLog.AppendText("\n======================================================\n");
            rtbLog.AppendText(">>> ⚡ INICIANDO MANTENIMIENTO AUTOMÁTICO COMPLETO ⚡ <<<\n");
            rtbLog.AppendText("======================================================\n");

            if (cmbTecnico?.SelectedItem != null)
                miReporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            await ProcesoLimpieza();
            await ProcesoSmart();
            await ProcesoUpdates();
            await ProcesoPDF();

            rtbLog.AppendText("\n>>> ✅ MANTENIMIENTO AUTOMÁTICO FINALIZADO.\n");
            SetBotonesHabilitados(true);
        }

        // =========================================================
        // 1. LIMPIEZA DE TEMPORALES Y CACHÉ WINDOWS UPDATE
        // =========================================================
        private async Task ProcesoLimpieza()
        {
            rtbLog.AppendText("\n>>> [1/4] Iniciando limpieza de archivos temporales y caché...\n");
            int archivosBorrados = 0;
            long bytesLiberados = 0;

            var rutas = new[]
            {
                (Path.GetTempPath(),                            "Temp de usuario"),
                (@"C:\Windows\Temp",                           "Temp de Windows"),
                (@"C:\Windows\SoftwareDistribution\Download", "Caché de Windows Update")
            };

            foreach (var (ruta, nombre) in rutas)
            {
                int arch = 0; long bytes = 0;
                try
                {
                    (arch, bytes) = await Task.Run(() => LimpiarDirectorio(ruta));
                }
                catch (Exception ex)
                {
                    rtbLog.AppendText($"    · {nombre}: sin acceso ({ex.GetType().Name})\n");
                    continue;
                }
                archivosBorrados += arch;
                bytesLiberados += bytes;
                rtbLog.AppendText($"    · {nombre}: {arch} archivos — {bytes / 1048576.0:F1} MB\n");
            }

            miReporte.ArchivosBorrados = archivosBorrados;
            miReporte.BytesLiberados = bytesLiberados;
            rtbLog.AppendText($">>> Limpieza completada. Total: {archivosBorrados} archivos | {bytesLiberados / 1048576.0:F2} MB liberados\n");
        }

        private (int Archivos, long Bytes) LimpiarDirectorio(string ruta)
        {
            int c = 0; long b = 0;
            if (!Directory.Exists(ruta)) return (c, b);

            // ── Archivos del directorio actual ───────────────────────────────
            string[] archivos = Array.Empty<string>();
            try { archivos = Directory.GetFiles(ruta, "*", SearchOption.TopDirectoryOnly); }
            catch { return (c, b); } // Sin acceso al directorio: abortamos silenciosamente

            foreach (string archivo in archivos)
            {
                try
                {
                    var fi = new FileInfo(archivo);
                    if (fi.IsReadOnly) fi.IsReadOnly = false;
                    long tam = fi.Length;
                    File.SetAttributes(archivo, FileAttributes.Normal);
                    File.Delete(archivo);
                    c++; b += tam;
                }
                catch { } // Archivo en uso o sin permisos: saltar
            }

            // ── Subdirectorios (recursivo) ───────────────────────────────────
            string[] carpetas = Array.Empty<string>();
            try { carpetas = Directory.GetDirectories(ruta); }
            catch { return (c, b); }

            foreach (string carpeta in carpetas)
            {
                try
                {
                    var res = LimpiarDirectorio(carpeta);
                    c += res.Archivos; b += res.Bytes;
                    Directory.Delete(carpeta, false);
                }
                catch { }
            }
            return (c, b);
        }

        // =========================================================
        // 2. TEST S.M.A.R.T. — Modelo, tamaño y estado (Win32_DiskDrive)
        // =========================================================
        private async Task ProcesoSmart()
        {
            rtbLog.AppendText("\n>>> [2/4] Iniciando diagnóstico S.M.A.R.T....\n");
            miReporte.Discos.Clear();

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
                            string estadoFinal = estado.ToUpper() == "OK" ? "OK (Saludable)" : $"ALERTA ({estado})";

                            miReporte.Discos.Add(new DiscoInfo
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
                rtbLog.AppendText(">>> ⚠️  No se pudieron leer los discos del sistema.\n    Asegúrate de ejecutar la aplicación como administrador.\n");

            foreach (var d in miReporte.Discos)
                rtbLog.AppendText($"    · {d.Modelo} [{d.Tipo}, {d.TamanoGB:F0} GB] — {d.Estado}\n");
        }


        // =========================================================
        // 3. WINDOWS UPDATE (SOLO LECTURA)
        // =========================================================
        private async Task ProcesoUpdates()
        {
            rtbLog.AppendText("\n>>> [3/4] Analizando Windows Update (solo lectura)...\n");

            // WUApiLib es una librería COM que requiere estrictamente un hilo STA.
            // Task.Factory.StartNew con TaskScheduler.Default usa el ThreadPool (MTA),
            // lo que causa fallos en algunas máquinas. Usamos un hilo STA explícito.
            Exception errorCapturado = null;
            await Task.Run(() =>
            {
                Exception hiloError = null;
                var hilo = new System.Threading.Thread(() =>
                {
                    try
                    {
                        miReporte.UpdatesEjecutado = true;
                        miReporte.UpdatesImportantes = 0;
                        miReporte.UpdatesOpcionales = 0;
                        miReporte.NombresUpdates.Clear();

                        var session = new UpdateSession();
                        var searcher = session.CreateUpdateSearcher();
                        var result = searcher.Search("IsInstalled=0 and IsHidden=0");

                        foreach (IUpdate upd in result.Updates)
                        {
                            miReporte.NombresUpdates.Add(upd.Title);
                            if (upd.AutoSelectOnWebSites) miReporte.UpdatesImportantes++;
                            else miReporte.UpdatesOpcionales++;
                        }

                        // ── Detección de reinicio pendiente ──────────────────
                        miReporte.RequiereReinicio = false;

                        // Método 1: clave de registro (la más fiable y rápida)
                        try
                        {
                            using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                                @"SOFTWARE\Microsoft\Windows\CurrentVersion\WindowsUpdate\Auto Update\RebootRequired");
                            if (key != null) miReporte.RequiereReinicio = true;
                        }
                        catch { }

                        // Método 2: ISystemInformation de WUApiLib (respaldo)
                        if (!miReporte.RequiereReinicio)
                        {
                            try
                            {
                                var sysInfo = new WUApiLib.SystemInformation();
                                miReporte.RequiereReinicio = sysInfo.RebootRequired;
                            }
                            catch { }
                        }
                    }
                    catch (Exception ex) { hiloError = ex; }
                });
                hilo.SetApartmentState(System.Threading.ApartmentState.STA);
                hilo.Start();
                hilo.Join();
                if (hiloError != null) errorCapturado = hiloError;
            });

            if (errorCapturado != null)
            {
                rtbLog.AppendText(">>> ⚠️  No se pudo analizar Windows Update.\n");
                rtbLog.AppendText("    Asegúrate de ejecutar la aplicación como administrador.\n");
                return;
            }

            if (miReporte.UpdatesImportantes == 0 && miReporte.UpdatesOpcionales == 0)
                rtbLog.AppendText(">>> ✅ Sistema al día. No hay actualizaciones pendientes.\n");
            else
            {
                rtbLog.AppendText($">>> ⚠️  Pendientes: {miReporte.UpdatesImportantes} importantes | {miReporte.UpdatesOpcionales} opcionales\n");
                foreach (var nombre in miReporte.NombresUpdates.Take(5))
                    rtbLog.AppendText($"    · {nombre}\n");
                if (miReporte.NombresUpdates.Count > 5)
                    rtbLog.AppendText($"    ... y {miReporte.NombresUpdates.Count - 5} más\n");
            }

            if (miReporte.RequiereReinicio)
                rtbLog.AppendText(">>> 🔁 REINICIO PENDIENTE — Se requiere reiniciar el equipo.\n");
            else
                rtbLog.AppendText(">>> ✅ No se requiere reinicio.\n");
        }

        // =========================================================
        // 4. GENERAR PDF
        // =========================================================
        private async Task ProcesoPDF()
        {
            rtbLog.AppendText("\n>>> [4/4] Recopilando datos del sistema y preparando informe PDF...\n");

            if (cmbTecnico?.SelectedItem != null)
                miReporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            // Si el usuario genera el PDF sin haber pasado por S.M.A.R.T., lo ejecutamos aquí
            if (miReporte.Discos.Count == 0)
                await ProcesoSmart();

            // Telemetría en segundo plano para no congelar la ventana
            await Task.Run(() => RecopilarTelemetriaAvanzada());
            await RecopilarJavaAsync();

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                Title = "Guardar Informe de Mantenimiento",
                FileName = $"Informe_Sistema_{DateTime.Now:dd_MM_yyyy_HHmm}"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                rtbLog.AppendText(">>> Generación de PDF cancelada.\n");
                return;
            }

            string rutaArchivo = sfd.FileName;

            try
            {
                byte[] logoBytes;
                using (var bmp = Properties.Resources.copicanariasicon)
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    logoBytes = ms.ToArray();
                }

                await Task.Run(() => GenerarDocumentoPDF(rutaArchivo, logoBytes));

                rtbLog.AppendText($">>> ✅ PDF guardado correctamente: {rutaArchivo}\n");
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo { FileName = rutaArchivo, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                rtbLog.AppendText($">>> ⚠️  No se pudo generar el PDF.\n");
                rtbLog.AppendText($"    Causa: {ex.Message}\n");
                rtbLog.AppendText("    Asegúrate de que la ruta de destino es accesible y el archivo no está abierto.\n");
            }
        }

        // ─────────────────────────────────────────────────────────
        // CONSTRUCCIÓN DEL DOCUMENTO PDF (QuestPDF)
        // ─────────────────────────────────────────────────────────
        private void GenerarDocumentoPDF(string ruta, byte[] logoBytes)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1.5f, Unit.Centimetre);
                    page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Segoe UI"));

                    // ══ CABECERA ══════════════════════════════════════════
                    page.Header().Column(hdr =>
                    {
                        hdr.Item().Row(row =>
                        {
                            row.RelativeItem().Column(txt =>
                            {
                                txt.Item().Text("INFORME DE AUDITORÍA Y MANTENIMIENTO")
                                    .SemiBold().FontSize(15).FontColor(Colors.Blue.Darken4);
                                txt.Item().Text("Grupo Copicanarias — Dpto. Sistemas")
                                    .FontSize(11).FontColor(Colors.Grey.Darken1);
                                txt.Item().Text($"Técnico responsable: {miReporte.TecnicoResponsable}")
                                    .FontSize(9).FontColor(Colors.Grey.Darken2);
                            });
                            row.ConstantItem(100).AlignRight().Height(40).Image(logoBytes).FitArea();
                        });
                        hdr.Item().PaddingTop(5).LineHorizontal(2).LineColor(Colors.Blue.Darken3);
                        hdr.Item().PaddingTop(3).AlignRight()
                            .Text($"Fecha de emisión: {miReporte.FechaHora}   |   Equipo: {miReporte.NombreServidor}")
                            .FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    });

                    // ══ CONTENIDO ═════════════════════════════════════════
                    page.Content().PaddingVertical(0.3f, Unit.Centimetre).Column(col =>
                    {
                        // ── 1. SISTEMA Y HARDWARE ───────────────────────
                        Seccion(col, "1. Sistema y Hardware");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            Fila(t, "Hostname / OS:",
                                $"{miReporte.NombreServidor} · {miReporte.SistemaOperativo} ({miReporte.Arquitectura})");
                            Fila(t, "Memoria RAM:", miReporte.MemoriaRAM);
                            Fila(t, "Usuario activo:", miReporte.UsuarioActivo);
                        });

                        // ── 2. SEGURIDAD ─────────────────────────────────
                        Seccion(col, "2. Seguridad");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

                            Fila(t, "Antivirus:", miReporte.AntivirusNombre);

                            var cAv = (miReporte.AntivirusEstado.Contains("Activo") || miReporte.AntivirusEstado.Contains("Monitori"))
                                ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            FilaColor(t, "Estado AV:", miReporte.AntivirusEstado, cAv);

                            if (!string.IsNullOrWhiteSpace(miReporte.AntivirusRuta))
                                Fila(t, "Ruta ejecutable:", miReporte.AntivirusRuta, 8);

                            var cBackup = miReporte.EstadoBackup.Contains("OK") ? Colors.Green.Darken2
                                        : miReporte.EstadoBackup.Contains("Error") ? Colors.Red.Darken2
                                        : Colors.Grey.Darken2;
                            FilaColor(t, "Backup Windows:", $"{miReporte.EstadoBackup}  [{miReporte.FechaUltimoBackup}]", cBackup);
                        });

                        // ── 3. WINDOWS UPDATE ────────────────────────────
                        Seccion(col, "3. Windows Update");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });

                            if (!miReporte.UpdatesEjecutado)
                            {
                                Fila(t, "Estado:", "No analizado en esta sesión.");
                            }
                            else if (miReporte.UpdatesImportantes == 0 && miReporte.UpdatesOpcionales == 0)
                            {
                                FilaColor(t, "Estado:", "✅ Sistema completamente actualizado.", Colors.Green.Darken2);
                            }
                            else
                            {
                                FilaColor(t, "Estado:",
                                    $"⚠️  {miReporte.UpdatesImportantes} importantes  |  {miReporte.UpdatesOpcionales} opcionales pendientes",
                                    Colors.Orange.Darken3);

                                if (miReporte.NombresUpdates.Count > 0)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text("Actualizaciones pendientes:").SemiBold();
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Column(lc =>
                                    {
                                        foreach (var u in miReporte.NombresUpdates)
                                            lc.Item().Text($"• {u}").FontSize(8);
                                    });
                                }
                            }

                            // ── Fila de reinicio pendiente (siempre visible si se ejecutó el análisis) ──
                            if (miReporte.UpdatesEjecutado)
                            {
                                var cReboot = miReporte.RequiereReinicio ? Colors.Red.Darken2 : Colors.Green.Darken2;
                                FilaColor(t, "Reinicio pendiente:",
                                    miReporte.RequiereReinicio ? "⚠️  Sí — Se debe reiniciar el equipo" : "✅ No requerido",
                                    cReboot);
                            }
                        });

                        // ── 4. SOFTWARE CLAVE ────────────────────────────
                        Seccion(col, "4. Software Clave");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            bool javaInstalado = !string.IsNullOrEmpty(miReporte.VersionJava)
                                                 && !miReporte.VersionJava.Contains("No instalado");
                            var cJava = !javaInstalado ? Colors.Grey.Darken1
                                      : miReporte.JavaAlDia ? Colors.Green.Darken2
                                      : Colors.Orange.Darken3;
                            FilaColor(t, "Java (JRE/JDK):", miReporte.VersionJava, cJava);
                            if (!string.IsNullOrEmpty(miReporte.JavaVersionOnline))
                                Fila(t, "Última versión LTS:", miReporte.JavaVersionOnline);
                        });

                        // ── 5. LIMPIEZA ──────────────────────────────────
                        Seccion(col, "5. Limpieza de Archivos Temporales");
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                            string espacio = miReporte.BytesLiberados > 1073741824
                                ? $"{miReporte.BytesLiberados / 1073741824.0:F2} GB"
                                : $"{miReporte.BytesLiberados / 1048576.0:F2} MB";
                            Fila(t, "Archivos eliminados:", $"{miReporte.ArchivosBorrados} archivos  ({espacio} liberados)");
                            Fila(t, "Áreas limpiadas:", "Temp usuario  ·  Temp Windows  ·  Caché Windows Update");
                        });

                        // ── 6. INTERFACES DE RED ─────────────────────────
                        Seccion(col, "6. Interfaces de Red");
                        if (miReporte.InterfacesRed.Count == 0)
                        {
                            col.Item().PaddingLeft(4).Text("No se detectaron interfaces de red activas.")
                                .FontColor(Colors.Red.Medium);
                        }
                        else
                        {
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(3);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                    c.RelativeColumn(1);
                                });
                                foreach (var h in new[] { "Adaptador", "Tipo", "Velocidad", "Estado" })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                                foreach (var r in miReporte.InterfacesRed)
                                {
                                    var cEst = r.Estado == "Conectado" ? Colors.Green.Darken2 : Colors.Grey.Darken1;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(r.Nombre).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(r.Tipo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(r.Velocidad).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                        .Text(r.Estado).FontColor(cEst).FontSize(8);
                                }
                            });
                        }

                        // Unidades de red mapeadas (subsección)
                        if (miReporte.UnidadesRed.Count > 0)
                        {
                            col.Item().PaddingTop(7).PaddingBottom(2)
                                .Text("Unidades de Red Mapeadas:").SemiBold().FontSize(9);
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.ConstantColumn(45); c.RelativeColumn(); });
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Letra").SemiBold().FontSize(8);
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text("Ruta de red").SemiBold().FontSize(8);
                                foreach (var u in miReporte.UnidadesRed)
                                {
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Letra).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(u.Ruta).FontSize(8);
                                }
                            });
                        }

                        // ── 7. CONTROLADORES (DRIVERS) ───────────────────
                        Seccion(col, "7. Controladores (Drivers)");
                        if (miReporte.DriversConError.Count == 0)
                        {
                            col.Item().PaddingLeft(4)
                                .Text("✅ Todos los dispositivos funcionan correctamente.")
                                .FontColor(Colors.Green.Darken2);
                        }
                        else
                        {
                            col.Item().PaddingLeft(4)
                                .Text($"⚠️  {miReporte.DriversConError.Count} dispositivo(s) con error:")
                                .FontColor(Colors.Red.Darken2).SemiBold();
                            foreach (var d in miReporte.DriversConError)
                                col.Item().PaddingLeft(10).Text($"• {d}").FontSize(8).FontColor(Colors.Red.Darken2);
                        }

                        // ── 8. ALMACENAMIENTO ────────────────────────────
                        Seccion(col, "8. Almacenamiento");

                        // Discos físicos (S.M.A.R.T.)
                        if (miReporte.Discos.Count > 0)
                        {
                            col.Item().PaddingBottom(3).Text("Discos físicos (S.M.A.R.T.):").SemiBold().FontSize(9);
                            col.Item().Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.RelativeColumn(4); // Modelo
                                    c.RelativeColumn(1); // Interfaz
                                    c.RelativeColumn(1); // Tamaño
                                    c.RelativeColumn(2); // Estado S.M.A.R.T.
                                });
                                foreach (var h in new[] { "Modelo", "Interfaz", "Tamaño", "Estado S.M.A.R.T." })
                                    t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                                foreach (var d in miReporte.Discos)
                                {
                                    var cSmart = d.Estado.Contains("OK") ? Colors.Green.Darken2 : Colors.Red.Darken2;
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Modelo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Tipo).FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text($"{d.TamanoGB:F0} GB").FontSize(8);
                                    t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(d.Estado).FontColor(cSmart).SemiBold().FontSize(8);
                                }
                            });
                            col.Item().PaddingTop(6);
                        }

                        // Volúmenes lógicos
                        col.Item().PaddingBottom(3).Text("Volúmenes lógicos:").SemiBold().FontSize(9);
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(38);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });
                            foreach (var h in new[] { "Vol.", "Total", "Libre", "% Libre", "Uso visual" })
                                t.Cell().Background(Colors.Grey.Lighten3).Padding(3).Text(h).SemiBold().FontSize(8);

                            foreach (var d in miReporte.DiscosLogicos)
                            {
                                bool critico = d.PorcentajeLibre < 20;
                                var cDisco = critico ? Colors.Red.Darken2 : Colors.Black;
                                int bloques = Math.Clamp((int)((100 - d.PorcentajeLibre) / 10), 0, 10);
                                string barra = new string('█', bloques) + new string('░', 10 - bloques);

                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(d.Letra).SemiBold().FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.TotalGB:F1} GB").FontSize(8);
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.LibreGB:F1} GB").FontColor(cDisco).FontSize(8);
                                var pctText = t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text($"{d.PorcentajeLibre:F1}%{(critico ? " ⚠️" : "")}").FontColor(cDisco).FontSize(8);
                                if (critico) pctText.SemiBold();
                                t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3)
                                    .Text(barra).FontColor(critico ? Colors.Red.Medium : Colors.Blue.Medium).FontSize(7);
                            }
                        });
                    });

                    // ══ PIE ═══════════════════════════════════════════════
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Grupo Copicanarias · Informe de Mantenimiento Automático · Página ")
                            .FontSize(7).FontColor(Colors.Grey.Medium);
                        x.CurrentPageNumber().FontSize(7).FontColor(Colors.Grey.Medium);
                        x.Span(" de ").FontSize(7).FontColor(Colors.Grey.Medium);
                        x.TotalPages().FontSize(7).FontColor(Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(ruta);
        }

        // ─── Helpers para filas del PDF ───────────────────────────
        private static void Seccion(ColumnDescriptor col, string titulo)
        {
            col.Item().PaddingTop(10).PaddingBottom(2).Text(titulo)
                .FontSize(11).SemiBold().FontColor(Colors.Blue.Darken3);
            col.Item().LineHorizontal(1).LineColor(Colors.Blue.Darken2);
            col.Item().PaddingBottom(4);
        }

        private static void Fila(TableDescriptor t, string etiqueta, string valor, float fontSize = 9)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(etiqueta).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor).FontSize(fontSize);
        }

        private static void FilaColor(TableDescriptor t, string etiqueta, string valor, string color)
        {
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(etiqueta).SemiBold();
            t.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(3).Text(valor).FontColor(color).SemiBold();
        }

        // =========================================================
        // TELEMETRÍA AVANZADA
        // =========================================================
        private void RecopilarTelemetriaAvanzada()
        {
            // ── Sistema Operativo
            try
            {
                using var s = new ManagementObjectSearcher("SELECT Caption FROM Win32_OperatingSystem");
                foreach (ManagementObject o in s.Get())
                    using (o)
                    { miReporte.SistemaOperativo = o["Caption"]?.ToString() ?? Environment.OSVersion.ToString(); break; }
            }
            catch { miReporte.SistemaOperativo = Environment.OSVersion.ToString(); }

            // ── RAM
            try
            {
                using var s = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");
                foreach (ManagementObject cs in s.Get())
                    using (cs)
                    {
                        miReporte.MemoriaRAM = $"{Math.Round(Convert.ToInt64(cs["TotalPhysicalMemory"]) / (1024.0 * 1024.0 * 1024.0))} GB";
                        break;
                    }
            }
            catch { }

            // ── Discos lógicos
            miReporte.DiscosLogicos.Clear();
            foreach (var d in DriveInfo.GetDrives().Where(x => x.IsReady && x.DriveType == DriveType.Fixed))
            {
                double total = d.TotalSize / 1073741824.0;
                double libre = d.AvailableFreeSpace / 1073741824.0;
                miReporte.DiscosLogicos.Add(new DiscoLogicoInfo
                {
                    Letra = d.Name,
                    TotalGB = total,
                    LibreGB = libre,
                    PorcentajeLibre = total > 0 ? (libre / total) * 100 : 0
                });
            }

            // ── Antivirus (nombre, estado decodificado, ruta ejecutable)
            miReporte.AntivirusNombre = "Windows Defender (Predeterminado)";
            miReporte.AntivirusEstado = "Activo";
            miReporte.AntivirusRuta = "";
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\SecurityCenter2",
                    "SELECT displayName, productState, pathToSignedProductExe FROM AntivirusProduct");
                foreach (ManagementObject av in s.Get())
                    using (av)
                    {
                        miReporte.AntivirusNombre = av["displayName"]?.ToString() ?? miReporte.AntivirusNombre;
                        miReporte.AntivirusRuta = av["pathToSignedProductExe"]?.ToString() ?? "";
                        try
                        {
                            uint state = Convert.ToUInt32(av["productState"] ?? 0u);
                            bool activo = ((state >> 12) & 0xF) == 1;
                            bool alDia = ((state >> 4) & 0xF) != 10;
                            miReporte.AntivirusEstado = activo
                                ? (alDia ? "Activo y actualizado" : "Activo — Definiciones desactualizadas")
                                : "Deshabilitado ⚠️";
                        }
                        catch { miReporte.AntivirusEstado = "Monitorizando"; }
                        break; // primer AV registrado
                    }
            }
            catch { /* Windows Server no tiene SecurityCenter2 */ }

            // ── Interfaces de red activas
            miReporte.InterfacesRed.Clear();
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT Name, NetConnectionID, Speed, AdapterType FROM Win32_NetworkAdapter WHERE NetConnectionStatus = 2");
                foreach (ManagementObject red in s.Get())
                    using (red)
                    {
                        string nombre = red["Name"]?.ToString() ?? "Desconocido";
                        string connId = red["NetConnectionID"]?.ToString() ?? "";
                        long velBps = 0;
                        try { velBps = Convert.ToInt64(red["Speed"] ?? 0L); } catch { }
                        string velStr = velBps > 0 ? $"{velBps / 1_000_000} Mbps" : "N/A";
                        string tipoAd = red["AdapterType"]?.ToString() ?? "";
                        string tipoStr = (tipoAd.Contains("Wireless") || connId.ToLower().Contains("wi-fi") || connId.ToLower().Contains("wifi"))
                            ? "Wi-Fi" : tipoAd.Contains("Ethernet") ? "Ethernet" : "Otro";

                        miReporte.InterfacesRed.Add(new RedInfo
                        {
                            Nombre = nombre,
                            Tipo = tipoStr,
                            Velocidad = velStr,
                            Estado = "Conectado"
                        });
                    }
            }
            catch { }

            // ── Unidades de red mapeadas (con ruta UNC completa)
            miReporte.UnidadesRed.Clear();
            try
            {
                using var s = new ManagementObjectSearcher("SELECT DeviceID, ProviderName FROM Win32_MappedLogicalDisk");
                foreach (ManagementObject map in s.Get())
                    using (map)
                        miReporte.UnidadesRed.Add(new UnidadRedInfo
                        {
                            Letra = map["DeviceID"]?.ToString() ?? "",
                            Ruta = map["ProviderName"]?.ToString() ?? ""
                        });
            }
            catch { }

            // ── Drivers con error
            miReporte.DriversConError.Clear();
            try
            {
                using var s = new ManagementObjectSearcher(
                    "SELECT Name FROM Win32_PnPEntity WHERE ConfigManagerErrorCode <> 0");
                foreach (ManagementObject d in s.Get())
                    using (d)
                        miReporte.DriversConError.Add(d["Name"]?.ToString() ?? "Dispositivo desconocido");
            }
            catch { }

            // ── Estado de Backup de Windows
            RecopilarEstadoBackup();
        }

        private void RecopilarEstadoBackup()
        {
            miReporte.EstadoBackup = "No configurado";
            miReporte.FechaUltimoBackup = "--/--/----";
            try
            {
                using var s = new ManagementObjectSearcher(
                    "root\\Microsoft\\Windows\\Backup", "SELECT * FROM MSFT_WBJob");
                foreach (ManagementObject job in s.Get())
                    using (job)
                    {
                        miReporte.EstadoBackup = "OK";
                        try
                        {
                            string fecha = job["StartTime"]?.ToString();
                            if (!string.IsNullOrEmpty(fecha))
                                miReporte.FechaUltimoBackup = ManagementDateTimeConverter
                                    .ToDateTime(fecha).ToString("dd/MM/yyyy HH:mm");
                        }
                        catch { }
                        break;
                    }
            }
            catch { }
        }

        // ─── Java: detección local + consulta online (Adoptium API) ─
        private async Task RecopilarJavaAsync()
        {
            miReporte.VersionJava = "No instalado / No detectado";
            miReporte.JavaAlDia = true;
            miReporte.JavaVersionOnline = "";
            string versionInstalada = "";

            string[] registryPaths =
            {
                @"SOFTWARE\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\JavaSoft\JDK",
                @"SOFTWARE\WOW6432Node\JavaSoft\Java Runtime Environment",
                @"SOFTWARE\WOW6432Node\JavaSoft\JDK"
            };

            foreach (var keyPath in registryPaths)
            {
                try
                {
                    using var key = Registry.LocalMachine.OpenSubKey(keyPath);
                    if (key == null) continue;
                    var ver = key.GetValue("CurrentVersion")?.ToString();
                    if (!string.IsNullOrEmpty(ver))
                    {
                        versionInstalada = ver;
                        miReporte.VersionJava = $"Instalado — Versión {ver}";
                        break;
                    }
                }
                catch { }
            }

            if (string.IsNullOrEmpty(versionInstalada))
            {
                rtbLog.AppendText("    · Java: No detectado en el registro.\n");
                return;
            }

            // Consulta a la API de Adoptium para obtener la última LTS disponible
            try
            {
                rtbLog.AppendText("    · Java: Consultando última versión LTS disponible online...\n");
                string json = await _http.GetStringAsync("https://api.adoptium.net/v3/info/available_releases");
                using var doc = JsonDocument.Parse(json);
                int ltsOnline = doc.RootElement.GetProperty("most_recent_lts").GetInt32();
                miReporte.JavaVersionOnline = $"Java {ltsOnline} LTS (Fuente: Adoptium)";

                // Extraer major version instalada (1.8 → 8, 11 → 11, etc.)
                int majorInstalado = 0;
                var partes = versionInstalada.Split('.');
                if (partes[0] == "1" && partes.Length > 1) int.TryParse(partes[1], out majorInstalado);
                else int.TryParse(partes[0], out majorInstalado);

                if (majorInstalado >= ltsOnline)
                {
                    miReporte.JavaAlDia = true;
                    miReporte.VersionJava += " ✅";
                }
                else
                {
                    miReporte.JavaAlDia = false;
                    miReporte.VersionJava += $" ⚠️ (disponible Java {ltsOnline} LTS)";
                }

                rtbLog.AppendText($"    · Java instalado: {versionInstalada} | LTS disponible: Java {ltsOnline}\n");
            }
            catch
            {
                miReporte.JavaVersionOnline = "No se pudo verificar (sin conexión o error de red)";
                rtbLog.AppendText("    · Java: No se pudo consultar versión online.\n");
            }
        }
    }

    // =========================================================
    // MODELOS DE DATOS
    // =========================================================


    public class DiscoInfo
    {
        public string Modelo { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Estado { get; set; } = "";
        public double TamanoGB { get; set; } = 0;
    }

    public class DiscoLogicoInfo
    {
        public string Letra { get; set; } = "";
        public double TotalGB { get; set; } = 0;
        public double LibreGB { get; set; } = 0;
        public double PorcentajeLibre { get; set; } = 0;
    }

    public class RedInfo
    {
        public string Nombre { get; set; } = "";
        public string Tipo { get; set; } = "";
        public string Velocidad { get; set; } = "";
        public string Estado { get; set; } = "";
    }

    public class UnidadRedInfo
    {
        public string Letra { get; set; } = "";
        public string Ruta { get; set; } = "";
    }

    public class DatosServidor
    {
        public string TecnicoResponsable { get; set; } = "No asignado";
        public string NombreServidor { get; set; } = Environment.MachineName;
        public string SistemaOperativo { get; set; } = string.Empty;
        public string Arquitectura { get; set; } = Environment.Is64BitOperatingSystem ? "x64" : "x86";
        public string UsuarioActivo { get; set; } = Environment.UserName;
        public string FechaHora { get; set; } = DateTime.Now.ToString("dd/MM/yyyy HH:mm");
        public string MemoriaRAM { get; set; } = string.Empty;

        public string EstadoBackup { get; set; } = "No configurado";
        public string FechaUltimoBackup { get; set; } = "--/--/----";

        public int ArchivosBorrados { get; set; } = 0;
        public long BytesLiberados { get; set; } = 0;

        public List<DiscoInfo> Discos { get; set; } = new();
        public List<DiscoLogicoInfo> DiscosLogicos { get; set; } = new();

        public bool UpdatesEjecutado { get; set; } = false;
        public int UpdatesImportantes { get; set; } = 0;
        public int UpdatesOpcionales { get; set; } = 0;
        public int UpdatesInstalados { get; set; } = 0;
        public bool RequiereReinicio { get; set; } = false;
        public List<string> NombresUpdates { get; set; } = new();

        public string AntivirusNombre { get; set; } = "";
        public string AntivirusEstado { get; set; } = "";
        public string AntivirusRuta { get; set; } = "";

        public List<RedInfo> InterfacesRed { get; set; } = new();
        public List<UnidadRedInfo> UnidadesRed { get; set; } = new();
        public List<string> DriversConError { get; set; } = new();

        public string VersionJava { get; set; } = "";
        public string JavaVersionOnline { get; set; } = "";
        public bool JavaAlDia { get; set; } = false; // false por defecto: se activa solo si se detecta y está al día
    }
}