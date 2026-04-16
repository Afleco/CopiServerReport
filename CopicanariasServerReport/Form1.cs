using CopicanariasServerReport.Componentes;
using CopicanariasServerReport.Pdf;
using CopicanariasServerReport.Services;
using QuestPDF.Infrastructure;
using WinColor = System.Drawing.Color;
using WinSize = System.Drawing.Size;

namespace CopicanariasServerReport
{
    public partial class Form1 : Form
    {
        private readonly ServerData _report = new ServerData();
        private static readonly HttpClient _http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

        private readonly List<(TextBox Nombre, DateTimePicker Fecha)> _certControls = new();

        private Label _lblSignatureNotice;

        private bool _isProcessRunning = false;

        private static readonly WinColor ClrFondo = WinColor.FromArgb(30, 30, 30);
        private static readonly WinColor ClrText = WinColor.FromArgb(212, 212, 212);
        private static readonly WinColor ClrSection = WinColor.FromArgb(86, 156, 214);
        private static readonly WinColor ClrOk = WinColor.FromArgb(78, 201, 176);
        private static readonly WinColor ClrAviso = WinColor.FromArgb(206, 145, 120);
        private static readonly WinColor ClrError = WinColor.FromArgb(244, 71, 71);
        private static readonly WinColor ClrDetail = WinColor.FromArgb(156, 220, 254);
        private static readonly WinColor ClrSubdetail = WinColor.FromArgb(128, 128, 128);

        public Form1()
        {
            InitializeComponent();
            this.DoubleBuffered = true;
            _http.DefaultRequestHeaders.UserAgent.ParseAdd("CopicanariasServerReport/1.0");

            this.FormClosing += Form1_FormClosing;
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
            rtbLog.ForeColor = ClrText;
            rtbLog.Clear();

            // AQUÍ SE PUEDEN AÑADIR TÉCNICOS (SI ESTE ES DE DF-SERVER SU NOMBRE DEBE TENER "(DF-SERVER)")
            cmbTechnician.Items.Add("— Seleccione un técnico —");
            cmbTechnician.Items.Add("Alejandro Martel");
            cmbTechnician.Items.Add("Himar Bautista");
            cmbTechnician.Items.Add("Mencey Medina");
            cmbTechnician.Items.Add("Aarón Ojeda (DF-Server)");
            cmbTechnician.Items.Add("Francisco Muñoz (DF-Server)");
            cmbTechnician.SelectedIndex = 0;

            _lblSignatureNotice = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Visible = false
            };
            DfPanel.Controls.Add(_lblSignatureNotice);
            SignaturesNum.ValueChanged += (s, ev) => UpdateSignaturesNotice();
            SignaturesNum.KeyUp += (s, ev) => UpdateSignaturesNotice();

            CertificatesNum.ValueChanged += (s, ev) =>
            {
                if (chkCertificates.Checked) RebuildCertificateFields((int)CertificatesNum.Value);
            };

            CertificatesNum.KeyUp += (s, ev) =>
            {
                if (chkCertificates.Checked && int.TryParse(CertificatesNum.Text, out int TypedAmount))
                {
                    if (TypedAmount > CertificatesNum.Maximum) TypedAmount = (int)CertificatesNum.Maximum;
                    if (TypedAmount < CertificatesNum.Minimum) TypedAmount = (int)CertificatesNum.Minimum;

                    RebuildCertificateFields(TypedAmount);
                }
                else if (chkCertificates.Checked && string.IsNullOrWhiteSpace(CertificatesNum.Text))
                {
                    RebuildCertificateFields(0);
                }
            };

            AdjustDfPositions();
            AdjustDashboardPositions();

            await InitialScanAsync();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Si el usuario le dio a la X y hay un proceso en ejecución
            if (e.CloseReason == CloseReason.UserClosing && _isProcessRunning)
            {
                // Usamos YesNo.
                var resp = ModalMessage.Show(
                    "Hay una operación de mantenimiento en curso.\n\nSi cierras la aplicación ahora, el proceso se interrumpirá de forma brusca.\n\n¿Estás completamente seguro de que deseas forzar el cierre?",
                    "Proceso en ejecución",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                // Si NO ha pulsado "Sí" cancelamos el cierre.
                if (resp != DialogResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }

        private void AdjustDashboardPositions()
        {
            int margin = 12;

            lblTitUpd.Left = lblIconUpd.Right + margin;
            lblValUpd.Left = lblTitUpd.Left;

            lblTitDrv.Left = lblIconDrv.Right + margin;
            lblValDrv.Left = lblTitDrv.Left;

            lblTitTmp.Left = lblIconTmp.Right + margin;
            lblValTmp.Left = lblTitTmp.Left;

            lblTitSmart.Left = lblIconSmart.Right + margin;
            lblValSmart.Left = lblTitSmart.Left;

            string txtUpdReal = lblValUpd.Text;
            string txtDrvReal = lblValDrv.Text;

            lblValUpd.Text = "99 Importantes\n99 Opcionales";
            lblValDrv.Text = "Todos operativos";

            int IdealWidthUpd = lblValUpd.Right + 15;
            int IdealWidthDrv = lblValDrv.Right + 15;

            lblValUpd.Text = txtUpdReal;
            lblValDrv.Text = txtDrvReal;

            int CardWidth = Math.Max(220, Math.Max(IdealWidthUpd, IdealWidthDrv));

            pnlCardUpd.Width = CardWidth;

            int separacionCentros = 10;
            pnlCardDrv.Left = pnlCardUpd.Right + separacionCentros;
            pnlCardDrv.Width = CardWidth;

            int TotalDashboardWidth = pnlCardDrv.Right - pnlCardUpd.Left;

            pnlCardTmp.Width = TotalDashboardWidth;
            pnlCardSmart.Width = TotalDashboardWidth;
            rtbLog.Width = TotalDashboardWidth;
            btnAuto.Width = TotalDashboardWidth;

            flpDisks.Width = TotalDashboardWidth - 30;
            foreach (Control ctrl in flpDisks.Controls)
            {
                ctrl.Width = flpDisks.Width - 25;
            }

            int margenDerechoVentana = 30;
            int nuevoAnchoVentana = Math.Max(820, pnlCardDrv.Right + margenDerechoVentana);

            if (this.WindowState != FormWindowState.Minimized && this.ClientSize.Width != nuevoAnchoVentana)
            {
                this.ClientSize = new WinSize(nuevoAnchoVentana, this.ClientSize.Height);
                btnToggleLog.Left = this.ClientSize.Width - btnToggleLog.Width - 30;
            }
        }

        private void AdjustDfPositions()
        {
            lblRemainingSignatures.Left = chkSignatures.Right + 15;
            SignaturesNum.Left = lblRemainingSignatures.Right + 10;

            lblCertsNum.Left = chkCertificates.Right + 15;
            CertificatesNum.Left = lblCertsNum.Right + 10;

            int ColumnNum = Math.Max(lblRemainingSignatures.Right, lblCertsNum.Right) + 10;
            SignaturesNum.Left = ColumnNum;
            CertificatesNum.Left = ColumnNum;

            if (_lblSignatureNotice != null)
            {
                _lblSignatureNotice.Left = SignaturesNum.Right + 15;
                _lblSignatureNotice.Top = SignaturesNum.Top + 2;
            }
        }

        private void chkSignatures_CheckedChanged(object sender, EventArgs e)
        {
            bool active = chkSignatures.Checked;
            lblRemainingSignatures.Enabled = active;
            SignaturesNum.Enabled = active;
            if (!active) SignaturesNum.Value = 0;
            UpdateSignaturesNotice();
        }

        private void UpdateSignaturesNotice()
        {
            if (_lblSignatureNotice == null) return;

            if (chkSignatures.Checked)
            {
                if (SignaturesNum.Value == 0)
                {
                    _lblSignatureNotice.Text = "❌ No quedan firmas";
                    _lblSignatureNotice.ForeColor = WinColor.FromArgb(226, 30, 45);
                    _lblSignatureNotice.Visible = true;
                }
                else if (SignaturesNum.Value <= 100)
                {
                    _lblSignatureNotice.Text = "⚠️ Quedan pocas firmas";
                    _lblSignatureNotice.ForeColor = WinColor.FromArgb(220, 100, 0);
                    _lblSignatureNotice.Visible = true;
                }
                else
                {
                    _lblSignatureNotice.Visible = false;
                }
            }
            else
            {
                _lblSignatureNotice.Visible = false;
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
            RecalculateHeight();
        }

        private void UpdateDashboard()
        {
            if (_report.IsUpdatesExecuted)
            {
                if (_report.ImportantUpdates > 0 || _report.OptionalUpdates > 0)
                {
                    lblValUpd.Text = $"{_report.ImportantUpdates} Importantes\n{_report.OptionalUpdates} Opcionales";
                    if (_report.ImportantUpdates > 0)
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

            if (_report.IsDriversExecuted)
            {
                int deprecated = _report.Drivers.Count;
                if (deprecated > 0)
                {
                    lblValDrv.Text = $"{deprecated} Con errores";
                    lblValDrv.ForeColor = WinColor.FromArgb(226, 30, 45);
                }
                else
                {
                    lblValDrv.Text = "Todos operativos";
                    lblValDrv.ForeColor = WinColor.FromArgb(34, 197, 94);
                }
            }

            if (_report.IsCleanupExecuted)
            {
                lblValTmp.Text = $"{_report.DeletedFiles} Archivos\n({_report.FreedBytes / 1048576.0:F1} MB)";
                lblValTmp.ForeColor = WinColor.FromArgb(17, 35, 108);
            }

            if (_report.Disks.Count > 0)
            {
                int HealthyDisks = _report.Disks.Count(d => !d.State.Contains("ALERTA") && !d.State.Contains("Error"));
                int AtRiskDisks = _report.Disks.Count - HealthyDisks;

                lblValSmart.Text = AtRiskDisks > 0 ? $"{AtRiskDisks} DISCOS EN PELIGRO" : $"Todos operativos ({HealthyDisks})";
                lblValSmart.ForeColor = AtRiskDisks > 0 ? WinColor.FromArgb(226, 30, 45) : WinColor.FromArgb(34, 197, 94);

                flpDisks.Controls.Clear();
                foreach (var disk in _report.Disks)
                {
                    bool isUsb = disk.Type.Contains("USB");
                    bool isAlert = disk.State.Contains("ALERTA") || disk.State.Contains("Error");

                    Panel pnlDisk = new Panel
                    {
                        Width = flpDisks.Width - 25,
                        Margin = new Padding(3, 3, 3, 6),
                        BackColor = WinColor.White
                    };

                    Label lblName = new Label
                    {
                        Text = $"{(isUsb ? "🔌" : "💽")} {disk.Model} ({disk.SizeGB:F0} GB)",
                        AutoSize = true,
                        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                        ForeColor = isAlert ? WinColor.FromArgb(226, 30, 45) : WinColor.FromArgb(17, 35, 108),
                        Location = new Point(5, 5)
                    };
                    pnlDisk.Controls.Add(lblName);

                    int currentX = 25;
                    int badgeY = lblName.Bottom + 5;

                    Label lblStatus = CreateBadge(disk.State,
                        isAlert ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(220, 252, 231),
                        isAlert ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(21, 128, 61),
                        ref currentX, badgeY);
                    pnlDisk.Controls.Add(lblStatus);

                    if (disk.Temperature.HasValue)
                    {
                        WinColor bgTemp = disk.Temperature >= 55 ? WinColor.FromArgb(254, 240, 138) : WinColor.FromArgb(243, 244, 246);
                        WinColor fgTemp = disk.Temperature >= 55 ? WinColor.FromArgb(161, 98, 7) : WinColor.FromArgb(75, 85, 99);
                        Label lblTemp = CreateBadge($"🌡️ {disk.Temperature}°C", bgTemp, fgTemp, ref currentX, badgeY);
                        pnlDisk.Controls.Add(lblTemp);
                    }

                    if (disk.HoursUsed.HasValue)
                    {
                        Label lblHours = CreateBadge($"⏱️ {disk.HoursUsed}h", WinColor.FromArgb(243, 244, 246), WinColor.FromArgb(75, 85, 99), ref currentX, badgeY);
                        pnlDisk.Controls.Add(lblHours);
                    }

                    if (disk.HasHealthData)
                    {
                        WinColor bgHealth = disk.HealthPercent <= 20 ? WinColor.FromArgb(254, 226, 226) : WinColor.FromArgb(219, 234, 254);
                        WinColor fgHealth = disk.HealthPercent <= 20 ? WinColor.FromArgb(220, 38, 38) : WinColor.FromArgb(29, 78, 216);
                        Label lblHealth = CreateBadge($"❤️ {disk.HealthPercent}%", bgHealth, fgHealth, ref currentX, badgeY);
                        pnlDisk.Controls.Add(lblHealth);
                    }

                    pnlDisk.Height = lblStatus.Bottom + 8;
                    pnlDisk.Paint += (s, e) => { e.Graphics.DrawLine(Pens.Gainsboro, 0, pnlDisk.Height - 1, pnlDisk.Width, pnlDisk.Height - 1); };
                    flpDisks.Controls.Add(pnlDisk);
                }
            }
            AdjustDashboardPositions();
        }

        private Label CreateBadge(string text, WinColor background, WinColor textColor, ref int currentX, int y)
        {
            Label badge = new Label
            {
                Text = text,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                BackColor = background,
                ForeColor = textColor,
                Location = new Point(currentX, y),
                Padding = new Padding(2)
            };
            currentX += badge.PreferredSize.Width + 5;
            return badge;
        }

        private void cmbTechnician_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmbTechnician.SelectedIndex <= 0) { ShowDfPanel(false); return; }
            bool esDf = cmbTechnician.SelectedItem?.ToString().Contains("(DF-Server)") == true;
            ShowDfPanel(esDf);
        }

        private void ShowDfPanel(bool show)
        {
            DfPanel.Visible = show;
            if (!show) CleanData();
            RecalculateHeight();
        }

        private void RecalculateHeight()
        {
            if (this.WindowState == FormWindowState.Minimized) return;

            int RightBackground = rtbLog.Visible ? rtbLog.Bottom : pnlCardSmart.Bottom;
            int LetfBackground = btnDeviceManager.Bottom;

            int BottomContent = Math.Max(RightBackground, LetfBackground);

            int CurrentWidth = this.ClientSize.Width;

            if (!DfPanel.Visible)
            {
                int yBotones = BottomContent + 15;
                btnReport.Top = yBotones;
                btnAuto.Top = yBotones;
                this.ClientSize = new WinSize(CurrentWidth, btnReport.Bottom + 20);
                return;
            }

            int DfPanelHeight = chkCertificates.Checked ? DynamicCertsPanel.Bottom + 10 : chkCertificates.Bottom + 10;
            DfPanel.Height = DfPanelHeight;
            DfPanel.Top = BottomContent + 10;

            int yButtonsDF = DfPanel.Bottom + 15;
            btnReport.Top = yButtonsDF;
            btnAuto.Top = yButtonsDF;

            this.ClientSize = new WinSize(CurrentWidth, btnReport.Bottom + 20);
        }

        private void CleanData()
        {
            checkDigitization.Checked = false;
            chkSignatures.Checked = false;
            chkCertificates.Checked = false;
            SignaturesNum.Value = 0;
            CertificatesNum.Value = 1;
            RebuildCertificateFields(0);
        }

        private void checkDigitization_CheckedChanged(object sender, EventArgs e)
        {
            if (!checkDigitization.Checked) return;

            var resp = ModalMessage.Show(
                "¿La digitalización certificada está configurada y funcionando correctamente?",
                "Digitalización certificada",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (resp != DialogResult.Yes)
            {
                checkDigitization.CheckedChanged -= checkDigitization_CheckedChanged;
                checkDigitization.Checked = false;
                checkDigitization.CheckedChanged += checkDigitization_CheckedChanged;
            }
        }
        private void chkCertificates_CheckedChanged(object sender, EventArgs e)
        {
            bool active = chkCertificates.Checked;
            lblCertsNum.Enabled = active;
            CertificatesNum.Enabled = active;
            DynamicCertsPanel.Visible = active;
            if (!active) CertificatesNum.Value = 1;
            RebuildCertificateFields(active ? (int)CertificatesNum.Value : 0);
        }

        private void CertificatesNum_ValueChanged(object sender, EventArgs e)
        {
            if (chkCertificates.Checked) RebuildCertificateFields((int)CertificatesNum.Value);
        }

        private void RebuildCertificateFields(int quantity)
        {
            DynamicCertsPanel.Controls.Clear();
            _certControls.Clear();

            if (quantity == 0)
            {
                RecalculateHeight();
                return;
            }

            int currentY = 10;

            for (int i = 0; i < quantity; i++)
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

                var txtName = new TextBox
                {
                    PlaceholderText = "Nombre del certificado *",
                    Font = new Font("Segoe UI", 11f),
                    Location = new Point(currentX, currentY),
                    Width = 230,
                    BackColor = WinColor.FromArgb(255, 235, 235)
                };
                currentX += txtName.Width + 15;

                txtName.TextChanged += (s, ev) =>
                {
                    if (string.IsNullOrWhiteSpace(txtName.Text) || txtName.Text.Trim().Length < 10)
                        txtName.BackColor = WinColor.FromArgb(255, 235, 235);
                    else
                        txtName.BackColor = WinColor.White;
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

                var lblStateCert = new Label
                {
                    AutoSize = true,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Location = new Point(currentX, currentY + 3)
                };

                void UpdateCertState()
                {
                    int days = (dtp.Value.Date - DateTime.Today).Days;
                    if (days < 0)
                    {
                        lblStateCert.Text = "❌ Caducado";
                        lblStateCert.ForeColor = WinColor.FromArgb(226, 30, 45);
                    }
                    else if (days <= 92)
                    {
                        lblStateCert.Text = "⚠️ Caduca pronto";
                        lblStateCert.ForeColor = WinColor.FromArgb(220, 100, 0);
                    }
                    else
                    {
                        lblStateCert.Text = "✅ Válido";
                        lblStateCert.ForeColor = WinColor.FromArgb(34, 197, 94);
                    }
                }

                dtp.ValueChanged += (s, ev) => UpdateCertState();
                dtp.CloseUp += (s, ev) => UpdateCertState();
                dtp.KeyUp += (s, ev) => UpdateCertState();

                UpdateCertState();

                DynamicCertsPanel.Controls.AddRange(new Control[] { lblN, txtName, lblF, dtp, lblStateCert });
                _certControls.Add((txtName, dtp));

                currentY = txtName.Bottom + 12;
            }

            int maxH = 170;
            DynamicCertsPanel.Height = Math.Min(currentY + 5, maxH);
            RecalculateHeight();
        }

        private void ReadDfData()
        {
            _report.IsDfTechnician = DfPanel.Visible;
            if (!DfPanel.Visible) return;

            var df = _report.DfServer;
            df.HasCertifiedDigitization = checkDigitization.Checked;
            df.HasSignatures = chkSignatures.Checked;
            df.RemainingSignatures = chkSignatures.Checked ? (int)SignaturesNum.Value : 0;
            df.HasCertificates = chkCertificates.Checked;
            df.Certificates.Clear();

            if (chkCertificates.Checked)
            {
                foreach (var (txt, dtp) in _certControls)
                {
                    df.Certificates.Add(new DigitalCertificate
                    {
                        Name = txt.Text.Trim(),
                        ExpirationDate = dtp.Value.Date
                    });
                }
            }
        }

        private bool ValidateDfFields()
        {
            if (!_report.IsDfTechnician) return true;

            var df = _report.DfServer;
            int certIndex = 1;
            foreach (var cert in df.Certificates)
            {
                if (string.IsNullOrWhiteSpace(cert.Name) || cert.Name.Length < 10)
                {
                    ModalMessage.Show(
                        $"El nombre del Certificado {certIndex} no tiene una longitud válida (mínimo 10 caracteres).\n\nPor favor, escribe el nombre completo o el titular real.",
                        "Nombre de certificado inválido",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    Log($">>> Informe cancelado: nombre no válido o demasiado corto en el Certificado {certIndex}.\n");
                    return false;
                }

                if (cert.IsExpiringSoon)
                {
                    int days = (cert.ExpirationDate.Date - DateTime.Today).Days;
                    bool expired = days < 0;

                    string message = expired
                        ? $"❌  El certificado \"{cert.Name}\" caducó el " +
                          $"{cert.ExpirationDate:dd/MM/yyyy} (hace {Math.Abs(days)} días).\n\n" +
                          $"¿Se le ha avisado al cliente de que este certificado está caducado?"
                        : $"⚠️  El certificado \"{cert.Name}\" caduca el " +
                          $"{cert.ExpirationDate:dd/MM/yyyy} (en {days} días).\n\n" +
                          $"¿Se le ha avisado al cliente de que este certificado va a caducar próximamente?";

                    string title = expired ? "Certificado caducado" : "Certificado próximo a caducar";

                    var resp = ModalMessage.Show(
                        message, title,
                        MessageBoxButtons.YesNo,
                        expired ? MessageBoxIcon.Error : MessageBoxIcon.Warning);

                    if (resp != DialogResult.Yes)
                    {
                        string reason = expired
                            ? "el cliente no ha sido avisado del certificado caducado"
                            : "el cliente no ha sido avisado del certificado próximo a caducar";
                        Log($">>> Informe cancelado: {reason}.\n");
                        return false;
                    }
                }
                certIndex++;
            }

            if (df.HasSignatures && df.RemainingSignatures <= 100)
            {
                var resp = ModalMessage.Show(
                    $"⚠️  Quedan solo {df.RemainingSignatures} firmas de DF-Signature disponibles.\n\n" +
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

        private void Log(string text)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(() => Log(text)));
                return;
            }

            if (string.IsNullOrEmpty(text)) return;
            string line = text.TrimEnd('\n', '\r');

            if (line.Contains("✅"))
            { Write(text, ClrOk, line.TrimStart().StartsWith(">>>"), 10f); return; }

            if (line.Contains("⚠️") || line.Contains("🔁"))
            { Write(text, ClrAviso, line.TrimStart().StartsWith(">>>"), 10f); return; }

            if (line.Contains("❌"))
            { Write(text, ClrError, line.TrimStart().StartsWith(">>>"), 10f); return; }

            if (line.Contains("⚡") || line.Contains("⬇️") || line.Contains("⚙️") || line.Contains("🔎") || line.Contains("ℹ️"))
            { Write(text, WinColor.FromArgb(197, 134, 192), true, 10f); return; }

            if (line.Contains("sin acceso") || line.Contains("No se pudo") || line.Contains("ALERTA"))
            { Write(text, ClrError, false, 10f); return; }

            if (line.Contains("🔴 IMPORTANTE"))
            { Write(text, WinColor.FromArgb(226, 30, 45), true, 10f); return; }

            if (line.Contains("🔵 OPCIONAL"))
            { Write(text, WinColor.FromArgb(86, 156, 214), true, 10f); return; }

            if (line.TrimStart().StartsWith(">>>"))
            { Write(text, ClrSection, true, 10f); return; }

            if (line.TrimStart().StartsWith("·") || line.StartsWith("    ·"))
            { Write(text, ClrDetail, false, 9.5f); return; }

            if (line.StartsWith("      ") || line.TrimStart().StartsWith("Código"))
            { Write(text, ClrSubdetail, false, 9f); return; }

            Write(text, ClrText, false, 10f);
        }

        private void LogBanner(string title, WinColor background, WinColor text)
        {
            rtbLog.SuspendLayout();
            Write("\n", ClrText, false, 10f);
            string contenido = $"  {title}  ".PadRight(64);
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;

            WinColor bannerFondoDark = WinColor.FromArgb(background.R / 2, background.G / 2, background.B / 2);
            rtbLog.SelectionBackColor = bannerFondoDark;

            rtbLog.SelectionColor = WinColor.White;
            rtbLog.SelectionFont = new Font("Consolas", 10.5f, FontStyle.Bold);
            rtbLog.AppendText(contenido + "\n");
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = ClrText;
            rtbLog.SelectionFont = rtbLog.Font;
            Write("\n", ClrText, false, 10f);
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        private void Write(string text, WinColor color, bool bold, float size)
        {
            if (rtbLog.InvokeRequired)
            {
                rtbLog.BeginInvoke(new Action(() => Write(text, color, bold, size)));
                return;
            }

            rtbLog.SuspendLayout();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.SelectionLength = 0;
            rtbLog.SelectionBackColor = ClrFondo;
            rtbLog.SelectionColor = color;
            rtbLog.SelectionFont = new Font("Consolas", size, bold ? FontStyle.Bold : FontStyle.Regular);
            rtbLog.AppendText(text);
            rtbLog.SelectionColor = ClrText;
            rtbLog.SelectionFont = rtbLog.Font;
            rtbLog.ResumeLayout();
            rtbLog.ScrollToCaret();
        }

        private void SetButtonsEnabled(bool enabled)
        {
            // Si bloqueamos los botones (enabled=false), significa que hay un proceso en ejecución.
            _isProcessRunning = !enabled;

            btnCleanTemp.Enabled = enabled;
            btnSmart.Enabled = enabled;
            btnUpdate.Enabled = enabled;
            btnInstallUpdates.Enabled = enabled;
            btnDrivers.Enabled = enabled;

            btnOpenUpdate.Enabled = true;
            btnDeviceManager.Enabled = true;

            btnReport.Enabled = enabled;
            btnAuto.Enabled = enabled;
        }

        private async Task InitialScanAsync()
        {
            SetButtonsEnabled(false);

            LogBanner("DIAGNÓSTICO INICIAL DEL SISTEMA",
                WinColor.FromArgb(28, 78, 170), WinColor.White);

            Log("  Revisando el estado del equipo antes de comenzar...\n\n");

            Log(">>> Comprobando actualizaciones de Windows...\n");
            await UpdateService.AnalizarAsync(_report, Log);

            Log("\n>>> Comprobando controladores del sistema...\n");
            _report.Drivers.Clear();
            var drivers = await DriverService.ScanAsync(Log);
            _report.Drivers.AddRange(drivers);

            bool HasProblems = _report.ImportantUpdates > 0
                             || _report.IsRestartRequired
                             || _report.Drivers.Count > 0;

            if (HasProblems)
                LogBanner("⚠️   Se han detectado elementos que requieren atención",
                    WinColor.FromArgb(170, 90, 0), WinColor.White);
            else
                LogBanner("✅   Sistema actualizado",
                    WinColor.FromArgb(20, 120, 55), WinColor.White);

            _report.IsUpdatesExecuted = true;
            _report.IsDriversExecuted = true;

            UpdateDashboard();
            SetButtonsEnabled(true);
        }

        private bool SelectedTechnician()
        {
            if (cmbTechnician.SelectedIndex > 0) return true;

            ModalMessage.Show(
                "Por favor, selecciona un técnico responsable antes de continuar.",
                "Técnico no seleccionado",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        private async void btnCleanTemp_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false); await CleaningProcess(); SetButtonsEnabled(true);
        }

        private async void btnSmart_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false); await SmartProcess(); SetButtonsEnabled(true);
        }

        private async void btnUpdate_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false); await UpdatesProcess(); SetButtonsEnabled(true);
        }

        private async void btnInstalarUpdates_Click(object sender, EventArgs e)
        {
            // --- Comprobar si realmente hay algo que instalar ---
            int totalUpdates = _report.ImportantUpdates + _report.OptionalUpdates;

            if (totalUpdates == 0)
            {
                ModalMessage.Show(
                    "El sistema ya está al día. No hay actualizaciones pendientes para instalar.",
                    "Todo actualizado",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            // --- Modal de confirmación anti-clics accidentales ---
            var confirmation = ModalMessage.Show(
                $"Se van a descargar e instalar {totalUpdates} actualización(es) de Windows.\n\n¿Estás seguro de que deseas continuar?\nEste proceso se realizará de fondo y puede tardar varios minutos.",
                "Confirmar instalación",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirmation != DialogResult.Yes)
            {
                Log(">>> Instalación de actualizaciones cancelada por el usuario.\n");
                return;
            }

            SetButtonsEnabled(false);

            // Hacemos una copia exacta de la lista de nombres de las actualizaciones pendientes
            var PreviousUpdates = new List<string>(_report.UpdateNames);

            LogBanner("INSTALANDO ACTUALIZACIONES", WinColor.FromArgb(50, 60, 100), WinColor.White);

            await UpdateService.InstallAsync(_report, Log);

            // ---> AÑADIMOS UN RESPIRO DE 3 SEGUNDOS PARA QUE EL SERVICIO SE ESTABILICE <---
            await Task.Delay(3000);

            Log("\n>>> Re-analizando el estado tras la instalación...\n");
            await UpdateService.AnalizarAsync(_report, Log);

            // Si después de instalar, Windows pide reiniciar Y AÚN quedan actualizaciones pendientes...
            if (_report.IsRestartRequired && _report.UpdateNames.Count > 0)
            {
                // Comparamos a ver si alguna de las que quedan ahora ya estaba en la lista del principio
                bool HasStuckUpdates = _report.UpdateNames.Any(upd => PreviousUpdates.Contains(upd));

                if (HasStuckUpdates)
                {
                    Log(">>> ⚠️ AVISO DE INSTALACIÓN PARCIAL:\n");
                    Log("    · Algunas actualizaciones no se han podido instalar.\n");
                    Log("    · Puede ser que Windows haya bloqueado la instalación porque exige un reinicio previo del servidor.\n");
                    Log("    · Por favor, reinicia el equipo y vuelve a ejecutar la herramienta para instalar las restantes.\n\n");
                }
            }

            UpdateDashboard();
            SetButtonsEnabled(true);
        }

        private async void btnDrivers_Click(object sender, EventArgs e)
        {
            SetButtonsEnabled(false); await DriversProcess(); SetButtonsEnabled(true);
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
            if (!SelectedTechnician()) return;

            // Bloqueamos el desplegable del técnico
            cmbTechnician.Enabled = false;
            SetButtonsEnabled(false);

            LogBanner("⚡   REALIZANDO PREVENTIVA COMPLETA",
                WinColor.FromArgb(100, 30, 180), WinColor.White);

            if (cmbTechnician?.SelectedItem != null)
                _report.AssignedTechnician = cmbTechnician.SelectedItem.ToString();

            await SmartProcess();
            await CleaningProcess();
            await UpdatesProcess();
            await DriversProcess();
            await PDFProcess();

            LogBanner("✅   Mantenimiento automático finalizado",
                WinColor.FromArgb(20, 120, 55), WinColor.White);

            SetButtonsEnabled(true);
            // Volvemos a habilitarlo al terminar
            cmbTechnician.Enabled = true;
        }

        private async Task CleaningProcess()
        {
            LogBanner("LIMPIEZA DE ARCHIVOS TEMPORALES",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            int DeletedFiles = 0;
            long FreedBytes = 0;

            var Paths = new List<(string Ruta, string Nombre)>();
            string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);

            Paths.Add((Path.Combine(winDir, "Temp"), "Temp de Windows"));
            Paths.Add((Path.Combine(winDir, @"SoftwareDistribution\Download"), "Caché de Windows Update"));

            try
            {
                string sysDrive = Path.GetPathRoot(winDir);
                string baseUsers = Path.Combine(sysDrive, "Users");

                if (Directory.Exists(baseUsers))
                {
                    foreach (var dirUser in Directory.GetDirectories(baseUsers))
                    {
                        string nameUser = new DirectoryInfo(dirUser).Name;

                        if (nameUser.Equals("Public", StringComparison.OrdinalIgnoreCase) ||
                            nameUser.Equals("Default", StringComparison.OrdinalIgnoreCase) ||
                            nameUser.Equals("Default User", StringComparison.OrdinalIgnoreCase) ||
                            nameUser.Equals("All Users", StringComparison.OrdinalIgnoreCase))
                            continue;

                        string tempUserPath = Path.Combine(dirUser, @"AppData\Local\Temp");

                        if (Directory.Exists(tempUserPath))
                        {
                            Paths.Add((tempUserPath, $"Temp de usuario ({nameUser})"));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"    · ⚠️ No se pudieron revisar los perfiles de usuario: {ex.Message}\n");
            }

            foreach (var (path, name) in Paths)
            {
                int arch = 0; long bytes = 0;
                try { (arch, bytes) = await Task.Run(() => CleanService.CleanDirectory(path)); }
                catch (Exception ex)
                {
                    Log($"    · {name}: sin acceso ({ex.GetType().Name})\n");
                    continue;
                }
                DeletedFiles += arch;
                FreedBytes += bytes;

                if (arch >= 0 || name.Contains("Windows") || name.Contains("Caché"))
                {
                    Log($"    · {name}: {arch} archivos — {bytes / 1048576.0:F1} MB\n");
                }
            }

            _report.DeletedFiles = DeletedFiles;
            _report.FreedBytes = FreedBytes;
            _report.IsCleanupExecuted = true;
            Log($">>> ✅ Limpieza completada: {DeletedFiles} archivos | {FreedBytes / 1048576.0:F2} MB liberados\n");

            UpdateDashboard();
        }

        private async Task SmartProcess()
        {
            LogBanner("DIAGNÓSTICO S.M.A.R.T. DE DISCOS",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _report.Disks.Clear();
            var discos = await SmartService.ObtenerDiscosAsync(Log);
            _report.Disks.AddRange(discos);

            UpdateDashboard();
        }

        private async Task UpdatesProcess()
        {
            LogBanner("ANÁLISIS DE WINDOWS UPDATE ⏳",
                WinColor.FromArgb(50, 60, 100), WinColor.White);
            await UpdateService.AnalizarAsync(_report, Log);

            UpdateDashboard();
        }

        private async Task DriversProcess()
        {
            LogBanner("ANÁLISIS DE CONTROLADORES ⏳",
                WinColor.FromArgb(50, 60, 100), WinColor.White);

            _report.Drivers.Clear();
            var drivers = await DriverService.ScanAsync(Log);
            _report.Drivers.AddRange(drivers);

            _report.IsDriversExecuted = true;

            UpdateDashboard();
        }

        // ── Manejo de PDF con advertencia de actualizaciones ──────────────
        private async Task PDFProcess()
        {
            LogBanner("PREPARANDO INFORME PDF", WinColor.FromArgb(50, 60, 100), WinColor.White);

            if (cmbTechnician?.SelectedItem != null)
                _report.AssignedTechnician = cmbTechnician.SelectedItem.ToString();

            ReadDfData();
            if (!ValidateDfFields()) return;

            // --- LÓGICA DE BACKUP Y RESTAURACIÓN ---
            bool restoreUpdates = false;
            int bkImportantUpdates = _report.ImportantUpdates;
            int bkOptionalUpdates = _report.OptionalUpdates;
            bool bkIsRestartRequired = _report.IsRestartRequired;
            List<string> bkUpdateNames = new List<string>(_report.UpdateNames);

            bool restoreDrivers = false;
            List<DriverInfo> bkDrivers = new List<DriverInfo>(_report.Drivers);

            // --- GESTIÓN DE ALERTAS DE WINDOWS UPDATE ---
            int totalUpdates = _report.ImportantUpdates + _report.OptionalUpdates;
            if (totalUpdates > 0 || _report.IsRestartRequired)
            {
                string message = totalUpdates > 0 && _report.IsRestartRequired
                    ? $"Hay {totalUpdates} actualización(es) y un REINICIO pendiente."
                    : totalUpdates > 0 ? $"Hay {totalUpdates} actualización(es) pendiente(s)." : "Hay un REINICIO pendiente.";

                var resp = ModalMessage.Show($"{message}\n\n¿Quieres que estas alertas se reflejen en el informe PDF?",
                    "Windows Update Pendiente", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (resp == DialogResult.Cancel)
                {
                    Log(">>> Generación de PDF cancelada por el usuario.\n");
                    return;
                }

                if (resp == DialogResult.No)
                {
                    _report.ImportantUpdates = 0; _report.OptionalUpdates = 0;
                    _report.UpdateNames.Clear(); _report.IsRestartRequired = false;
                    restoreUpdates = true;
                    Log("    · Nota: Se omitirán las alertas de Windows Update en el PDF.\n");
                }
            }

            // --- GESTIÓN DE ALERTAS DE DRIVERS ---
            if (_report.Drivers.Count > 0)
            {
                string driversMessage = _report.Drivers.Count == 1
                    ? "Hay 1 controlador con error."
                    : $"Hay {_report.Drivers.Count} controladores con errores.";

                var resp = ModalMessage.Show(
                    $"{driversMessage}\n\n¿Quieres que estos errores aparezcan en el informe PDF?",
                    "Controladores con errores", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (resp == DialogResult.Cancel)
                {
                    if (restoreUpdates)
                    {
                        _report.ImportantUpdates = bkImportantUpdates; _report.OptionalUpdates = bkOptionalUpdates;
                        _report.IsRestartRequired = bkIsRestartRequired; _report.UpdateNames = bkUpdateNames;
                    }
                    Log(">>> Generación de PDF cancelada por el usuario.\n");
                    return;
                }

                if (resp == DialogResult.No)
                {
                    _report.Drivers.Clear();
                    restoreDrivers = true;
                    Log("    · Nota: Se omitirán los errores de controladores en el PDF.\n");
                }
            }

            // --- PROCESO DE GENERACIÓN ---
            if (_report.Disks.Count == 0) await SmartProcess();

            Log(">>> Recopilando telemetría del sistema...\n");
            await Task.Run(() => TelemetryService.RecopilarTelemetria(_report, Log));
            await TelemetryService.FetchJavaAsync(_report, Log, _http);

            using var sfd = new SaveFileDialog
            {
                Filter = "Archivos PDF (*.pdf)|*.pdf",
                Title = "Guardar Informe de Mantenimiento",
                FileName = $"Informe_Sistema_{DateTime.Now:dd_MM_yyyy}_{_report.ServerName}"
            };

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                string filePath = sfd.FileName;
                try
                {
                    byte[] logoBytes = ImageToBytes(Properties.Resources.copicanariasicon);
                    byte[] dfLogoBytes = _report.IsDfTechnician ? ImageToBytes(Properties.Resources.DF_SERVER_logo_300x60) : null;

                    await Task.Run(() => PdfGenerator.Generate(filePath, _report, logoBytes, dfLogoBytes));

                    Log($">>> ✅ PDF guardado en: {filePath}\n");
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = filePath, UseShellExecute = true });
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
            if (restoreUpdates)
            {
                _report.ImportantUpdates = bkImportantUpdates; _report.OptionalUpdates = bkOptionalUpdates;
                _report.IsRestartRequired = bkIsRestartRequired; _report.UpdateNames = bkUpdateNames;
            }
            if (restoreDrivers)
            {
                _report.Drivers.AddRange(bkDrivers);
            }
        }

        // Helper rápido para limpiar el código de arriba
        private byte[] ImageToBytes(System.Drawing.Image img)
        {
            if (img == null) return null;
            using var ms = new MemoryStream();
            img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }

        private async void btnReport_Click(object sender, EventArgs e)
        {
            if (!SelectedTechnician()) return;

            // Bloqueamos el desplegable del técnico
            cmbTechnician.Enabled = false;
            SetButtonsEnabled(false);

            await PDFProcess();

            SetButtonsEnabled(true);
            // Volvemos a habilitarlo al terminar
            cmbTechnician.Enabled = true;
        }
    }
}