namespace CopicanariasServerReport
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            panelHeader = new Panel();
            btnToggleLog = new BotonModerno();
            lblTituloCabecera = new Label();
            pictureBoxLogo = new PictureBox();
            lblTecnico = new Label();
            cmbTecnico = new ComboBox();
            btnCleanTemp = new BotonModerno();
            btnSmart = new BotonModerno();
            btnUpdate = new BotonModerno();
            btnAbrirUpdate = new BotonModerno();
            btnDrivers = new BotonModerno();
            btnDeviceManager = new BotonModerno();
            btnReport = new BotonModerno();
            btnAuto = new BotonModerno();
            rtbLog = new RichTextBox();
            panelDF = new Panel();
            pictureBox1 = new PictureBox();
            lblDfTitulo = new Label();
            panelDfLinea = new Panel();
            chkDigitalizacion = new CheckBox();
            chkFirmas = new CheckBox();
            lblFirmasRestantes = new Label();
            numFirmas = new NumericUpDown();
            chkCertificados = new CheckBox();
            lblNumCerts = new Label();
            numCertificados = new NumericUpDown();
            panelCertsDinamico = new Panel();
            pnlCardUpd = new Panel();
            lblIconUpd = new Label();
            lblTitUpd = new Label();
            lblValUpd = new Label();
            pnlCardDrv = new Panel();
            lblIconDrv = new Label();
            lblTitDrv = new Label();
            lblValDrv = new Label();
            pnlCardTmp = new Panel();
            lblIconTmp = new Label();
            lblTitTmp = new Label();
            lblValTmp = new Label();
            pnlCardSmart = new Panel();
            lblIconSmart = new Label();
            lblTitSmart = new Label();
            lblValSmart = new Label();
            flpDiscos = new FlowLayoutPanel();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            panelDF.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFirmas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCertificados).BeginInit();
            pnlCardUpd.SuspendLayout();
            pnlCardDrv.SuspendLayout();
            pnlCardTmp.SuspendLayout();
            pnlCardSmart.SuspendLayout();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(btnToggleLog);
            panelHeader.Controls.Add(lblTituloCabecera);
            panelHeader.Controls.Add(pictureBoxLogo);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(820, 93);
            panelHeader.TabIndex = 16;
            // 
            // btnToggleLog
            // 
            btnToggleLog.BackColor = Color.FromArgb(100, 100, 100);
            btnToggleLog.FlatStyle = FlatStyle.Flat;
            btnToggleLog.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnToggleLog.ForeColor = Color.White;
            btnToggleLog.Location = new Point(640, 30);
            btnToggleLog.Name = "btnToggleLog";
            btnToggleLog.Size = new Size(150, 35);
            btnToggleLog.TabIndex = 0;
            btnToggleLog.Text = "👁 Ver Log Técnico";
            btnToggleLog.UseVisualStyleBackColor = false;
            btnToggleLog.Click += btnToggleLog_Click;
            // 
            // lblTituloCabecera
            // 
            lblTituloCabecera.AutoSize = true;
            lblTituloCabecera.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTituloCabecera.ForeColor = Color.FromArgb(17, 35, 108);
            lblTituloCabecera.Location = new Point(265, 28);
            lblTituloCabecera.Name = "lblTituloCabecera";
            lblTituloCabecera.Size = new Size(218, 30);
            lblTituloCabecera.TabIndex = 1;
            lblTituloCabecera.Text = "Panel de Preventiva";
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.Image = Properties.Resources.copicanariasicon;
            pictureBoxLogo.Location = new Point(103, 0);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new Size(145, 93);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.TabIndex = 2;
            pictureBoxLogo.TabStop = false;
            // 
            // lblTecnico
            // 
            lblTecnico.AutoSize = true;
            lblTecnico.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTecnico.ForeColor = Color.FromArgb(64, 64, 64);
            lblTecnico.Location = new Point(30, 115);
            lblTecnico.Name = "lblTecnico";
            lblTecnico.Size = new Size(124, 15);
            lblTecnico.TabIndex = 9;
            lblTecnico.Text = "Técnico Responsable:";
            // 
            // cmbTecnico
            // 
            cmbTecnico.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTecnico.Font = new Font("Segoe UI", 10F);
            cmbTecnico.Location = new Point(30, 135);
            cmbTecnico.Name = "cmbTecnico";
            cmbTecnico.Size = new Size(300, 25);
            cmbTecnico.TabIndex = 8;
            cmbTecnico.SelectedIndexChanged += cmbTecnico_SelectedIndexChanged;
            // 
            // btnCleanTemp
            // 
            btnCleanTemp.BackColor = Color.FromArgb(17, 35, 108);
            btnCleanTemp.FlatStyle = FlatStyle.Flat;
            btnCleanTemp.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCleanTemp.ForeColor = Color.White;
            btnCleanTemp.Location = new Point(30, 240);
            btnCleanTemp.Name = "btnCleanTemp";
            btnCleanTemp.Size = new Size(300, 40);
            btnCleanTemp.TabIndex = 15;
            btnCleanTemp.Text = "Limpiar Archivos Temporales";
            btnCleanTemp.UseVisualStyleBackColor = false;
            btnCleanTemp.Click += btnCleanTemp_Click;
            // 
            // btnSmart
            // 
            btnSmart.BackColor = Color.FromArgb(17, 35, 108);
            btnSmart.FlatStyle = FlatStyle.Flat;
            btnSmart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSmart.ForeColor = Color.White;
            btnSmart.Location = new Point(30, 180);
            btnSmart.Name = "btnSmart";
            btnSmart.Size = new Size(300, 40);
            btnSmart.TabIndex = 14;
            btnSmart.Text = "Test S.M.A.R.T. de Discos";
            btnSmart.UseVisualStyleBackColor = false;
            btnSmart.Click += btnSmart_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.BackColor = Color.FromArgb(17, 35, 108);
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUpdate.ForeColor = Color.White;
            btnUpdate.Location = new Point(30, 305);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(300, 40);
            btnUpdate.TabIndex = 13;
            btnUpdate.Text = "🔄 Re-analizar Windows Update";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnAbrirUpdate
            // 
            btnAbrirUpdate.BackColor = Color.White;
            btnAbrirUpdate.FlatAppearance.BorderColor = Color.FromArgb(17, 35, 108);
            btnAbrirUpdate.FlatStyle = FlatStyle.Flat;
            btnAbrirUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAbrirUpdate.ForeColor = Color.FromArgb(17, 35, 108);
            btnAbrirUpdate.Location = new Point(30, 351);
            btnAbrirUpdate.Name = "btnAbrirUpdate";
            btnAbrirUpdate.Size = new Size(300, 35);
            btnAbrirUpdate.TabIndex = 7;
            btnAbrirUpdate.Text = "Abrir Panel de Windows Update";
            btnAbrirUpdate.UseVisualStyleBackColor = false;
            btnAbrirUpdate.Click += btnAbrirUpdate_Click;
            // 
            // btnDrivers
            // 
            btnDrivers.BackColor = Color.FromArgb(17, 35, 108);
            btnDrivers.FlatStyle = FlatStyle.Flat;
            btnDrivers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDrivers.ForeColor = Color.White;
            btnDrivers.Location = new Point(30, 405);
            btnDrivers.Name = "btnDrivers";
            btnDrivers.Size = new Size(300, 40);
            btnDrivers.TabIndex = 6;
            btnDrivers.Text = "🔄 Re-escanear Drivers";
            btnDrivers.UseVisualStyleBackColor = false;
            btnDrivers.Click += btnDrivers_Click;
            // 
            // btnDeviceManager
            // 
            btnDeviceManager.BackColor = Color.White;
            btnDeviceManager.FlatAppearance.BorderColor = Color.FromArgb(17, 35, 108);
            btnDeviceManager.FlatStyle = FlatStyle.Flat;
            btnDeviceManager.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDeviceManager.ForeColor = Color.FromArgb(17, 35, 108);
            btnDeviceManager.Location = new Point(30, 451);
            btnDeviceManager.Name = "btnDeviceManager";
            btnDeviceManager.Size = new Size(300, 35);
            btnDeviceManager.TabIndex = 5;
            btnDeviceManager.Text = "Abrir Administrador de Dispositivos";
            btnDeviceManager.UseVisualStyleBackColor = false;
            btnDeviceManager.Click += btnDeviceManager_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = Color.FromArgb(34, 197, 94);
            btnReport.FlatStyle = FlatStyle.Flat;
            btnReport.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnReport.ForeColor = Color.White;
            btnReport.Location = new Point(30, 510);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(300, 50);
            btnReport.TabIndex = 12;
            btnReport.Text = "📄 Generar Informe PDF";
            btnReport.UseVisualStyleBackColor = false;
            btnReport.Click += btnReport_Click;
            // 
            // btnAuto
            // 
            btnAuto.BackColor = Color.FromArgb(226, 30, 45);
            btnAuto.FlatStyle = FlatStyle.Flat;
            btnAuto.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnAuto.ForeColor = Color.White;
            btnAuto.Location = new Point(350, 510);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(440, 50);
            btnAuto.TabIndex = 10;
            btnAuto.Text = "⚡ Realizar Preventiva Completa ⚡";
            btnAuto.UseVisualStyleBackColor = false;
            btnAuto.Click += btnAuto_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.FromArgb(30, 30, 30);
            rtbLog.BorderStyle = BorderStyle.None;
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.ForeColor = Color.FromArgb(212, 212, 212);
            rtbLog.Location = new Point(350, 115);
            rtbLog.Name = "rtbLog";
            rtbLog.Size = new Size(440, 380);
            rtbLog.TabIndex = 11;
            rtbLog.Text = "";
            rtbLog.Visible = false;
            // 
            // panelDF
            // 
            panelDF.BackColor = Color.White;
            panelDF.Controls.Add(pictureBox1);
            panelDF.Controls.Add(lblDfTitulo);
            panelDF.Controls.Add(panelDfLinea);
            panelDF.Controls.Add(chkDigitalizacion);
            panelDF.Controls.Add(chkFirmas);
            panelDF.Controls.Add(lblFirmasRestantes);
            panelDF.Controls.Add(numFirmas);
            panelDF.Controls.Add(chkCertificados);
            panelDF.Controls.Add(lblNumCerts);
            panelDF.Controls.Add(numCertificados);
            panelDF.Controls.Add(panelCertsDinamico);
            panelDF.Location = new Point(30, 580);
            panelDF.Name = "panelDF";
            panelDF.Size = new Size(760, 122);
            panelDF.TabIndex = 4;
            panelDF.Visible = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.DF_SERVER_logo_300x60;
            pictureBox1.Location = new Point(280, -1);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(232, 59);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 0;
            pictureBox1.TabStop = false;
            // 
            // lblDfTitulo
            // 
            lblDfTitulo.AutoSize = true;
            lblDfTitulo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDfTitulo.ForeColor = Color.FromArgb(17, 35, 108);
            lblDfTitulo.Location = new Point(10, 8);
            lblDfTitulo.Name = "lblDfTitulo";
            lblDfTitulo.Size = new Size(156, 19);
            lblDfTitulo.TabIndex = 1;
            lblDfTitulo.Text = "🔷 Sección DF-Server";
            // 
            // panelDfLinea
            // 
            panelDfLinea.BackColor = Color.FromArgb(17, 35, 108);
            panelDfLinea.Location = new Point(10, 30);
            panelDfLinea.Name = "panelDfLinea";
            panelDfLinea.Size = new Size(740, 2);
            panelDfLinea.TabIndex = 2;
            // 
            // chkDigitalizacion
            // 
            chkDigitalizacion.AutoSize = true;
            chkDigitalizacion.Font = new Font("Segoe UI", 9.5F);
            chkDigitalizacion.Location = new Point(10, 42);
            chkDigitalizacion.Name = "chkDigitalizacion";
            chkDigitalizacion.Size = new Size(214, 21);
            chkDigitalizacion.TabIndex = 3;
            chkDigitalizacion.Text = "¿Tiene digitalización certificada?";
            chkDigitalizacion.CheckedChanged += chkDigitalizacion_CheckedChanged;
            // 
            // chkFirmas
            // 
            chkFirmas.AutoSize = true;
            chkFirmas.Font = new Font("Segoe UI", 9.5F);
            chkFirmas.Location = new Point(10, 74);
            chkFirmas.Name = "chkFirmas";
            chkFirmas.Size = new Size(196, 21);
            chkFirmas.TabIndex = 4;
            chkFirmas.Text = "Tiene firmas de DF-Signature";
            chkFirmas.CheckedChanged += chkFirmas_CheckedChanged;
            // 
            // lblFirmasRestantes
            // 
            lblFirmasRestantes.AutoSize = true;
            lblFirmasRestantes.Enabled = false;
            lblFirmasRestantes.Font = new Font("Segoe UI", 9F);
            lblFirmasRestantes.ForeColor = Color.DimGray;
            lblFirmasRestantes.Location = new Point(242, 77);
            lblFirmasRestantes.Name = "lblFirmasRestantes";
            lblFirmasRestantes.Size = new Size(95, 15);
            lblFirmasRestantes.TabIndex = 5;
            lblFirmasRestantes.Text = "Firmas restantes:";
            // 
            // numFirmas
            // 
            numFirmas.Enabled = false;
            numFirmas.Font = new Font("Segoe UI", 9.5F);
            numFirmas.Location = new Point(352, 74);
            numFirmas.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            numFirmas.Name = "numFirmas";
            numFirmas.Size = new Size(90, 24);
            numFirmas.TabIndex = 6;
            // 
            // chkCertificados
            // 
            chkCertificados.AutoSize = true;
            chkCertificados.Font = new Font("Segoe UI", 9.5F);
            chkCertificados.Location = new Point(10, 108);
            chkCertificados.Name = "chkCertificados";
            chkCertificados.Size = new Size(182, 21);
            chkCertificados.TabIndex = 7;
            chkCertificados.Text = "Tiene certificados digitales";
            chkCertificados.CheckedChanged += chkCertificados_CheckedChanged;
            // 
            // lblNumCerts
            // 
            lblNumCerts.AutoSize = true;
            lblNumCerts.Enabled = false;
            lblNumCerts.Font = new Font("Segoe UI", 9F);
            lblNumCerts.ForeColor = Color.DimGray;
            lblNumCerts.Location = new Point(242, 111);
            lblNumCerts.Name = "lblNumCerts";
            lblNumCerts.Size = new Size(104, 15);
            lblNumCerts.TabIndex = 8;
            lblNumCerts.Text = "Nº de certificados:";
            // 
            // numCertificados
            // 
            numCertificados.Enabled = false;
            numCertificados.Font = new Font("Segoe UI", 9.5F);
            numCertificados.Location = new Point(352, 108);
            numCertificados.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            numCertificados.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            numCertificados.Name = "numCertificados";
            numCertificados.Size = new Size(60, 24);
            numCertificados.TabIndex = 9;
            numCertificados.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numCertificados.ValueChanged += numCertificados_ValueChanged;
            // 
            // panelCertsDinamico
            // 
            panelCertsDinamico.AutoScroll = true;
            panelCertsDinamico.BackColor = Color.FromArgb(240, 242, 245);
            panelCertsDinamico.BorderStyle = BorderStyle.FixedSingle;
            panelCertsDinamico.Location = new Point(10, 140);
            panelCertsDinamico.Name = "panelCertsDinamico";
            panelCertsDinamico.Size = new Size(740, 112);
            panelCertsDinamico.TabIndex = 10;
            panelCertsDinamico.Visible = false;
            // 
            // pnlCardUpd
            // 
            pnlCardUpd.BackColor = Color.White;
            pnlCardUpd.Controls.Add(lblIconUpd);
            pnlCardUpd.Controls.Add(lblTitUpd);
            pnlCardUpd.Controls.Add(lblValUpd);
            pnlCardUpd.Location = new Point(350, 115);
            pnlCardUpd.Name = "pnlCardUpd";
            pnlCardUpd.Size = new Size(215, 99);
            pnlCardUpd.TabIndex = 0;
            // 
            // lblIconUpd
            // 
            lblIconUpd.AutoSize = true;
            lblIconUpd.Font = new Font("Segoe UI", 24F);
            lblIconUpd.ForeColor = Color.FromArgb(17, 35, 108);
            lblIconUpd.Location = new Point(10, 10);
            lblIconUpd.Name = "lblIconUpd";
            lblIconUpd.Size = new Size(64, 45);
            lblIconUpd.TabIndex = 0;
            lblIconUpd.Text = "🔄";
            // 
            // lblTitUpd
            // 
            lblTitUpd.AutoSize = true;
            lblTitUpd.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitUpd.ForeColor = Color.Gray;
            lblTitUpd.Location = new Point(80, 20);
            lblTitUpd.Name = "lblTitUpd";
            lblTitUpd.Size = new Size(102, 15);
            lblTitUpd.TabIndex = 1;
            lblTitUpd.Text = "Windows Update";
            // 
            // lblValUpd
            // 
            lblValUpd.AutoSize = true;
            lblValUpd.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblValUpd.ForeColor = Color.FromArgb(17, 35, 108);
            lblValUpd.Location = new Point(80, 45);
            lblValUpd.Name = "lblValUpd";
            lblValUpd.Size = new Size(94, 20);
            lblValUpd.TabIndex = 2;
            lblValUpd.Text = "Esperando...";
            lblValUpd.Click += lblValUpd_Click;
            // 
            // pnlCardDrv
            // 
            pnlCardDrv.BackColor = Color.White;
            pnlCardDrv.Controls.Add(lblIconDrv);
            pnlCardDrv.Controls.Add(lblTitDrv);
            pnlCardDrv.Controls.Add(lblValDrv);
            pnlCardDrv.Location = new Point(571, 115);
            pnlCardDrv.Name = "pnlCardDrv";
            pnlCardDrv.Size = new Size(219, 99);
            pnlCardDrv.TabIndex = 1;
            // 
            // lblIconDrv
            // 
            lblIconDrv.AutoSize = true;
            lblIconDrv.Font = new Font("Segoe UI", 24F);
            lblIconDrv.ForeColor = Color.FromArgb(17, 35, 108);
            lblIconDrv.Location = new Point(10, 10);
            lblIconDrv.Name = "lblIconDrv";
            lblIconDrv.Size = new Size(52, 45);
            lblIconDrv.TabIndex = 0;
            lblIconDrv.Text = "🖧";
            // 
            // lblTitDrv
            // 
            lblTitDrv.AutoSize = true;
            lblTitDrv.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitDrv.ForeColor = Color.Gray;
            lblTitDrv.Location = new Point(60, 20);
            lblTitDrv.Name = "lblTitDrv";
            lblTitDrv.Size = new Size(85, 15);
            lblTitDrv.TabIndex = 1;
            lblTitDrv.Text = "Controladores";
            // 
            // lblValDrv
            // 
            lblValDrv.AutoSize = true;
            lblValDrv.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblValDrv.ForeColor = Color.FromArgb(17, 35, 108);
            lblValDrv.Location = new Point(60, 45);
            lblValDrv.Name = "lblValDrv";
            lblValDrv.Size = new Size(94, 20);
            lblValDrv.TabIndex = 2;
            lblValDrv.Text = "Esperando...";
            // 
            // pnlCardTmp
            // 
            pnlCardTmp.BackColor = Color.White;
            pnlCardTmp.Controls.Add(lblIconTmp);
            pnlCardTmp.Controls.Add(lblTitTmp);
            pnlCardTmp.Controls.Add(lblValTmp);
            pnlCardTmp.Location = new Point(350, 220);
            pnlCardTmp.Name = "pnlCardTmp";
            pnlCardTmp.Size = new Size(440, 79);
            pnlCardTmp.TabIndex = 2;
            // 
            // lblIconTmp
            // 
            lblIconTmp.AutoSize = true;
            lblIconTmp.Font = new Font("Segoe UI", 24F);
            lblIconTmp.ForeColor = Color.FromArgb(17, 35, 108);
            lblIconTmp.Location = new Point(10, 10);
            lblIconTmp.Name = "lblIconTmp";
            lblIconTmp.Size = new Size(64, 45);
            lblIconTmp.TabIndex = 0;
            lblIconTmp.Text = "🗑️";
            // 
            // lblTitTmp
            // 
            lblTitTmp.AutoSize = true;
            lblTitTmp.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitTmp.ForeColor = Color.Gray;
            lblTitTmp.Location = new Point(80, 20);
            lblTitTmp.Name = "lblTitTmp";
            lblTitTmp.Size = new Size(140, 15);
            lblTitTmp.TabIndex = 1;
            lblTitTmp.Text = "Limpieza de Temporales";
            // 
            // lblValTmp
            // 
            lblValTmp.AutoSize = true;
            lblValTmp.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblValTmp.ForeColor = Color.FromArgb(17, 35, 108);
            lblValTmp.Location = new Point(80, 35);
            lblValTmp.Name = "lblValTmp";
            lblValTmp.Size = new Size(94, 20);
            lblValTmp.TabIndex = 2;
            lblValTmp.Text = "Esperando...";
            // 
            // pnlCardSmart
            // 
            pnlCardSmart.BackColor = Color.White;
            pnlCardSmart.Controls.Add(lblIconSmart);
            pnlCardSmart.Controls.Add(lblTitSmart);
            pnlCardSmart.Controls.Add(lblValSmart);
            pnlCardSmart.Controls.Add(flpDiscos);
            pnlCardSmart.Location = new Point(350, 305);
            pnlCardSmart.Name = "pnlCardSmart";
            pnlCardSmart.Size = new Size(440, 190);
            pnlCardSmart.TabIndex = 3;
            // 
            // lblIconSmart
            // 
            lblIconSmart.AutoSize = true;
            lblIconSmart.Font = new Font("Segoe UI", 24F);
            lblIconSmart.ForeColor = Color.FromArgb(17, 35, 108);
            lblIconSmart.Location = new Point(10, 10);
            lblIconSmart.Name = "lblIconSmart";
            lblIconSmart.Size = new Size(64, 45);
            lblIconSmart.TabIndex = 0;
            lblIconSmart.Text = "💽";
            // 
            // lblTitSmart
            // 
            lblTitSmart.AutoSize = true;
            lblTitSmart.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTitSmart.ForeColor = Color.Gray;
            lblTitSmart.Location = new Point(80, 19);
            lblTitSmart.Name = "lblTitSmart";
            lblTitSmart.Size = new Size(164, 15);
            lblTitSmart.TabIndex = 1;
            lblTitSmart.Text = "Estado de Discos (S.M.A.R.T.)";
            lblTitSmart.Click += lblTitSmart_Click;
            // 
            // lblValSmart
            // 
            lblValSmart.AutoSize = true;
            lblValSmart.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblValSmart.ForeColor = Color.FromArgb(17, 35, 108);
            lblValSmart.Location = new Point(80, 35);
            lblValSmart.Name = "lblValSmart";
            lblValSmart.Size = new Size(94, 20);
            lblValSmart.TabIndex = 2;
            lblValSmart.Text = "Esperando...";
            // 
            // flpDiscos
            // 
            flpDiscos.AutoScroll = true;
            flpDiscos.BackColor = Color.FromArgb(250, 250, 250);
            flpDiscos.Location = new Point(15, 70);
            flpDiscos.Name = "flpDiscos";
            flpDiscos.Size = new Size(410, 115);
            flpDiscos.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(240, 242, 245);
            ClientSize = new Size(820, 720);
            Controls.Add(pnlCardUpd);
            Controls.Add(pnlCardDrv);
            Controls.Add(pnlCardTmp);
            Controls.Add(pnlCardSmart);
            Controls.Add(panelDF);
            Controls.Add(btnDeviceManager);
            Controls.Add(btnDrivers);
            Controls.Add(btnAbrirUpdate);
            Controls.Add(cmbTecnico);
            Controls.Add(lblTecnico);
            Controls.Add(btnAuto);
            Controls.Add(rtbLog);
            Controls.Add(btnReport);
            Controls.Add(btnUpdate);
            Controls.Add(btnSmart);
            Controls.Add(btnCleanTemp);
            Controls.Add(panelHeader);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.Manual;
            Text = "Copicanarias Server Report (CSR)";
            Load += Form1_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            panelDF.ResumeLayout(false);
            panelDF.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFirmas).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCertificados).EndInit();
            pnlCardUpd.ResumeLayout(false);
            pnlCardUpd.PerformLayout();
            pnlCardDrv.ResumeLayout(false);
            pnlCardDrv.PerformLayout();
            pnlCardTmp.ResumeLayout(false);
            pnlCardTmp.PerformLayout();
            pnlCardSmart.ResumeLayout(false);
            pnlCardSmart.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelHeader;
        private Label lblTituloCabecera;
        private PictureBox pictureBoxLogo;
        private BotonModerno btnToggleLog;
        private Label lblTecnico;
        private ComboBox cmbTecnico;
        private BotonModerno btnCleanTemp;
        private BotonModerno btnSmart;
        private BotonModerno btnUpdate;
        private BotonModerno btnAbrirUpdate;
        private BotonModerno btnReport;
        private RichTextBox rtbLog;
        private BotonModerno btnAuto;
        private BotonModerno btnDrivers;
        private BotonModerno btnDeviceManager;

        private Panel panelDF;
        private Label lblDfTitulo;
        private Panel panelDfLinea;
        private CheckBox chkDigitalizacion;
        private CheckBox chkFirmas;
        private Label lblFirmasRestantes;
        private NumericUpDown numFirmas;
        private CheckBox chkCertificados;
        private Label lblNumCerts;
        private NumericUpDown numCertificados;
        private Panel panelCertsDinamico;
        private PictureBox pictureBox1;

        // --- DASHBOARD CONTROLS ---
        private Panel pnlCardUpd;
        private Label lblIconUpd;
        private Label lblTitUpd;
        private Label lblValUpd;

        private Panel pnlCardDrv;
        private Label lblIconDrv;
        private Label lblTitDrv;
        private Label lblValDrv;

        private Panel pnlCardTmp;
        private Label lblIconTmp;
        private Label lblTitTmp;
        private Label lblValTmp;

        private Panel pnlCardSmart;
        private Label lblIconSmart;
        private Label lblTitSmart;
        private Label lblValSmart;
        private FlowLayoutPanel flpDiscos;
    }
}