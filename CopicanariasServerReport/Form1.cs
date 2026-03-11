using System;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CopicanariasServerReport.Pdf;
using CopicanariasServerReport.Services;
using QuestPDF.Infrastructure;
using WinColor = System.Drawing.Color; // alias para evitar ambigüedad con QuestPDF.Infrastructure.Color

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private readonly DatosServidor _reporte = new DatosServidor();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        // Colores del log reutilizados en varios métodos
        private static readonly WinColor ClrFondo = WinColor.FromArgb(238, 241, 248); // fondo log
        private static readonly WinColor ClrTexto = WinColor.FromArgb(40, 40, 40);  // texto base
        private static readonly WinColor ClrSeccion = WinColor.FromArgb(28, 78, 170);  // azul corporativo
        private static readonly WinColor ClrOk = WinColor.FromArgb(20, 120, 55);  // verde éxito
        private static readonly WinColor ClrAviso = WinColor.FromArgb(170, 90, 0); // naranja aviso
        private static readonly WinColor ClrError = WinColor.FromArgb(180, 20, 20); // rojo error
        private static readonly WinColor ClrDetalle = WinColor.FromArgb(65, 65, 75);  // detalle normal
        private static readonly WinColor ClrSubdetalle = WinColor.FromArgb(120, 120, 130); // texto secundario

        public Form1()
        {
            InitializeComponent();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");
        }

        // ── Al cargar el formulario ───────────────────────────────────
        private async void Form1_Load(object sender, EventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            // Estilo del log
            rtbLog.BackColor = ClrFondo;
            rtbLog.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            rtbLog.ForeColor = ClrTexto;
            rtbLog.Clear();

            // ── Edita aquí los técnicos del desplegable ──
            cmbTecnico.Items.Add("Himar Bautista");
            cmbTecnico.Items.Add("Mencey Medina");
            cmbTecnico.Items.Add("Alejandro Martel");
            cmbTecnico.Items.Add("Aarón Ojeda");
            cmbTecnico.Items.Add("Francisco Muñoz");
            cmbTecnico.SelectedIndex = 0;

            await EscaneoInicialAsync();
        }

        // ═════════════════════════════════════════════════════════════
        // SISTEMA DE LOG VISUAL
        // ═════════════════════════════════════════════════════════════

        // Escribe texto detectando el tipo de mensaje por su contenido.
        // Ningún servicio necesita cambiar su firma para que funcione.
        private void Log(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return;

            string linea = texto.TrimEnd('\n', '\r');

            // Éxito
            if (linea.Contains("✅"))
            {
                bool bold = linea.TrimStart().StartsWith(">>>");
                Escribir(texto, ClrOk, bold, 9.5f);
                return;
            }

            // Advertencia o reinicio
            if (linea.Contains("⚠️") || linea.Contains("🔁"))
            {
                bool bold = linea.TrimStart().StartsWith(">>>");
                Escribir(texto, ClrAviso, bold, 9.5f);
                return;
            }

            // Acción completa (⚡)
            if (linea.Contains("⚡"))
            {
                Escribir(texto, WinColor.FromArgb(100, 30, 180), true, 9.5f);
                return;
            }

            // Error / sin acceso / ALERTA
            if (linea.Contains("sin acceso") ||
                linea.Contains("No se pudo") ||
                linea.Contains("ALERTA"))
            {
                Escribir(texto, ClrError, false, 9.5f);
                return;
            }

            // Encabezado de sección >>>
            if (linea.TrimStart().StartsWith(">>>"))
            {
                Escribir(texto, ClrSeccion, true, 9.5f);
                return;
            }

            // Detalle · ítem
            if (linea.TrimStart().StartsWith("·") || linea.StartsWith("    ·"))
            {
                Escribir(texto, ClrDetalle, false, 9f);
                return;
            }

            // Sub-detalle (indentado o código de error)
            if (linea.StartsWith("      ") || linea.TrimStart().StartsWith("Código"))
            {
                Escribir(texto, ClrSubdetalle, false, 8.5f);
                return;
            }

            // Texto informativo genérico
            Escribir(texto, ClrTexto, false, 9.5f);
        }

        // Escribe un banner de color sólido como cabecera de sección.
        // Sustituye a los caracteres Unicode ╔ ║ ╚ que renderizan mal.
        private void LogBanner(string titulo, WinColor fondo, WinColor texto)
        {
            rtbLog.SuspendLayout();

            // Línea en blanco antes del banner
            Escribir("\n", ClrTexto, false, 9.5f);

            // Texto del banner centrado y rellenado con espacios
            string contenido = $"  {titulo}  ".PadRight(64);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionBackColor = fondo;
            rtbLog.SelectionColor = texto;
            rtbLog.SelectionFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            rtbLog.AppendText(contenido + "\n");

            // Restaurar
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = ClrTexto;
            rtbLog.SelectionFont = rtbLog.Font;

            // Línea en blanco después
            Escribir("\n", ClrTexto, false, 9.5f);

            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        // Escritura base con color y tamaño concretos
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

        // ── Habilitar / deshabilitar botones durante operaciones ─────
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
                LogBanner("✅   Sistema en buen estado — listo para generar el informe",
                    WinColor.FromArgb(20, 120, 55), WinColor.White);

            SetBotonesHabilitados(true);
        }

        // ─────────────────────────────────────────────────────────────
        // EVENTOS DE BOTONES
        // ─────────────────────────────────────────────────────────────
        private async void btnCleanTemp_Click(object sender, EventArgs e)
        { SetBotonesHabilitados(false); await ProcesoLimpieza(); SetBotonesHabilitados(true); }

        private async void btnSmart_Click(object sender, EventArgs e)
        { SetBotonesHabilitados(false); await ProcesoSmart(); SetBotonesHabilitados(true); }

        private async void btnUpdate_Click(object sender, EventArgs e)
        { SetBotonesHabilitados(false); await ProcesoUpdates(); SetBotonesHabilitados(true); }

        private async void btnDrivers_Click(object sender, EventArgs e)
        { SetBotonesHabilitados(false); await ProcesoDrivers(); SetBotonesHabilitados(true); }

        private async void btnReport_Click(object sender, EventArgs e)
        { SetBotonesHabilitados(false); await ProcesoPDF(); SetBotonesHabilitados(true); }

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
                byte[] logoBytes;
                using (var bmp = Properties.Resources.copicanariasicon)
                using (var ms = new MemoryStream())
                {
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    logoBytes = ms.ToArray();
                }

                await Task.Run(() => PdfGenerator.Generar(rutaArchivo, _reporte, logoBytes));

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