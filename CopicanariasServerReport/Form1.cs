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

        // 🎨 PALETA DE COLORES (Estilo Consola)
        private static readonly WinColor ClrFondo = WinColor.FromArgb(30, 30, 30);
        private static readonly WinColor ClrTexto = WinColor.FromArgb(212, 212, 212);
        private static readonly WinColor ClrSeccion = WinColor.FromArgb(86, 156, 214);
        private static readonly WinColor ClrOk = WinColor.FromArgb(78, 201, 176);
        private static readonly WinColor ClrAviso = WinColor.FromArgb(206, 145, 120);
        private static readonly WinColor ClrError = WinColor.FromArgb(244, 71, 71);
        private static readonly WinColor ClrDetalle = WinColor.FromArgb(156, 220, 254);
        private static readonly WinColor ClrSubdetalle = WinColor.FromArgb(128, 128, 128);

        public Form1()
        {
            InitializeComponent();
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");
        }

        // ── Carga del formulario ─────────────────────────────────────
        private async void Form1_Load(object sender, EventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var workArea = Screen.GetWorkingArea(this);
            this.Location = new System.Drawing.Point(
                workArea.Left + (workArea.Width - this.Width) / 2,
                workArea.Top + (int)((workArea.Height - this.Height) * 0.30)
            );

            // Ajustes iniciales del Log oscuro
            rtbLog.BackColor = ClrFondo;
            rtbLog.Font = new Font("Consolas", 10f, FontStyle.Regular);
            rtbLog.ForeColor = ClrTexto;
            rtbLog.Clear();

            cmbTecnico.Items.Add("— Seleccione un técnico —");
            cmbTecnico.Items.Add("Alejandro Martel");
            cmbTecnico.Items.Add("Himar Bautista");
            cmbTecnico.Items.Add("Mencey Medina");
            cmbTecnico.Items.Add("Aarón Ojeda (DF-Server)");
            cmbTecnico.Items.Add("Francisco Muñoz (DF-Server)");
            cmbTecnico.SelectedIndex = 0;

            await EscaneoInicialAsync();
        }

        // ═════════════════════════════════════════════════════════════
        // ALTERNAR VISTA: DASHBOARD VS LOG TÉCNICO
        // ═════════════════════════════════════════════════════════════
        private void btnToggleLog_Click(object sender, EventArgs e)
        {
            if (rtbLog.Visible)
            {
                // Ocultar Log, Mostrar Dashboard
                rtbLog.Visible = false;
                pnlCardUpd.Visible = true;
                pnlCardDrv.Visible = true;
                pnlCardTmp.Visible = true;
                pnlCardSmart.Visible = true;

                btnToggleLog.Text = "👁 Ver Log Técnico";
                btnToggleLog.BackColor = WinColor.FromArgb(100, 100, 100); // Gris
            }
            else
            {
                // Mostrar Log, Ocultar Dashboard
                rtbLog.Visible = true;
                pnlCardUpd.Visible = false;
                pnlCardDrv.Visible = false;
                pnlCardTmp.Visible = false;
                pnlCardSmart.Visible = false;

                btnToggleLog.Text = "📊 Ver Dashboard";
                btnToggleLog.BackColor = WinColor.FromArgb(17, 35, 108); // Azul Copicanarias
            }
        }

        // ═════════════════════════════════════════════════════════════
        // ACTUALIZADOR DEL DASHBOARD VISUAL
        // ═════════════════════════════════════════════════════════════
        private void ActualizarDashboard()
        {
            // 1. Tarjeta Windows Update (CORREGIDA LÓGICA)
            if (_reporte.UpdatesEjecutado)
            {
                // Mostramos detalles de importantes y opcionales
                if (_reporte.UpdatesImportantes > 0 || _reporte.UpdatesOpcionales > 0)
                {
                    lblValUpd.Text = $"{_reporte.UpdatesImportantes} Importantes\n{_reporte.UpdatesOpcionales} Opcionales";

                    // Color: Rojo si hay importantes, Naranja si solo hay opcionales
                    if (_reporte.UpdatesImportantes > 0)
                        lblValUpd.ForeColor = WinColor.FromArgb(226, 30, 45); // Rojo
                    else
                        lblValUpd.ForeColor = WinColor.FromArgb(206, 145, 120); // Naranja
                }
                else
                {
                    lblValUpd.Text = "Todo al día\n(0 pendientes)";
                    lblValUpd.ForeColor = WinColor.FromArgb(34, 197, 94); // Verde ok
                }
            }

            // 2. Tarjeta Drivers (CORREGIDA LÓGICA)
            if (_reporte.DriversEjecutado)
            {
                int obsoletos = _reporte.Drivers.Count;
                if (obsoletos > 0)
                {
                    lblValDrv.Text = $"{obsoletos} Con errores";
                    lblValDrv.ForeColor = WinColor.FromArgb(226, 30, 45);
                }
                else
                {
                    lblValDrv.Text = "Todos operativos";
                    lblValDrv.ForeColor = WinColor.FromArgb(34, 197, 94);
                }
            }

            // 3. Tarjeta Limpieza
            if (_reporte.LimpiezaEjecutada)
            {
                lblValTmp.Text = $"{_reporte.ArchivosBorrados} Archivos\n({_reporte.BytesLiberados / 1048576.0:F1} MB)";
                lblValTmp.ForeColor = WinColor.FromArgb(17, 35, 108); // Azul
            }

            // 4. Tarjeta S.M.A.R.T. (CON ETIQUETAS VISUALES)
            if (_reporte.Discos.Count > 0)
            {
                int discosSanos = _reporte.Discos.Count(d => !d.Estado.Contains("ALERTA") && !d.Estado.Contains("Error"));
                int discosPeligro = _reporte.Discos.Count - discosSanos;

                lblValSmart.Text = discosPeligro > 0 ? $"{discosPeligro} DISCOS EN PELIGRO" : $"Todos operativos ({discosSanos})";
                lblValSmart.ForeColor = discosPeligro > 0 ? WinColor.FromArgb(226, 30, 45) : WinColor.FromArgb(34, 197, 94);

                flpDiscos.Controls.Clear();
                foreach (var disco in _reporte.Discos)
                {
                    bool isUsb = disco.Tipo.Contains("USB");
                    bool isAlert = disco.Estado.Contains("ALERTA") || disco.Estado.Contains("Error");

                    // Contenedor principal del disco
                    Panel pnlDisco = new Panel
                    {
                        Width = flpDiscos.Width - 25,
                        Height = 55, // Más alto para que quepan las etiquetas
                        Margin = new Padding(3, 3, 3, 6),
                        BackColor = WinColor.White
                    };

                    // Dibujar una línea sutil debajo de cada disco para separarlos
                    pnlDisco.Paint += (s, e) => { e.Graphics.DrawLine(Pens.Gainsboro, 0, pnlDisco.Height - 1, pnlDisco.Width, pnlDisco.Height - 1); };

                    // Nombre del disco
                    Label lblNombre = new Label
                    {
                        Text = $"{(isUsb ? "🔌" : "💽")} {disco.Modelo} ({disco.TamanoGB:F0} GB)",
                        AutoSize = true,
                        Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                        ForeColor = isAlert ? WinColor.FromArgb(226, 30, 45) : WinColor.FromArgb(17, 35, 108),
                        Location = new Point(5, 5)
                    };
                    pnlDisco.Controls.Add(lblNombre);

                    // --- GENERADOR DE ETIQUETAS (BADGES) ---
                    int currentX = 25; // Empezamos a dibujar etiquetas con un pequeño margen izquierdo

                    // 1. Etiqueta de Estado
                    Label lblStatus = CrearBadge(disco.Estado,
                        isAlert ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(220, 252, 231),
                        isAlert ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(21, 128, 61),
                        ref currentX, 28);
                    pnlDisco.Controls.Add(lblStatus);

                    // 2. Etiqueta de Temperatura
                    if (disco.Temperatura.HasValue)
                    {
                        WinColor bgTemp = disco.Temperatura >= 55 ? WinColor.FromArgb(254, 240, 138) : WinColor.FromArgb(243, 244, 246);
                        WinColor fgTemp = disco.Temperatura >= 55 ? WinColor.FromArgb(161, 98, 7) : WinColor.FromArgb(75, 85, 99);
                        Label lblTemp = CrearBadge($"🌡️ {disco.Temperatura}°C", bgTemp, fgTemp, ref currentX, 28);
                        pnlDisco.Controls.Add(lblTemp);
                    }

                    // 3. Etiqueta de Horas
                    if (disco.HorasEncendido.HasValue)
                    {
                        Label lblHours = CrearBadge($"⏱️ {disco.HorasEncendido}h", WinColor.FromArgb(243, 244, 246), WinColor.FromArgb(75, 85, 99), ref currentX, 28);
                        pnlDisco.Controls.Add(lblHours);
                    }

                    // 4. Etiqueta de Salud
                    if (disco.TieneDatosSalud)
                    {
                        WinColor bgHealth = disco.PorcentajeSalud <= 20 ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(219, 234, 254);
                        WinColor fgHealth = disco.PorcentajeSalud <= 20 ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(29, 78, 216);
                        Label lblHealth = CrearBadge($"❤️ {disco.PorcentajeSalud}%", bgHealth, fgHealth, ref currentX, 28);
                        pnlDisco.Controls.Add(lblHealth);
                    }


                    flpDiscos.Controls.Add(pnlDisco);
                }
            }
        }

        // --- HELPER PARA CREAR ETIQUETAS (BADGES) VISUALES ---
        private Label CrearBadge(string texto, WinColor fondo, WinColor textoColor, ref int currentX, int y)
        {
            Label badge = new Label
            {
                Text = texto,
                AutoSize = true,
                Font = new Font("Segoe UI", 8F, FontStyle.Bold),
                BackColor = fondo,
                ForeColor = textoColor,
                Location = new Point(currentX, y),
                Padding = new Padding(2) // Margen interno para que no quede pegado el texto
            };

            // Calculamos el espacio de forma síncrona usando PreferredSize
            currentX += badge.PreferredSize.Width + 5;

            return badge;
        }

        // ═════════════════════════════════════════════════════════════
        // PANEL DF-SERVER Y GESTIÓN DE ALTURA
        // ═════════════════════════════════════════════════════════════
        private void cmbTecnico_SelectedIndexChanged(object sender, EventArgs e)
        {
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

        private void RecalcularAltura()
        {
            // Fijamos la altura base para que no dependa del Log
            int contenidoBottom = 500;

            // Leemos el ancho actual que Windows ha decidido para la pantalla,
            int anchoActual = this.ClientSize.Width;

            if (!panelDF.Visible)
            {
                int yBotones = contenidoBottom + 12;
                btnReport.Top = yBotones;
                btnAuto.Top = yBotones;
                this.ClientSize = new WinSize(anchoActual, yBotones + btnReport.Height + 40);
                return;
            }

            int alturaPanel = 38 + 3 * 32 + 12;
            if (panelCertsDinamico.Visible)
                alturaPanel += panelCertsDinamico.Height + 14;

            panelDF.Height = alturaPanel;
            panelDF.Top = contenidoBottom + 8;

            int yBotonesDF = panelDF.Bottom + 8;
            btnReport.Top = yBotonesDF;
            btnAuto.Top = yBotonesDF;
            this.ClientSize = new WinSize(anchoActual, yBotonesDF + btnReport.Height + 40);
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

        private void chkFirmas_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkFirmas.Checked;
            lblFirmasRestantes.Enabled = activo;
            numFirmas.Enabled = activo;
            if (!activo) numFirmas.Value = 0;
        }

        private void chkCertificados_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkCertificados.Checked;
            lblNumCerts.Enabled = activo;
            numCertificados.Enabled = activo;
            panelCertsDinamico.Visible = activo;

            RebuildCertificadoFields(activo ? (int)numCertificados.Value : 0);
        }

        private void numCertificados_ValueChanged(object sender, EventArgs e)
        {
            if (chkCertificados.Checked)
                RebuildCertificadoFields((int)numCertificados.Value);
        }

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

            RecalcularAltura();

            for (int i = 0; i < cantidad; i++)
            {
                int y = 4 + i * rowH;

                var lblN = new Label
                {
                    Text = $"Certificado {i + 1}:",
                    Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                    ForeColor = WinColor.FromArgb(17, 35, 108),
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

        private bool ValidarCamposDf()
        {
            if (!_reporte.EsTecnicoDf) return true;

            var df = _reporte.DfServer;

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
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => Log(texto)));
                return;
            }

            if (string.IsNullOrEmpty(texto)) return;
            string linea = texto.TrimEnd('\n', '\r');

            if (linea.Contains("✅"))
            { Escribir(texto, ClrOk, linea.TrimStart().StartsWith(">>>"), 10f); return; }

            if (linea.Contains("⚠️") || linea.Contains("🔁"))
            { Escribir(texto, ClrAviso, linea.TrimStart().StartsWith(">>>"), 10f); return; }

            if (linea.Contains("⚡"))
            { Escribir(texto, WinColor.FromArgb(197, 134, 192), true, 10f); return; }

            if (linea.Contains("sin acceso") || linea.Contains("No se pudo") || linea.Contains("ALERTA"))
            { Escribir(texto, ClrError, false, 10f); return; }

            if (linea.TrimStart().StartsWith(">>>"))
            { Escribir(texto, ClrSeccion, true, 10f); return; }

            if (linea.TrimStart().StartsWith("·") || linea.StartsWith("    ·"))
            { Escribir(texto, ClrDetalle, false, 9.5f); return; }

            if (linea.StartsWith("      ") || linea.TrimStart().StartsWith("Código"))
            { Escribir(texto, ClrSubdetalle, false, 9f); return; }

            Escribir(texto, ClrTexto, false, 10f);
        }

        private void LogBanner(string titulo, WinColor fondo, WinColor texto)
        {
            rtbLog.SuspendLayout();
            Escribir("\n", ClrTexto, false, 10f);
            string contenido = $"  {titulo}  ".PadRight(64);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;

            WinColor bannerFondoDark = WinColor.FromArgb(fondo.R / 2, fondo.G / 2, fondo.B / 2);
            rtbLog.SelectionBackColor = bannerFondoDark;

            rtbLog.SelectionColor = WinColor.White;
            rtbLog.SelectionFont = new Font("Consolas", 10.5f, FontStyle.Bold);
            rtbLog.AppendText(contenido + "\n");
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = ClrTexto;
            rtbLog.SelectionFont = rtbLog.Font;
            Escribir("\n", ClrTexto, false, 10f);
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        private void Escribir(string texto, WinColor color, bool bold, float size)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.Invoke(new Action(() => Escribir(texto, color, bold, size)));
                return;
            }

            rtbLog.SuspendLayout();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = color;
            rtbLog.SelectionFont = new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular);
            rtbLog.AppendText(texto);
            rtbLog.SelectionColor = ClrTexto;
            rtbLog.SelectionFont = rtbLog.Font;
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

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
            panelDF.Enabled = habilitado;
        }

        // ═════════════════════════════════════════════════════════════
        // PROCESOS DE ESCANEO Y MANTENIMIENTO
        // ═════════════════════════════════════════════════════════════
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

            // --- AÑADE ESTAS DOS LÍNEAS AQUÍ ---
            _reporte.UpdatesEjecutado = true;
            _reporte.DriversEjecutado = true;
            // ------------------------------------

            ActualizarDashboard(); // <-- Ahora sí refrescará los recuadros visuales al arrancar
            SetBotonesHabilitados(true);
        }

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
            LogBanner("⚡   REALIZANDO PREVENTIVA COMPLETA",
                WinColor.FromArgb(100, 30, 180), WinColor.White);

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            await ProcesoSmart();
            await ProcesoLimpieza();
            await ProcesoUpdates();
            await ProcesoDrivers();
            await ProcesoPDF();

            LogBanner("✅   Mantenimiento automático finalizado",
                WinColor.FromArgb(20, 120, 55), WinColor.White);

            SetBotonesHabilitados(true);
        }

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

            ActualizarDashboard(); 
        }

        private async Task ProcesoSmart()
        {
            LogBanner("DIAGNÓSTICO S.M.A.R.T. DE DISCOS",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _reporte.Discos.Clear();
            var discos = await SmartService.ObtenerDiscosAsync(Log);
            _reporte.Discos.AddRange(discos);

            ActualizarDashboard(); 
        }

        private async Task ProcesoUpdates()
        {
            LogBanner("ANÁLISIS DE WINDOWS UPDATE",
                WinColor.FromArgb(50, 60, 100), WinColor.White);
            await UpdateService.AnalizarAsync(_reporte, Log);

            ActualizarDashboard(); 
        }

        private async Task ProcesoDrivers()
        {
            LogBanner("ANÁLISIS DE CONTROLADORES",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _reporte.Drivers.Clear();
            var drivers = await DriverService.EscanearAsync(Log);
            _reporte.Drivers.AddRange(drivers);

            _reporte.DriversEjecutado = true;

            ActualizarDashboard();
        }

        private async Task ProcesoPDF()
        {
            LogBanner("PREPARANDO INFORME PDF",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            LeerDatosDF();
            if (!ValidarCamposDf()) return;

            if (_reporte.Discos.Count == 0)
                await ProcesoSmart();

            Log(">>> Recopilando telemetría del sistema...\n");
            await Task.Run(() => TelemetriaService.RecopilarTelemetria(_reporte, Log));
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

        private void lblTitSmart_Click(object sender, EventArgs e)
        {

        }

        private void lblValUpd_Click(object sender, EventArgs e)
        {

        }
    }
}