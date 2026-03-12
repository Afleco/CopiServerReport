using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CopicanariasServerReport.Pdf;
using CopicanariasServerReport.Services;
using QuestPDF.Infrastructure;
using WinColor = System.Drawing.Color;  // alias para evitar ambigüedad con QuestPDF.Infrastructure.Color
using WinSize = System.Drawing.Size;   // alias para evitar ambigüedad con QuestPDF.Infrastructure.Size

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private readonly DatosServidor _reporte = new DatosServidor();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Controles de certificados creados dinámicamente
        private readonly List<(TextBox Nombre, DateTimePicker Fecha)> _certControls = new();

        // Colores del log
        private static readonly WinColor ClrFondo = WinColor.FromArgb(238, 241, 248);
        private static readonly WinColor ClrTexto = WinColor.FromArgb(40, 40, 40);
        private static readonly WinColor ClrSeccion = WinColor.FromArgb(28, 78, 170);
        private static readonly WinColor ClrOk = WinColor.FromArgb(20, 120, 55);
        private static readonly WinColor ClrAviso = WinColor.FromArgb(170, 90, 0);
        private static readonly WinColor ClrError = WinColor.FromArgb(180, 20, 20);
        private static readonly WinColor ClrDetalle = WinColor.FromArgb(65, 65, 75);
        private static readonly WinColor ClrSubdetalle = WinColor.FromArgb(120, 120, 130);

        public Form1()
        {
            InitializeComponent();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");
        }

        // ── Carga del formulario ─────────────────────────────────────
        private async void Form1_Load(object sender, EventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Centrar horizontalmente en el área de trabajo y subir un 15%
            // para que al expandirse con el panel DF no quede bajo la barra de tareas
            var workArea = Screen.GetWorkingArea(this);
            this.Location = new System.Drawing.Point(
                workArea.Left + (workArea.Width - this.Width) / 2,
                workArea.Top + (int)((workArea.Height - this.Height) * 0.30)
            );

            rtbLog.BackColor = ClrFondo;
            rtbLog.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            rtbLog.ForeColor = ClrTexto;
            rtbLog.Clear();

            // ── Edita aquí los técnicos del desplegable ──────────────
            cmbTecnico.Items.Add("— Seleccione un técnico —");
            // Técnicos de Sistemas
            cmbTecnico.Items.Add("Alejandro Martel");
            cmbTecnico.Items.Add("Himar Bautista");
            cmbTecnico.Items.Add("Mencey Medina");
            // Técnicos DF-Server (deben contener el texto "DF-Server")
            cmbTecnico.Items.Add("Aarón Ojeda (DF-Server)");
            cmbTecnico.Items.Add("Francisco Muñoz (DF-Server)");
            cmbTecnico.SelectedIndex = 0;

            await EscaneoInicialAsync();
        }

        // ═════════════════════════════════════════════════════════════
        // PANEL DF-SERVER — mostrar / ocultar según técnico seleccionado
        // ═════════════════════════════════════════════════════════════
        private void cmbTecnico_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Ignorar el placeholder "— Seleccione un técnico —"
            if (cmbTecnico.SelectedIndex <= 0) { MostrarPanelDf(false); return; }

            bool esDf = cmbTecnico.SelectedItem?.ToString().Contains("DF-Server") == true;
            MostrarPanelDf(esDf);
        }

        private void MostrarPanelDf(bool mostrar)
        {
            panelDF.Visible = mostrar;
            if (!mostrar) LimpiarDatos();
            RecalcularAltura();
        }

        // Fuente de verdad única para todas las alturas del formulario.
        // Orden visual: log/botones izquierda → panel DF (si aplica) → Generar/Realizar
        private void RecalcularAltura()
        {
            // El contenido fijo termina donde acaba el log (Bottom del rtbLog)
            int contenidoBottom = rtbLog.Bottom; // = 110 + 385 = 495

            if (!panelDF.Visible)
            {
                int yBotones = contenidoBottom + 12;
                btnReport.Top = yBotones;
                btnAuto.Top = yBotones;
                this.ClientSize = new WinSize(800, yBotones + btnReport.Height + 40);
                return;
            }

            // Altura dinámica del panel DF
            int alturaPanel = 38 + 3 * 32 + 12; // título + 3 filas + margen ≈ 122px
            if (panelCertsDinamico.Visible)
                alturaPanel += panelCertsDinamico.Height + 14;

            panelDF.Height = alturaPanel;
            panelDF.Top = contenidoBottom + 8;   // pegado al log sin hueco

            int yBotonesDF = panelDF.Bottom + 8;
            btnReport.Top = yBotonesDF;
            btnAuto.Top = yBotonesDF;
            this.ClientSize = new WinSize(800, yBotonesDF + btnReport.Height + 40);
        }

        private void LimpiarDatos()
        {
            chkDigitalizacion.Checked = false;
            chkFirmas.Checked = false;
            chkCertificados.Checked = false;
            numFirmas.Value = 0;
            numCertificados.Value = 1;
            RebuildCertificadoFields(0);
        }

        // ── Checkbox Digitalización ──────────────────────────────────
        private void chkDigitalizacion_CheckedChanged(object sender, EventArgs e)
        {
            if (!chkDigitalizacion.Checked) return;

            var resp = MessageBox.Show(
                "¿La digitalización certificada está configurada y funcionando correctamente?",
                "Digitalización certificada",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button1);

            if (resp == DialogResult.No)
            {
                chkDigitalizacion.CheckedChanged -= chkDigitalizacion_CheckedChanged;
                chkDigitalizacion.Checked = false;
                chkDigitalizacion.CheckedChanged += chkDigitalizacion_CheckedChanged;
            }
        }

        // ── Checkbox Firmas ──────────────────────────────────────────
        private void chkFirmas_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkFirmas.Checked;
            lblFirmasRestantes.Enabled = activo;
            numFirmas.Enabled = activo;
            if (!activo) numFirmas.Value = 0;
        }

        // ── Checkbox Certificados ────────────────────────────────────
        private void chkCertificados_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkCertificados.Checked;
            lblNumCerts.Enabled = activo;
            numCertificados.Enabled = activo;
            panelCertsDinamico.Visible = activo;

            RebuildCertificadoFields(activo ? (int)numCertificados.Value : 0);
        }

        // ── Cambio en el número de certificados ──────────────────────
        private void numCertificados_ValueChanged(object sender, EventArgs e)
        {
            if (chkCertificados.Checked)
                RebuildCertificadoFields((int)numCertificados.Value);
        }

        // ── Genera dinámicamente las filas de certificados ───────────
        private void RebuildCertificadoFields(int cantidad)
        {
            panelCertsDinamico.Controls.Clear();
            _certControls.Clear();

            if (cantidad == 0)
            {
                RecalcularAltura();
                return;
            }

            const int rowH = 34;
            const int maxH = 170;
            int totalH = cantidad * rowH + 8;
            panelCertsDinamico.Height = Math.Min(Math.Max(totalH, 44), maxH);

            // Primero ajustar el tamaño del panel, luego recalcular el formulario
            RecalcularAltura();

            for (int i = 0; i < cantidad; i++)
            {
                int y = 4 + i * rowH;

                var lblN = new Label
                {
                    Text = $"Certificado {i + 1}:",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = WinColor.FromArgb(28, 60, 160),
                    Location = new Point(6, y + 6),
                    Size = new WinSize(100, 18),
                    AutoSize = false
                };

                var txtNombre = new TextBox
                {
                    PlaceholderText = "Nombre del certificado",
                    Font = new Font("Segoe UI", 9f),
                    Location = new Point(110, y + 4),
                    Size = new WinSize(300, 24),
                    BackColor = WinColor.White
                };

                var lblF = new Label
                {
                    Text = "Caduca:",
                    Font = new Font("Segoe UI", 8.5f),
                    ForeColor = WinColor.DimGray,
                    Location = new Point(420, y + 6),
                    Size = new WinSize(55, 18),
                    AutoSize = false
                };

                var dtp = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Font = new Font("Segoe UI", 9f),
                    Location = new Point(478, y + 3),
                    Size = new WinSize(140, 24),
                    Value = DateTime.Today.AddYears(1)
                };

                panelCertsDinamico.Controls.AddRange(new Control[] { lblN, txtNombre, lblF, dtp });
                _certControls.Add((txtNombre, dtp));
            }
        }

        // ── Lee los campos DF del panel y los guarda en _reporte ─────
        private void LeerDatosDF()
        {
            _reporte.EsTecnicoDf = panelDF.Visible;
            if (!panelDF.Visible) return;

            var df = _reporte.DfServer;
            df.DigitalizacionCertificada = chkDigitalizacion.Checked;
            df.TieneFirmas = chkFirmas.Checked;
            df.FirmasRestantes = chkFirmas.Checked ? (int)numFirmas.Value : 0;
            df.TieneCertificados = chkCertificados.Checked;
            df.Certificados.Clear();

            if (chkCertificados.Checked)
            {
                foreach (var (txt, dtp) in _certControls)
                {
                    df.Certificados.Add(new CertificadoDigital
                    {
                        Nombre = txt.Text.Trim().Length > 0
                                         ? txt.Text.Trim()
                                         : $"Certificado {df.Certificados.Count + 1}",
                        FechaCaducidad = dtp.Value.Date
                    });
                }
            }
        }

        // ── Validaciones DF antes de generar el PDF ──────────────────
        // Devuelve true si se puede continuar, false si se cancela.
        private bool ValidarCamposDf()
        {
            if (!_reporte.EsTecnicoDf) return true;

            var df = _reporte.DfServer;

            // Validar cada certificado caducado o próximo a caducar
            foreach (var cert in df.Certificados)
            {
                if (!cert.ProximoACaducar) continue;

                int dias = (cert.FechaCaducidad.Date - DateTime.Today).Days;
                bool caducado = dias < 0;

                string mensaje = caducado
                    ? $"❌  El certificado \"{cert.Nombre}\" caducó el " +
                      $"{cert.FechaCaducidad:dd/MM/yyyy} (hace {Math.Abs(dias)} días).\n\n" +
                      $"¿Se le ha avisado al cliente de que este certificado está caducado?"
                    : $"⚠️  El certificado \"{cert.Nombre}\" caduca el " +
                      $"{cert.FechaCaducidad:dd/MM/yyyy} (en {dias} días).\n\n" +
                      $"¿Se le ha avisado al cliente de que este certificado va a caducar próximamente?";

                string titulo = caducado ? "Certificado caducado" : "Certificado próximo a caducar";

                var resp = MessageBox.Show(
                    mensaje, titulo,
                    MessageBoxButtons.YesNo,
                    caducado ? MessageBoxIcon.Error : MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (resp == DialogResult.No)
                {
                    string motivo = caducado
                        ? "el cliente no ha sido avisado del certificado caducado"
                        : "el cliente no ha sido avisado del certificado próximo a caducar";
                    Log($">>> Informe cancelado: {motivo}.\n");
                    return false;
                }
            }

            // Validar firmas de DF-Signature
            if (df.TieneFirmas && df.FirmasRestantes <= 100)
            {
                var resp = MessageBox.Show(
                    $"⚠️  Quedan solo {df.FirmasRestantes} firmas de DF-Signature disponibles.\n\n" +
                    $"¿Se le ha avisado al cliente de que le quedan pocas firmas de DF-Signature?",
                    "Pocas firmas disponibles",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);

                if (resp == DialogResult.No)
                {
                    Log(">>> Informe cancelado: el cliente no ha sido avisado de las pocas firmas restantes.\n");
                    return false;
                }
            }

            return true;
        }

        // ═════════════════════════════════════════════════════════════
        // SISTEMA DE LOG VISUAL
        // ═════════════════════════════════════════════════════════════
        private void Log(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;
            string linea = texto.TrimEnd('\n', '\r');

            if (linea.Contains("✅"))
            { Escribir(texto, ClrOk, linea.TrimStart().StartsWith(">>>"), 9.5f); return; }

            if (linea.Contains("⚠️") || linea.Contains("🔁"))
            { Escribir(texto, ClrAviso, linea.TrimStart().StartsWith(">>>"), 9.5f); return; }

            if (linea.Contains("⚡"))
            { Escribir(texto, WinColor.FromArgb(100, 30, 180), true, 9.5f); return; }

            if (linea.Contains("sin acceso") ||
                linea.Contains("No se pudo") ||
                linea.Contains("ALERTA"))
            { Escribir(texto, ClrError, false, 9.5f); return; }

            if (linea.TrimStart().StartsWith(">>>"))
            { Escribir(texto, ClrSeccion, true, 9.5f); return; }

            if (linea.TrimStart().StartsWith("·") || linea.StartsWith("    ·"))
            { Escribir(texto, ClrDetalle, false, 9f); return; }

            if (linea.StartsWith("      ") || linea.TrimStart().StartsWith("Código"))
            { Escribir(texto, ClrSubdetalle, false, 8.5f); return; }

            Escribir(texto, ClrTexto, false, 9.5f);
        }

        private void LogBanner(string titulo, WinColor fondo, WinColor texto)
        {
            rtbLog.SuspendLayout();
            Escribir("\n", ClrTexto, false, 9.5f);
            string contenido = $"  {titulo}  ".PadRight(64);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionBackColor = fondo;
            rtbLog.SelectionColor = texto;
            rtbLog.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            rtbLog.AppendText(contenido + "\n");
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = ClrTexto;
            rtbLog.SelectionFont = rtbLog.Font;
            Escribir("\n", ClrTexto, false, 9.5f);
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        private void Escribir(string texto, WinColor color, bool bold, float size)
        {
            rtbLog.SuspendLayout();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = color;
            rtbLog.SelectionFont = new Font("Segoe UI", size,
                bold ? FontStyle.Bold : FontStyle.Regular);
            rtbLog.AppendText(texto);
            rtbLog.SelectionColor = ClrTexto;
            rtbLog.SelectionFont = rtbLog.Font;
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        // ── Habilitar / deshabilitar botones ─────────────────────────
        private void SetBotonesHabilitados(bool habilitado)
        {
            btnCleanTemp.Enabled = habilitado;
            btnSmart.Enabled = habilitado;
            btnUpdate.Enabled = habilitado;
            btnDrivers.Enabled = habilitado;
            btnAbrirUpdate.Enabled = habilitado;
            btnDeviceManager.Enabled = habilitado;
            btnReport.Enabled = habilitado;
            btnAuto.Enabled = habilitado;
            // Los controles DF solo se activan cuando no hay operación en curso
            panelDF.Enabled = habilitado;
        }

        // ─────────────────────────────────────────────────────────────
        // ESCANEO AUTOMÁTICO AL ARRANCAR
        // ─────────────────────────────────────────────────────────────
        private async Task EscaneoInicialAsync()
        {
            SetBotonesHabilitados(false);

            LogBanner("DIAGNÓSTICO INICIAL DEL SISTEMA",
                WinColor.FromArgb(28, 78, 170), WinColor.White);

            Log("  Revisando el estado del equipo antes de comenzar...\n\n");

            Log(">>> Comprobando actualizaciones de Windows...\n");
            await UpdateService.AnalizarAsync(_reporte, Log);

            Log("\n>>> Comprobando controladores del sistema...\n");
            _reporte.Drivers.Clear();
            var drivers = await DriverService.EscanearAsync(Log);
            _reporte.Drivers.AddRange(drivers);

            bool hayProblemas = _reporte.UpdatesImportantes > 0
                             || _reporte.RequiereReinicio
                             || _reporte.Drivers.Count > 0;

            if (hayProblemas)
                LogBanner("⚠️   Se han detectado elementos que requieren atención",
                    WinColor.FromArgb(170, 90, 0), WinColor.White);
            else
                LogBanner("✅   Sistema actualizado",
                    WinColor.FromArgb(20, 120, 55), WinColor.White);

            SetBotonesHabilitados(true);
        }

        // ─────────────────────────────────────────────────────────────
        // EVENTOS DE BOTONES
        // ─────────────────────────────────────────────────────────────

        // Devuelve true si hay un técnico real seleccionado.
        // Si no, muestra un aviso y devuelve false.
        private bool TecnicoSeleccionado()
        {
            if (cmbTecnico.SelectedIndex > 0) return true;

            MessageBox.Show(
                "Por favor, selecciona un técnico responsable antes de continuar.",
                "Técnico no seleccionado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        private async void btnCleanTemp_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoLimpieza(); SetBotonesHabilitados(true);
        }

        private async void btnSmart_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoSmart(); SetBotonesHabilitados(true);
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoUpdates(); SetBotonesHabilitados(true);
        }

        private async void btnDrivers_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoDrivers(); SetBotonesHabilitados(true);
        }

        private async void btnReport_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoPDF(); SetBotonesHabilitados(true);
        }

        private void btnAbrirUpdate_Click(object sender, EventArgs e) =>
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("ms-settings:windowsupdate")
                { UseShellExecute = true });

        private void btnDeviceManager_Click(object sender, EventArgs e) =>
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo("devmgmt.msc")
                { UseShellExecute = true });

        private async void btnAuto_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false);
            LogBanner("⚡   MANTENIMIENTO AUTOMÁTICO COMPLETO",
                WinColor.FromArgb(100, 30, 180), WinColor.White);

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            await ProcesoLimpieza();
            await ProcesoSmart();
            await ProcesoUpdates();
            await ProcesoDrivers();
            await ProcesoPDF();

            LogBanner("✅   Mantenimiento automático finalizado",
                WinColor.FromArgb(20, 120, 55), WinColor.White);

            SetBotonesHabilitados(true);
        }

        // ─────────────────────────────────────────────────────────────
        // 1. LIMPIEZA
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoLimpieza()
        {
            LogBanner("LIMPIEZA DE ARCHIVOS TEMPORALES",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

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
                try { (arch, bytes) = await Task.Run(() => LimpiezaService.LimpiarDirectorio(ruta)); }
                catch (Exception ex)
                {
                    Log($"    · {nombre}: sin acceso ({ex.GetType().Name})\n");
                    continue;
                }
                archivosBorrados += arch;
                bytesLiberados += bytes;
                Log($"    · {nombre}: {arch} archivos — {bytes / 1048576.0:F1} MB\n");
            }

            _reporte.ArchivosBorrados = archivosBorrados;
            _reporte.BytesLiberados = bytesLiberados;
            _reporte.LimpiezaEjecutada = true;
            Log($">>> ✅ Limpieza completada: {archivosBorrados} archivos | {bytesLiberados / 1048576.0:F2} MB liberados\n");
        }

        // ─────────────────────────────────────────────────────────────
        // 2. S.M.A.R.T.
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoSmart()
        {
            LogBanner("DIAGNÓSTICO S.M.A.R.T. DE DISCOS",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _reporte.Discos.Clear();
            var discos = await SmartService.ObtenerDiscosAsync(Log);
            _reporte.Discos.AddRange(discos);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. WINDOWS UPDATE
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoUpdates()
        {
            LogBanner("ANÁLISIS DE WINDOWS UPDATE",
                WinColor.FromArgb(50, 60, 100), WinColor.White);
            await UpdateService.AnalizarAsync(_reporte, Log);
        }

        // ─────────────────────────────────────────────────────────────
        // 4. DRIVERS
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoDrivers()
        {
            LogBanner("ANÁLISIS DE CONTROLADORES",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _reporte.Drivers.Clear();
            var drivers = await DriverService.EscanearAsync(Log);
            _reporte.Drivers.AddRange(drivers);
        }

        // ─────────────────────────────────────────────────────────────
        // 5. GENERAR PDF
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoPDF()
        {
            LogBanner("PREPARANDO INFORME PDF",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            // Leer campos DF y validar antes de continuar
            LeerDatosDF();
            if (!ValidarCamposDf()) return;

            if (_reporte.Discos.Count == 0)
                await ProcesoSmart();

            Log(">>> Recopilando telemetría del sistema...\n");
            await Task.Run(() => TelemetriaService.RecopilarTelemetria(_reporte));
            await TelemetriaService.RecopilarJavaAsync(_reporte, Log, _http);

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                Title = "Guardar Informe de Mantenimiento",
                FileName = $"Informe_Sistema_{DateTime.Now:dd_MM_yyyy_HHmm}"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
            {
                Log(">>> Generación de PDF cancelada.\n");
                return;
            }

            string rutaArchivo = sfd.FileName;
            try
            {
                // (cabecera del informe)
                byte[] logoBytes;
                using (var bmp = Properties.Resources.copicanariasicon)
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    logoBytes = ms.ToArray();
                }

                // (cabecera sección 9, solo si el técnico es DF)
                byte[] dfLogoBytes = null;
                if (_reporte.EsTecnicoDf)
                {
                    try
                    {
                        using var bmpDf = Properties.Resources.DF_SERVER_logo_300x60;
                        using var msDf = new MemoryStream();
                        bmpDf.Save(msDf, System.Drawing.Imaging.ImageFormat.Png);
                        dfLogoBytes = msDf.ToArray();
                    }
                    catch { /* Si el recurso no existe, el logo simplemente no aparece */ }
                }

                await Task.Run(() => PdfGenerator.Generar(rutaArchivo, _reporte, logoBytes, dfLogoBytes));

                Log($">>> ✅ PDF guardado en: {rutaArchivo}\n");
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo
                    { FileName = rutaArchivo, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log(">>> No se pudo generar el PDF.\n");
                Log($"      Causa: {ex.Message}\n");
                Log("      Comprueba que la ruta es accesible y el archivo no está abierto.\n");
            }
        }
    }
}