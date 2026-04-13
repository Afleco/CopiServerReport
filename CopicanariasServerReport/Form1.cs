using CopicanariasServerReport.Pdf;
using CopicanariasServerReport.Services;
using QuestPDF.Infrastructure;
using WinColor = System.Drawing.Color;
using WinSize = System.Drawing.Size;
using CopicanariasServerReport.Componentes;

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private readonly DatosServidor _reporte = new DatosServidor();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly List<(TextBox Nombre, DateTimePicker Fecha)> _certControls = new();

        private Label _lblAvisoFirmas;

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
            this.DoubleBuffered = true;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var workArea = Screen.GetWorkingArea(this);
            this.Location = new System.Drawing.Point(
                workArea.Left + (workArea.Width - this.Width) / 2,
                workArea.Top + (int)((workArea.Height - this.Height) * 0.30)
            );

            rtbLog.BackColor = ClrFondo;
            rtbLog.Font = new Font("Consolas", 10f, FontStyle.Regular);
            rtbLog.ForeColor = ClrTexto;
            rtbLog.Clear();

            // AQUÍ SE PUEDEN AÑADIR TÉCNICOS (SI ESTE ES DE DF-SERVER SU NOMBRE DEBE TENER "(DF-SERVER)")
            cmbTecnico.Items.Add("— Seleccione un técnico —");
            cmbTecnico.Items.Add("Alejandro Martel");
            cmbTecnico.Items.Add("Himar Bautista");
            cmbTecnico.Items.Add("Mencey Medina");
            cmbTecnico.Items.Add("Aarón Ojeda (DF-Server)");
            cmbTecnico.Items.Add("Francisco Muñoz (DF-Server)");
            cmbTecnico.SelectedIndex = 0;

            _lblAvisoFirmas = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Visible = false
            };
            panelDF.Controls.Add(_lblAvisoFirmas);
            numFirmas.ValueChanged += (s, ev) => ActualizarAvisoFirmas();
            numFirmas.KeyUp += (s, ev) => ActualizarAvisoFirmas();

            numCertificados.ValueChanged += (s, ev) =>
            {
                if (chkCertificados.Checked) RebuildCertificadoFields((int)numCertificados.Value);
            };

            numCertificados.KeyUp += (s, ev) =>
            {
                if (chkCertificados.Checked && int.TryParse(numCertificados.Text, out int cantidadTecleada))
                {
                    if (cantidadTecleada > numCertificados.Maximum) cantidadTecleada = (int)numCertificados.Maximum;
                    if (cantidadTecleada < numCertificados.Minimum) cantidadTecleada = (int)numCertificados.Minimum;

                    RebuildCertificadoFields(cantidadTecleada);
                }
                else if (chkCertificados.Checked && string.IsNullOrWhiteSpace(numCertificados.Text))
                {
                    RebuildCertificadoFields(0);
                }
            };

            AjustarPosicionesDF();
            AjustarPosicionesDashboard();

            await EscaneoInicialAsync();
        }

        private void AjustarPosicionesDashboard()
        {
            int margen = 12;

            lblTitUpd.Left = lblIconUpd.Right + margen;
            lblValUpd.Left = lblTitUpd.Left;

            lblTitDrv.Left = lblIconDrv.Right + margen;
            lblValDrv.Left = lblTitDrv.Left;

            lblTitTmp.Left = lblIconTmp.Right + margen;
            lblValTmp.Left = lblTitTmp.Left;

            lblTitSmart.Left = lblIconSmart.Right + margen;
            lblValSmart.Left = lblTitSmart.Left;

            string txtUpdReal = lblValUpd.Text;
            string txtDrvReal = lblValDrv.Text;

            lblValUpd.Text = "99 Importantes\n99 Opcionales";
            lblValDrv.Text = "Todos operativos";

            int anchoIdealUpd = lblValUpd.Right + 15;
            int anchoIdealDrv = lblValDrv.Right + 15;

            lblValUpd.Text = txtUpdReal;
            lblValDrv.Text = txtDrvReal;

            int anchoTarjeta = Math.Max(220, Math.Max(anchoIdealUpd, anchoIdealDrv));

            pnlCardUpd.Width = anchoTarjeta;

            int separacionCentros = 10;
            pnlCardDrv.Left = pnlCardUpd.Right + separacionCentros;
            pnlCardDrv.Width = anchoTarjeta;

            int anchoTotalDashboard = pnlCardDrv.Right - pnlCardUpd.Left;

            pnlCardTmp.Width = anchoTotalDashboard;
            pnlCardSmart.Width = anchoTotalDashboard;
            rtbLog.Width = anchoTotalDashboard;
            btnAuto.Width = anchoTotalDashboard;

            flpDiscos.Width = anchoTotalDashboard - 30;
            foreach (Control ctrl in flpDiscos.Controls)
            {
                ctrl.Width = flpDiscos.Width - 25;
            }

            int margenDerechoVentana = 30;
            int nuevoAnchoVentana = Math.Max(820, pnlCardDrv.Right + margenDerechoVentana);

            if (this.WindowState != FormWindowState.Minimized && this.ClientSize.Width != nuevoAnchoVentana)
            {
                this.ClientSize = new WinSize(nuevoAnchoVentana, this.ClientSize.Height);
                btnToggleLog.Left = this.ClientSize.Width - btnToggleLog.Width - 30;
            }
        }

        private void AjustarPosicionesDF()
        {
            lblFirmasRestantes.Left = chkFirmas.Right + 15;
            numFirmas.Left = lblFirmasRestantes.Right + 10;

            lblNumCerts.Left = chkCertificados.Right + 15;
            numCertificados.Left = lblNumCerts.Right + 10;

            int columnaNumeros = Math.Max(lblFirmasRestantes.Right, lblNumCerts.Right) + 10;
            numFirmas.Left = columnaNumeros;
            numCertificados.Left = columnaNumeros;

            if (_lblAvisoFirmas != null)
            {
                _lblAvisoFirmas.Left = numFirmas.Right + 15;
                _lblAvisoFirmas.Top = numFirmas.Top + 2;
            }
        }

        private void chkFirmas_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkFirmas.Checked;
            lblFirmasRestantes.Enabled = activo;
            numFirmas.Enabled = activo;
            if (!activo) numFirmas.Value = 0;
            ActualizarAvisoFirmas();
        }

        private void ActualizarAvisoFirmas()
        {
            if (_lblAvisoFirmas == null) return;

            if (chkFirmas.Checked)
            {
                if (numFirmas.Value == 0)
                {
                    _lblAvisoFirmas.Text = "❌ No quedan firmas";
                    _lblAvisoFirmas.ForeColor = WinColor.FromArgb(226, 30, 45);
                    _lblAvisoFirmas.Visible = true;
                }
                else if (numFirmas.Value <= 100)
                {
                    _lblAvisoFirmas.Text = "⚠️ Quedan pocas firmas";
                    _lblAvisoFirmas.ForeColor = WinColor.FromArgb(220, 100, 0);
                    _lblAvisoFirmas.Visible = true;
                }
                else
                {
                    _lblAvisoFirmas.Visible = false;
                }
            }
            else
            {
                _lblAvisoFirmas.Visible = false;
            }
        }

        private void btnToggleLog_Click(object sender, EventArgs e)
        {
            if (rtbLog.Visible)
            {
                rtbLog.Visible = false;
                pnlCardUpd.Visible = true;
                pnlCardDrv.Visible = true;
                pnlCardTmp.Visible = true;
                pnlCardSmart.Visible = true;

                btnToggleLog.Text = "👁 Ver Log Técnico";
                btnToggleLog.BackColor = WinColor.FromArgb(100, 100, 100);
            }
            else
            {
                rtbLog.Visible = true;
                pnlCardUpd.Visible = false;
                pnlCardDrv.Visible = false;
                pnlCardTmp.Visible = false;
                pnlCardSmart.Visible = false;

                btnToggleLog.Text = "📊 Ver Dashboard";
                btnToggleLog.BackColor = WinColor.FromArgb(17, 35, 108);
            }
            RecalcularAltura();
        }

        private void ActualizarDashboard()
        {
            if (_reporte.UpdatesEjecutado)
            {
                if (_reporte.UpdatesImportantes > 0 || _reporte.UpdatesOpcionales > 0)
                {
                    lblValUpd.Text = $"{_reporte.UpdatesImportantes} Importantes\n{_reporte.UpdatesOpcionales} Opcionales";
                    if (_reporte.UpdatesImportantes > 0)
                        lblValUpd.ForeColor = WinColor.FromArgb(226, 30, 45);
                    else
                        lblValUpd.ForeColor = WinColor.FromArgb(206, 145, 120);
                }
                else
                {
                    lblValUpd.Text = "Todo al día\n(0 pendientes)";
                    lblValUpd.ForeColor = WinColor.FromArgb(34, 197, 94);
                }
            }

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

            if (_reporte.LimpiezaEjecutada)
            {
                lblValTmp.Text = $"{_reporte.ArchivosBorrados} Archivos\n({_reporte.BytesLiberados / 1048576.0:F1} MB)";
                lblValTmp.ForeColor = WinColor.FromArgb(17, 35, 108);
            }

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

                    Panel pnlDisco = new Panel
                    {
                        Width = flpDiscos.Width - 25,
                        Margin = new Padding(3, 3, 3, 6),
                        BackColor = WinColor.White
                    };

                    Label lblNombre = new Label
                    {
                        Text = $"{(isUsb ? "🔌" : "💽")} {disco.Modelo} ({disco.TamanoGB:F0} GB)",
                        AutoSize = true,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = isAlert ? WinColor.FromArgb(226, 30, 45) : WinColor.FromArgb(17, 35, 108),
                        Location = new Point(5, 5)
                    };
                    pnlDisco.Controls.Add(lblNombre);

                    int currentX = 25;
                    int badgeY = lblNombre.Bottom + 5;

                    Label lblStatus = CrearBadge(disco.Estado,
                        isAlert ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(220, 252, 231),
                        isAlert ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(21, 128, 61),
                        ref currentX, badgeY);
                    pnlDisco.Controls.Add(lblStatus);

                    if (disco.Temperatura.HasValue)
                    {
                        WinColor bgTemp = disco.Temperatura >= 55 ? WinColor.FromArgb(254, 240, 138) : WinColor.FromArgb(243, 244, 246);
                        WinColor fgTemp = disco.Temperatura >= 55 ? WinColor.FromArgb(161, 98, 7) : WinColor.FromArgb(75, 85, 99);
                        Label lblTemp = CrearBadge($"🌡️ {disco.Temperatura}°C", bgTemp, fgTemp, ref currentX, badgeY);
                        pnlDisco.Controls.Add(lblTemp);
                    }

                    if (disco.HorasEncendido.HasValue)
                    {
                        Label lblHours = CrearBadge($"⏱️ {disco.HorasEncendido}h", WinColor.FromArgb(243, 244, 246), WinColor.FromArgb(75, 85, 99), ref currentX, badgeY);
                        pnlDisco.Controls.Add(lblHours);
                    }

                    if (disco.TieneDatosSalud)
                    {
                        WinColor bgHealth = disco.PorcentajeSalud <= 20 ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(219, 234, 254);
                        WinColor fgHealth = disco.PorcentajeSalud <= 20 ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(29, 78, 216);
                        Label lblHealth = CrearBadge($"❤️ {disco.PorcentajeSalud}%", bgHealth, fgHealth, ref currentX, badgeY);
                        pnlDisco.Controls.Add(lblHealth);
                    }

                    pnlDisco.Height = lblStatus.Bottom + 8;
                    pnlDisco.Paint += (s, e) => { e.Graphics.DrawLine(Pens.Gainsboro, 0, pnlDisco.Height - 1, pnlDisco.Width, pnlDisco.Height - 1); };
                    flpDiscos.Controls.Add(pnlDisco);
                }
            }
            AjustarPosicionesDashboard();
        }

        private Label CrearBadge(string texto, WinColor fondo, WinColor textoColor, ref int currentX, int y)
        {
            Label badge = new Label
            {
                Text = texto,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = fondo,
                ForeColor = textoColor,
                Location = new Point(currentX, y),
                Padding = new Padding(2)
            };
            currentX += badge.PreferredSize.Width + 5;
            return badge;
        }

        private void cmbTecnico_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTecnico.SelectedIndex <= 0) { MostrarPanelDf(false); return; }
            bool esDf = cmbTecnico.SelectedItem?.ToString().Contains("(DF-Server)") == true;
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
            if (this.WindowState == FormWindowState.Minimized) return;

            int fondoDerecho = rtbLog.Visible ? rtbLog.Bottom : pnlCardSmart.Bottom;
            int fondoIzquierdo = btnDeviceManager.Bottom;

            int contenidoBottom = Math.Max(fondoDerecho, fondoIzquierdo);

            int anchoActual = this.ClientSize.Width;

            if (!panelDF.Visible)
            {
                int yBotones = contenidoBottom + 15;
                btnReport.Top = yBotones;
                btnAuto.Top = yBotones;
                this.ClientSize = new WinSize(anchoActual, btnReport.Bottom + 20);
                return;
            }

            int alturaPanelDf = chkCertificados.Checked ? panelCertsDinamico.Bottom + 10 : chkCertificados.Bottom + 10;
            panelDF.Height = alturaPanelDf;
            panelDF.Top = contenidoBottom + 10;

            int yBotonesDF = panelDF.Bottom + 15;
            btnReport.Top = yBotonesDF;
            btnAuto.Top = yBotonesDF;

            this.ClientSize = new WinSize(anchoActual, btnReport.Bottom + 20);
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

            var resp = MensajeModal.Show(
                "¿La digitalización certificada está configurada y funcionando correctamente?",
                "Digitalización certificada",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resp != DialogResult.Yes)
            {
                chkDigitalizacion.CheckedChanged -= chkDigitalizacion_CheckedChanged;
                chkDigitalizacion.Checked = false;
                chkDigitalizacion.CheckedChanged += chkDigitalizacion_CheckedChanged;
            }
        }
        private void chkCertificados_CheckedChanged(object sender, EventArgs e)
        {
            bool activo = chkCertificados.Checked;
            lblNumCerts.Enabled = activo;
            numCertificados.Enabled = activo;
            panelCertsDinamico.Visible = activo;
            if (!activo) numCertificados.Value = 1;
            RebuildCertificadoFields(activo ? (int)numCertificados.Value : 0);
        }

        private void numCertificados_ValueChanged(object sender, EventArgs e)
        {
            if (chkCertificados.Checked) RebuildCertificadoFields((int)numCertificados.Value);
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

            int currentY = 10;

            for (int i = 0; i < cantidad; i++)
            {
                int currentX = 6;

                var lblN = new Label
                {
                    Text = $"Certificado {i + 1}:",
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    ForeColor = WinColor.FromArgb(17, 35, 108),
                    Location = new Point(currentX, currentY + 3),
                    AutoSize = true
                };
                currentX += lblN.PreferredSize.Width + 10;

                var txtNombre = new TextBox
                {
                    PlaceholderText = "Nombre del certificado *",
                    Font = new Font("Segoe UI", 11f),
                    Location = new Point(currentX, currentY),
                    Width = 230,
                    BackColor = WinColor.FromArgb(255, 235, 235)
                };
                currentX += txtNombre.Width + 15;

                txtNombre.TextChanged += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtNombre.Text) || txtNombre.Text.Trim().Length < 10)
                        txtNombre.BackColor = WinColor.FromArgb(255, 235, 235);
                    else
                        txtNombre.BackColor = WinColor.White;
                };

                var lblF = new Label
                {
                    Text = "Caduca:",
                    Font = new Font("Segoe UI", 11f),
                    ForeColor = WinColor.DimGray,
                    Location = new Point(currentX, currentY + 3),
                    AutoSize = true
                };
                currentX += lblF.PreferredSize.Width + 10;

                var dtp = new DateTimePicker
                {
                    Format = DateTimePickerFormat.Short,
                    Font = new Font("Segoe UI", 11f),
                    Location = new Point(currentX, currentY),
                    Width = 120,
                    Value = DateTime.Today.AddYears(1)
                };
                currentX += dtp.Width + 15;

                var lblEstadoCert = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Location = new Point(currentX, currentY + 3)
                };

                void ActualizarEstadoCert()
                {
                    int dias = (dtp.Value.Date - DateTime.Today).Days;
                    if (dias < 0)
                    {
                        lblEstadoCert.Text = "❌ Caducado";
                        lblEstadoCert.ForeColor = WinColor.FromArgb(226, 30, 45);
                    }
                    else if (dias <= 92)
                    {
                        lblEstadoCert.Text = "⚠️ Caduca pronto";
                        lblEstadoCert.ForeColor = WinColor.FromArgb(220, 100, 0);
                    }
                    else
                    {
                        lblEstadoCert.Text = "✅ Válido";
                        lblEstadoCert.ForeColor = WinColor.FromArgb(34, 197, 94);
                    }
                }

                dtp.ValueChanged += (s, ev) => ActualizarEstadoCert();
                dtp.CloseUp += (s, ev) => ActualizarEstadoCert();
                dtp.KeyUp += (s, ev) => ActualizarEstadoCert();

                ActualizarEstadoCert();

                panelCertsDinamico.Controls.AddRange(new Control[] { lblN, txtNombre, lblF, dtp, lblEstadoCert });
                _certControls.Add((txtNombre, dtp));

                currentY = txtNombre.Bottom + 12;
            }

            int maxH = 170;
            panelCertsDinamico.Height = Math.Min(currentY + 5, maxH);
            RecalcularAltura();
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
                        Nombre = txt.Text.Trim(),
                        FechaCaducidad = dtp.Value.Date
                    });
                }
            }
        }

        private bool ValidarCamposDf()
        {
            if (!_reporte.EsTecnicoDf) return true;

            var df = _reporte.DfServer;
            int certIndex = 1;
            foreach (var cert in df.Certificados)
            {
                if (string.IsNullOrWhiteSpace(cert.Nombre) || cert.Nombre.Length < 10)
                {
                    MensajeModal.Show(
                        $"El nombre del Certificado {certIndex} no tiene una longitud válida (mínimo 10 caracteres).\n\nPor favor, escribe el nombre completo o el titular real.",
                        "Nombre de certificado inválido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    Log($">>> Informe cancelado: nombre no válido o demasiado corto en el Certificado {certIndex}.\n");
                    return false;
                }

                if (cert.ProximoACaducar)
                {
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

                    var resp = MensajeModal.Show(
                        mensaje, titulo,
                        MessageBoxButtons.YesNo,
                        caducado ? MessageBoxIcon.Error : MessageBoxIcon.Warning);

                    if (resp != DialogResult.Yes)
                    {
                        string motivo = caducado
                            ? "el cliente no ha sido avisado del certificado caducado"
                            : "el cliente no ha sido avisado del certificado próximo a caducar";
                        Log($">>> Informe cancelado: {motivo}.\n");
                        return false;
                    }
                }
                certIndex++;
            }

            if (df.TieneFirmas && df.FirmasRestantes <= 100)
            {
                var resp = MensajeModal.Show(
                    $"⚠️  Quedan solo {df.FirmasRestantes} firmas de DF-Signature disponibles.\n\n" +
                    $"¿Se le ha avisado al cliente de que le quedan pocas firmas de DF-Signature?",
                    "Pocas firmas disponibles",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (resp != DialogResult.Yes)
                {
                    Log(">>> Informe cancelado: el cliente no ha sido avisado de las pocas firmas restantes.\n");
                    return false;
                }
            }

            return true;
        }

        private void Log(string texto)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(() => Log(texto)));
                return;
            }

            if (string.IsNullOrEmpty(texto)) return;
            string linea = texto.TrimEnd('\n', '\r');

            if (linea.Contains("✅"))
            { Escribir(texto, ClrOk, linea.TrimStart().StartsWith(">>>"), 10f); return; }

            if (linea.Contains("⚠️") || linea.Contains("🔁"))
            { Escribir(texto, ClrAviso, linea.TrimStart().StartsWith(">>>"), 10f); return; }

            if (linea.Contains("❌"))
            { Escribir(texto, ClrError, linea.TrimStart().StartsWith(">>>"), 10f); return; }

            if (linea.Contains("⚡") || linea.Contains("⬇️") || linea.Contains("⚙️") || linea.Contains("🔎") || linea.Contains("ℹ️"))
            { Escribir(texto, WinColor.FromArgb(197, 134, 192), true, 10f); return; }

            if (linea.Contains("sin acceso") || linea.Contains("No se pudo") || linea.Contains("ALERTA"))
            { Escribir(texto, ClrError, false, 10f); return; }

            if (linea.Contains("🔴 IMPORTANTE"))
            { Escribir(texto, WinColor.FromArgb(226, 30, 45), true, 10f); return; }

            if (linea.Contains("🔵 OPCIONAL"))
            { Escribir(texto, WinColor.FromArgb(86, 156, 214), true, 10f); return; }

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
                rtbLog.BeginInvoke(new Action(() => Escribir(texto, color, bold, size)));
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
            btnInstalarUpdates.Enabled = habilitado;
            btnDrivers.Enabled = habilitado;

            btnAbrirUpdate.Enabled = true;
            btnDeviceManager.Enabled = true;

            btnReport.Enabled = habilitado;
            btnAuto.Enabled = habilitado;
        }

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

            _reporte.UpdatesEjecutado = true;
            _reporte.DriversEjecutado = true;

            ActualizarDashboard();
            SetBotonesHabilitados(true);
        }

        private bool TecnicoSeleccionado()
        {
            if (cmbTecnico.SelectedIndex > 0) return true;

            MensajeModal.Show(
                "Por favor, selecciona un técnico responsable antes de continuar.",
                "Técnico no seleccionado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        private async void btnCleanTemp_Click(object sender, EventArgs e)
        {
            SetBotonesHabilitados(false); await ProcesoLimpieza(); SetBotonesHabilitados(true);
        }

        private async void btnSmart_Click(object sender, EventArgs e)
        {
            SetBotonesHabilitados(false); await ProcesoSmart(); SetBotonesHabilitados(true);
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            SetBotonesHabilitados(false); await ProcesoUpdates(); SetBotonesHabilitados(true);
        }

        private async void btnInstalarUpdates_Click(object sender, EventArgs e)
        {
            // --- Comprobar si realmente hay algo que instalar ---
            int totalUpdates = _reporte.UpdatesImportantes + _reporte.UpdatesOpcionales;

            if (totalUpdates == 0)
            {
                MensajeModal.Show(
                    "El sistema ya está al día. No hay actualizaciones pendientes para instalar.",
                    "Todo actualizado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // --- Modal de confirmación anti-clics accidentales ---
            var confirmacion = MensajeModal.Show(
                $"Se van a descargar e instalar {totalUpdates} actualización(es) de Windows.\n\n¿Estás seguro de que deseas continuar?\nEste proceso se realizará de fondo y puede tardar varios minutos.",
                "Confirmar instalación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmacion != DialogResult.Yes)
            {
                Log(">>> Instalación de actualizaciones cancelada por el usuario.\n");
                return;
            }

            SetBotonesHabilitados(false);

            // Hacemos una copia exacta de la lista de nombres de las actualizaciones pendientes
            var actualizacionesPrevias = new List<string>(_reporte.NombresUpdates);

            LogBanner("INSTALANDO ACTUALIZACIONES", WinColor.FromArgb(50, 60, 100), WinColor.White);

            await UpdateService.InstalarAsync(_reporte, Log);

            // ---> AÑADIMOS UN RESPIRO DE 3 SEGUNDOS PARA QUE EL SERVICIO SE ESTABILICE <---
            await Task.Delay(3000);

            Log("\n>>> Re-analizando el estado tras la instalación...\n");
            await UpdateService.AnalizarAsync(_reporte, Log);

            // Si después de instalar, Windows pide reiniciar Y AÚN quedan actualizaciones pendientes...
            if (_reporte.RequiereReinicio && _reporte.NombresUpdates.Count > 0)
            {
                // Comparamos a ver si alguna de las que quedan ahora ya estaba en la lista del principio
                bool hayAtascadas = _reporte.NombresUpdates.Any(upd => actualizacionesPrevias.Contains(upd));

                if (hayAtascadas)
                {
                    Log(">>> ⚠️ AVISO DE INSTALACIÓN PARCIAL:\n");
                    Log("    · Algunas actualizaciones no se han podido instalar.\n");
                    Log("    · Puede ser que Windows haya bloqueado la instalación porque exige un reinicio previo del servidor.\n");
                    Log("    · Por favor, reinicia el equipo y vuelve a ejecutar la herramienta para instalar las restantes.\n\n");
                }
            }

            ActualizarDashboard();
            SetBotonesHabilitados(true);
        }

        private async void btnDrivers_Click(object sender, EventArgs e)
        {
            SetBotonesHabilitados(false); await ProcesoDrivers(); SetBotonesHabilitados(true);
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

            var rutas = new List<(string Ruta, string Nombre)>();
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            rutas.Add((Path.Combine(winDir, "Temp"), "Temp de Windows"));
            rutas.Add((Path.Combine(winDir, @"SoftwareDistribution\Download"), "Caché de Windows Update"));

            try
            {
                string sysDrive = Path.GetPathRoot(winDir);
                string baseUsers = Path.Combine(sysDrive, "Users");

                if (Directory.Exists(baseUsers))
                {
                    foreach (var dirUsuario in Directory.GetDirectories(baseUsers))
                    {
                        string nombreUser = new DirectoryInfo(dirUsuario).Name;

                        if (nombreUser.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                            nombreUser.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                            nombreUser.Equals("Default User", StringComparison.OrdinalIgnoreCase) ||
                            nombreUser.Equals("All Users", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string tempUserPath = Path.Combine(dirUsuario, @"AppData\Local\Temp");

                        if (Directory.Exists(tempUserPath))
                        {
                            rutas.Add((tempUserPath, $"Temp de usuario ({nombreUser})"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"    · ⚠️ No se pudieron revisar los perfiles de usuario: {ex.Message}\n");
            }

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

                if (arch >= 0 || nombre.Contains("Windows") || nombre.Contains("Caché"))
                {
                    Log($"    · {nombre}: {arch} archivos — {bytes / 1048576.0:F1} MB\n");
                }
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
            LogBanner("ANÁLISIS DE WINDOWS UPDATE ⏳",
                WinColor.FromArgb(50, 60, 100), WinColor.White);
            await UpdateService.AnalizarAsync(_reporte, Log);

            ActualizarDashboard();
        }

        private async Task ProcesoDrivers()
        {
            LogBanner("ANÁLISIS DE CONTROLADORES ⏳",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _reporte.Drivers.Clear();
            var drivers = await DriverService.EscanearAsync(Log);
            _reporte.Drivers.AddRange(drivers);

            _reporte.DriversEjecutado = true;

            ActualizarDashboard();
        }

        // ── Manejo de PDF con advertencia de actualizaciones ──────────────
        private async Task ProcesoPDF()
        {
            LogBanner("PREPARANDO INFORME PDF", WinColor.FromArgb(50, 60, 100), WinColor.White);

            if (cmbTecnico?.SelectedItem != null)
                _reporte.TecnicoResponsable = cmbTecnico.SelectedItem.ToString();

            LeerDatosDF();
            if (!ValidarCamposDf()) return;

            // --- LÓGICA DE BACKUP Y RESTAURACIÓN ---
            bool restaurarUpdates = false;
            int bkImportantes = _reporte.UpdatesImportantes;
            int bkOpcionales = _reporte.UpdatesOpcionales;
            bool bkReinicio = _reporte.RequiereReinicio;
            List<string> bkNombres = new List<string>(_reporte.NombresUpdates);

            bool restaurarDrivers = false;
            List<DriverInfo> bkDrivers = new List<DriverInfo>(_reporte.Drivers);

            // --- GESTIÓN DE ALERTAS DE WINDOWS UPDATE ---
            int totalUpdates = _reporte.UpdatesImportantes + _reporte.UpdatesOpcionales;
            if (totalUpdates > 0 || _reporte.RequiereReinicio)
            {
                string mensaje = totalUpdates > 0 && _reporte.RequiereReinicio
                    ? $"Hay {totalUpdates} actualización(es) y un REINICIO pendiente."
                    : totalUpdates > 0 ? $"Hay {totalUpdates} actualización(es) pendiente(s)." : "Hay un REINICIO pendiente.";

                var resp = MensajeModal.Show($"{mensaje}\n\n¿Quieres que estas alertas se reflejen en el informe PDF?",
                    "Windows Update Pendiente", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resp == DialogResult.Cancel)
                {
                    Log(">>> Generación de PDF cancelada por el usuario.\n");
                    return;
                }

                if (resp == DialogResult.No)
                {
                    _reporte.UpdatesImportantes = 0; _reporte.UpdatesOpcionales = 0;
                    _reporte.NombresUpdates.Clear(); _reporte.RequiereReinicio = false;
                    restaurarUpdates = true;
                    Log("    · Nota: Se omitirán las alertas de Windows Update en el PDF.\n");
                }
            }

            // --- GESTIÓN DE ALERTAS DE DRIVERS ---
            if (_reporte.Drivers.Count > 0)
            {
                string mensajeDrivers = _reporte.Drivers.Count == 1
                    ? "Hay 1 controlador con error."
                    : $"Hay {_reporte.Drivers.Count} controladores con errores.";

                var resp = MensajeModal.Show(
                    $"{mensajeDrivers}\n\n¿Quieres que estos errores aparezcan en el informe PDF?",
                    "Controladores con errores", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (resp == DialogResult.Cancel)
                {
                    if (restaurarUpdates)
                    {
                        _reporte.UpdatesImportantes = bkImportantes; _reporte.UpdatesOpcionales = bkOpcionales;
                        _reporte.RequiereReinicio = bkReinicio; _reporte.NombresUpdates = bkNombres;
                    }
                    Log(">>> Generación de PDF cancelada por el usuario.\n");
                    return;
                }

                if (resp == DialogResult.No)
                {
                    _reporte.Drivers.Clear();
                    restaurarDrivers = true;
                    Log("    · Nota: Se omitirán los errores de controladores en el PDF.\n");
                }
            }

            // --- PROCESO DE GENERACIÓN ---
            if (_reporte.Discos.Count == 0) await ProcesoSmart();

            Log(">>> Recopilando telemetría del sistema...\n");
            await Task.Run(() => TelemetriaService.RecopilarTelemetria(_reporte, Log));
            await TelemetriaService.RecopilarJavaAsync(_reporte, Log, _http);

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                Title = "Guardar Informe de Mantenimiento",
                FileName = $"Informe_Sistema_{DateTime.Now:dd_MM_yyyy}_{_reporte.NombreServidor}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string rutaArchivo = sfd.FileName;
                try
                {
                    byte[] logoBytes = ImagenToBytes(Properties.Resources.copicanariasicon);
                    byte[] dfLogoBytes = _reporte.EsTecnicoDf ? ImagenToBytes(Properties.Resources.DF_SERVER_logo_300x60) : null;

                    await Task.Run(() => PdfGenerator.Generar(rutaArchivo, _reporte, logoBytes, dfLogoBytes));

                    Log($">>> ✅ PDF guardado en: {rutaArchivo}\n");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = rutaArchivo, UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    Log($">>> ❌ Error al generar PDF: {ex.Message}\n");
                }
            }
            else
            {
                Log(">>> Generación de PDF cancelada.\n");
            }

            // --- RESTAURACIÓN DE DATOS PARA LA INTERFAZ ---
            if (restaurarUpdates)
            {
                _reporte.UpdatesImportantes = bkImportantes; _reporte.UpdatesOpcionales = bkOpcionales;
                _reporte.RequiereReinicio = bkReinicio; _reporte.NombresUpdates = bkNombres;
            }
            if (restaurarDrivers)
            {
                _reporte.Drivers.AddRange(bkDrivers);
            }
        }

        // Helper rápido para limpiar el código de arriba
        private byte[] ImagenToBytes(System.Drawing.Image img)
        {
            if (img == null) return null;
            using var ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private async void btnReport_Click(object sender, EventArgs e)
        {
            if (!TecnicoSeleccionado()) return;
            SetBotonesHabilitados(false); await ProcesoPDF(); SetBotonesHabilitados(true);
        }
    }
}