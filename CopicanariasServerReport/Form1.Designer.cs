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
            btnToggleLog = new ModernButton();
            lblTituloCabecera = new Label();
            pictureBoxLogo = new PictureBox();
            lblTecnico = new Label();
            cmbTechnician = new ComboBox();
            btnCleanTemp = new ModernButton();
            btnSmart = new ModernButton();
            btnUpdate = new ModernButton();
            btnInstallUpdates = new ModernButton();
            btnOpenUpdate = new ModernButton();
            btnDrivers = new ModernButton();
            btnDeviceManager = new ModernButton();
            btnReport = new ModernButton();
            btnAuto = new ModernButton();
            rtbLog = new RichTextBox();
            DfPanel = new Panel();
            pictureBox1 = new PictureBox();
            lblDfTitulo = new Label();
            panelDfLinea = new Panel();
            checkDigitization = new CheckBox();
            chkSignatures = new CheckBox();
            lblRemainingSignatures = new Label();
            SignaturesNum = new NumericUpDown();
            chkCertificates = new CheckBox();
            lblCertsNum = new Label();
            CertificatesNum = new NumericUpDown();
            DynamicCertsPanel = new Panel();
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
            flpDisks = new FlowLayoutPanel();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            DfPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)SignaturesNum).BeginInit();
            ((System.ComponentModel.ISupportInitialize)CertificatesNum).BeginInit();
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
            btnToggleLog.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
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
            lblTituloCabecera.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTituloCabecera.ForeColor = Color.FromArgb(17, 35, 108);
            lblTituloCabecera.Location = new Point(256, 20);
            lblTituloCabecera.Name = "lblTituloCabecera";
            lblTituloCabecera.Size = new Size(314, 45);
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
            lblTecnico.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTecnico.ForeColor = Color.FromArgb(64, 64, 64);
            lblTecnico.Location = new Point(30, 115);
            lblTecnico.Name = "lblTecnico";
            lblTecnico.Size = new Size(139, 17);
            lblTecnico.TabIndex = 9;
            lblTecnico.Text = "Técnico Responsable:";
            // 
            // cmbTecnico
            // 
            cmbTechnician.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTechnician.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            cmbTechnician.Location = new Point(30, 135);
            cmbTechnician.Name = "cmbTecnico";
            cmbTechnician.Size = new Size(300, 28);
            cmbTechnician.TabIndex = 8;
            cmbTechnician.SelectedIndexChanged += cmbTechnician_SelectedIndexChanged;
            // 
            // btnCleanTemp
            // 
            btnCleanTemp.BackColor = Color.FromArgb(17, 35, 108);
            btnCleanTemp.FlatStyle = FlatStyle.Flat;
            btnCleanTemp.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
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
            btnSmart.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
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
            btnUpdate.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnUpdate.ForeColor = Color.White;
            btnUpdate.Location = new Point(30, 305);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(300, 40);
            btnUpdate.TabIndex = 13;
            btnUpdate.Text = "🔄 Re-analizar Windows Update";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnInstalarUpdates
            // 
            btnInstallUpdates.BackColor = Color.FromArgb(34, 197, 94);
            btnInstallUpdates.FlatStyle = FlatStyle.Flat;
            btnInstallUpdates.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnInstallUpdates.ForeColor = Color.White;
            btnInstallUpdates.Location = new Point(30, 351);
            btnInstallUpdates.Name = "btnInstalarUpdates";
            btnInstallUpdates.Size = new Size(300, 40);
            btnInstallUpdates.TabIndex = 17;
            btnInstallUpdates.Text = "📥 Instalar Actualizaciones 📥";
            btnInstallUpdates.UseVisualStyleBackColor = false;
            btnInstallUpdates.Click += btnInstalarUpdates_Click;
            // 
            // btnAbrirUpdate
            // 
            btnOpenUpdate.BackColor = Color.White;
            btnOpenUpdate.FlatAppearance.BorderColor = Color.FromArgb(17, 35, 108);
            btnOpenUpdate.FlatStyle = FlatStyle.Flat;
            btnOpenUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnOpenUpdate.ForeColor = Color.FromArgb(17, 35, 108);
            btnOpenUpdate.Location = new Point(30, 397);
            btnOpenUpdate.Name = "btnAbrirUpdate";
            btnOpenUpdate.Size = new Size(300, 35);
            btnOpenUpdate.TabIndex = 7;
            btnOpenUpdate.Text = "Abrir Panel de Windows Update";
            btnOpenUpdate.UseVisualStyleBackColor = false;
            btnOpenUpdate.Click += btnAbrirUpdate_Click;
            // 
            // btnDrivers
            // 
            btnDrivers.BackColor = Color.FromArgb(17, 35, 108);
            btnDrivers.FlatStyle = FlatStyle.Flat;
            btnDrivers.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnDrivers.ForeColor = Color.White;
            btnDrivers.Location = new Point(30, 451);
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
            btnDeviceManager.Location = new Point(30, 497);
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
            btnReport.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnReport.ForeColor = Color.White;
            btnReport.Location = new Point(30, 556);
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
            btnAuto.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnAuto.ForeColor = Color.White;
            btnAuto.Location = new Point(350, 556);
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
            rtbLog.Size = new Size(440, 417);
            rtbLog.TabIndex = 11;
            rtbLog.Text = "";
            rtbLog.Visible = false;
            // 
            // panelDF
            // 
            DfPanel.BackColor = Color.White;
            DfPanel.Controls.Add(pictureBox1);
            DfPanel.Controls.Add(lblDfTitulo);
            DfPanel.Controls.Add(panelDfLinea);
            DfPanel.Controls.Add(checkDigitization);
            DfPanel.Controls.Add(chkSignatures);
            DfPanel.Controls.Add(lblRemainingSignatures);
            DfPanel.Controls.Add(SignaturesNum);
            DfPanel.Controls.Add(chkCertificates);
            DfPanel.Controls.Add(lblCertsNum);
            DfPanel.Controls.Add(CertificatesNum);
            DfPanel.Controls.Add(DynamicCertsPanel);
            DfPanel.Location = new Point(30, 626);
            DfPanel.Name = "panelDF";
            DfPanel.Size = new Size(760, 122);
            DfPanel.TabIndex = 4;
            DfPanel.Visible = false;
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
            lblDfTitulo.Font = new Font("Segoe UI", 11.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblDfTitulo.ForeColor = Color.FromArgb(17, 35, 108);
            lblDfTitulo.Location = new Point(10, 8);
            lblDfTitulo.Name = "lblDfTitulo";
            lblDfTitulo.Size = new Size(161, 20);
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
            checkDigitization.AutoSize = true;
            checkDigitization.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            checkDigitization.Location = new Point(10, 42);
            checkDigitization.Name = "chkDigitalizacion";
            checkDigitization.Size = new Size(246, 24);
            checkDigitization.TabIndex = 3;
            checkDigitization.Text = "¿Tiene digitalización certificada?";
            checkDigitization.CheckedChanged += checkDigitization_CheckedChanged;
            // 
            // chkFirmas
            // 
            chkSignatures.AutoSize = true;
            chkSignatures.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkSignatures.Location = new Point(10, 74);
            chkSignatures.Name = "chkFirmas";
            chkSignatures.Size = new Size(221, 24);
            chkSignatures.TabIndex = 4;
            chkSignatures.Text = "Tiene firmas de DF-Signature";
            chkSignatures.CheckedChanged += chkSignatures_CheckedChanged;
            // 
            // lblFirmasRestantes
            // 
            lblRemainingSignatures.AutoSize = true;
            lblRemainingSignatures.Enabled = false;
            lblRemainingSignatures.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblRemainingSignatures.ForeColor = Color.DimGray;
            lblRemainingSignatures.Location = new Point(242, 77);
            lblRemainingSignatures.Name = "lblFirmasRestantes";
            lblRemainingSignatures.Size = new Size(106, 17);
            lblRemainingSignatures.TabIndex = 5;
            lblRemainingSignatures.Text = "Firmas restantes:";
            // 
            // numFirmas
            // 
            SignaturesNum.Enabled = false;
            SignaturesNum.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            SignaturesNum.Location = new Point(352, 74);
            SignaturesNum.Maximum = new decimal(new int[] { 99999, 0, 0, 0 });
            SignaturesNum.Name = "numFirmas";
            SignaturesNum.Size = new Size(90, 27);
            SignaturesNum.TabIndex = 6;
            // 
            // chkCertificados
            // 
            chkCertificates.AutoSize = true;
            chkCertificates.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            chkCertificates.Location = new Point(10, 108);
            chkCertificates.Name = "chkCertificados";
            chkCertificates.Size = new Size(206, 24);
            chkCertificates.TabIndex = 7;
            chkCertificates.Text = "Tiene certificados digitales";
            chkCertificates.CheckedChanged += chkCertificates_CheckedChanged;
            // 
            // lblNumCerts
            // 
            lblCertsNum.AutoSize = true;
            lblCertsNum.Enabled = false;
            lblCertsNum.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCertsNum.ForeColor = Color.DimGray;
            lblCertsNum.Location = new Point(242, 111);
            lblCertsNum.Name = "lblNumCerts";
            lblCertsNum.Size = new Size(117, 17);
            lblCertsNum.TabIndex = 8;
            lblCertsNum.Text = "Nº de certificados:";
            // 
            // numCertificados
            // 
            CertificatesNum.Enabled = false;
            CertificatesNum.Font = new Font("Segoe UI", 11.25F, FontStyle.Regular, GraphicsUnit.Point, 0);
            CertificatesNum.Location = new Point(365, 108);
            CertificatesNum.Maximum = new decimal(new int[] { 20, 0, 0, 0 });
            CertificatesNum.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
            CertificatesNum.Name = "numCertificados";
            CertificatesNum.Size = new Size(60, 27);
            CertificatesNum.TabIndex = 9;
            CertificatesNum.Value = new decimal(new int[] { 1, 0, 0, 0 });
            CertificatesNum.ValueChanged += CertificatesNum_ValueChanged;
            // 
            // panelCertsDinamico
            // 
            DynamicCertsPanel.AutoScroll = true;
            DynamicCertsPanel.BackColor = Color.FromArgb(240, 242, 245);
            DynamicCertsPanel.BorderStyle = BorderStyle.FixedSingle;
            DynamicCertsPanel.Location = new Point(10, 140);
            DynamicCertsPanel.Name = "panelCertsDinamico";
            DynamicCertsPanel.Size = new Size(740, 112);
            DynamicCertsPanel.TabIndex = 10;
            DynamicCertsPanel.Visible = false;
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
            lblTitUpd.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitUpd.ForeColor = Color.Gray;
            lblTitUpd.Location = new Point(80, 20);
            lblTitUpd.Name = "lblTitUpd";
            lblTitUpd.Size = new Size(113, 17);
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
            lblIconDrv.Font = new Font("Segoe UI", 27.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblIconDrv.ForeColor = Color.FromArgb(17, 35, 108);
            lblIconDrv.Location = new Point(10, 10);
            lblIconDrv.Name = "lblIconDrv";
            lblIconDrv.Size = new Size(59, 50);
            lblIconDrv.TabIndex = 0;
            lblIconDrv.Text = "🖧";
            // 
            // lblTitDrv
            // 
            lblTitDrv.AutoSize = true;
            lblTitDrv.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitDrv.ForeColor = Color.Gray;
            lblTitDrv.Location = new Point(60, 20);
            lblTitDrv.Name = "lblTitDrv";
            lblTitDrv.Size = new Size(95, 17);
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
            lblTitTmp.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitTmp.ForeColor = Color.Gray;
            lblTitTmp.Location = new Point(80, 18);
            lblTitTmp.Name = "lblTitTmp";
            lblTitTmp.Size = new Size(157, 17);
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
            pnlCardSmart.Controls.Add(flpDisks);
            pnlCardSmart.Location = new Point(350, 305);
            pnlCardSmart.Name = "pnlCardSmart";
            pnlCardSmart.Size = new Size(440, 227);
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
            lblTitSmart.Font = new Font("Segoe UI", 9.75F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblTitSmart.ForeColor = Color.Gray;
            lblTitSmart.Location = new Point(80, 13);
            lblTitSmart.Name = "lblTitSmart";
            lblTitSmart.Size = new Size(189, 17);
            lblTitSmart.TabIndex = 1;
            lblTitSmart.Text = "Estado de Discos (S.M.A.R.T.)";
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
            flpDisks.AutoScroll = true;
            flpDisks.BackColor = Color.FromArgb(250, 250, 250);
            flpDisks.Location = new Point(15, 70);
            flpDisks.Name = "flpDiscos";
            flpDisks.Size = new Size(410, 142);
            flpDisks.TabIndex = 3;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(240, 242, 245);
            ClientSize = new Size(820, 770);
            Controls.Add(pnlCardUpd);
            Controls.Add(pnlCardDrv);
            Controls.Add(pnlCardTmp);
            Controls.Add(pnlCardSmart);
            Controls.Add(DfPanel);
            Controls.Add(btnDeviceManager);
            Controls.Add(btnDrivers);
            Controls.Add(btnOpenUpdate);
            Controls.Add(btnInstallUpdates);
            Controls.Add(cmbTechnician);
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
            DfPanel.ResumeLayout(false);
            DfPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)SignaturesNum).EndInit();
            ((System.ComponentModel.ISupportInitialize)CertificatesNum).EndInit();
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
        private ModernButton btnToggleLog;
        private Label lblTecnico;
        private ComboBox cmbTechnician;
        private ModernButton btnCleanTemp;
        private ModernButton btnSmart;
        private ModernButton btnUpdate;
        private ModernButton btnInstallUpdates;
        private ModernButton btnOpenUpdate;
        private ModernButton btnReport;
        private RichTextBox rtbLog;
        private ModernButton btnAuto;
        private ModernButton btnDrivers;
        private ModernButton btnDeviceManager;

        private Panel DfPanel;
        private Label lblDfTitulo;
        private Panel panelDfLinea;
        private CheckBox checkDigitization;
        private CheckBox chkSignatures;
        private Label lblRemainingSignatures;
        private NumericUpDown SignaturesNum;
        private CheckBox chkCertificates;
        private Label lblCertsNum;
        private NumericUpDown CertificatesNum;
        private Panel DynamicCertsPanel;
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
        private FlowLayoutPanel flpDisks;
    }
}