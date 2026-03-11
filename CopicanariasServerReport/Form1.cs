using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using CopicanariasServerReport.Pdf;
using CopicanariasServerReport.Services;
using QuestPDF.Infrastructure;

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private readonly DatosServidor _reporte = new DatosServidor();
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

        // ── Escribir en el log de la UI ──────────────────────────────
        // Todos los servicios reciben este método como callback para
        // que Form1 sea el único responsable de tocar la interfaz.
        private void Log(string texto) => rtbLog.AppendText(texto);

        // ── Habilitar / deshabilitar botones durante operaciones ─────
        private void SetBotonesHabilitados(bool habilitado)
        {
            btnCleanTemp.Enabled = habilitado;
            btnSmart.Enabled = habilitado;
            btnUpdate.Enabled = habilitado;
            btnAbrirUpdate.Enabled = habilitado;
            btnReport.Enabled = habilitado;
            btnAuto.Enabled = habilitado;
        }

        // ─────────────────────────────────────────────────────────────
        // EVENTOS DE BOTONES
        // ─────────────────────────────────────────────────────────────
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
            Log("\n======================================================\n");
            Log(">>> ⚡ INICIANDO MANTENIMIENTO AUTOMÁTICO COMPLETO ⚡ <<<\n");
            Log("======================================================\n");

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            await ProcesoLimpieza();
            await ProcesoSmart();
            await ProcesoUpdates();
            await ProcesoPDF();

            Log("\n>>> ✅ MANTENIMIENTO AUTOMÁTICO FINALIZADO.\n");
            SetBotonesHabilitados(true);
        }

        // ─────────────────────────────────────────────────────────────
        // 1. LIMPIEZA
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoLimpieza()
        {
            Log("\n>>> [1/4] Iniciando limpieza de archivos temporales y caché...\n");
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
                    (arch, bytes) = await Task.Run(() => LimpiezaService.LimpiarDirectorio(ruta));
                }
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
            Log($">>> Limpieza completada. Total: {archivosBorrados} archivos | {bytesLiberados / 1048576.0:F2} MB liberados\n");
        }

        // ─────────────────────────────────────────────────────────────
        // 2. S.M.A.R.T.
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoSmart()
        {
            Log("\n>>> [2/4] Iniciando diagnóstico S.M.A.R.T....\n");
            _reporte.Discos.Clear();
            var discos = await SmartService.ObtenerDiscosAsync(Log);
            _reporte.Discos.AddRange(discos);
        }

        // ─────────────────────────────────────────────────────────────
        // 3. WINDOWS UPDATE
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoUpdates()
        {
            Log("\n>>> [3/4] Analizando Windows Update (solo lectura)...\n");
            await UpdateService.AnalizarAsync(_reporte, Log);
        }

        // ─────────────────────────────────────────────────────────────
        // 4. GENERAR PDF
        // ─────────────────────────────────────────────────────────────
        private async Task ProcesoPDF()
        {
            Log("\n>>> [4/4] Recopilando datos del sistema y preparando informe PDF...\n");

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            // Si el usuario genera el PDF sin haber pasado por S.M.A.R.T., lo ejecutamos aquí
            if (_reporte.Discos.Count == 0)
                await ProcesoSmart();

            // Telemetría en segundo plano para no congelar la ventana
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

                Log($">>> ✅ PDF guardado correctamente: {rutaArchivo}\n");
                System.Diagnostics.Process.Start(
                    new System.Diagnostics.ProcessStartInfo { FileName = rutaArchivo, UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log(">>> ⚠️  No se pudo generar el PDF.\n");
                Log($"    Causa: {ex.Message}\n");
                Log("    Asegúrate de que la ruta de destino es accesible y el archivo no está abierto.\n");
            }
        }
    }
}