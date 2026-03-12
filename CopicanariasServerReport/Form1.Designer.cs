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
            lblTituloCabecera = new Label();
            pictureBoxLogo = new PictureBox();
            lblTecnico = new Label();
            cmbTecnico = new ComboBox();
            btnCleanTemp = new Button();
            btnSmart = new Button();
            btnUpdate = new Button();
            btnAbrirUpdate = new Button();
            btnDrivers = new Button();
            btnDeviceManager = new Button();
            btnReport = new Button();
            btnAuto = new Button();
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
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
            panelDF.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numFirmas).BeginInit();
            ((System.ComponentModel.ISupportInitialize)numCertificados).BeginInit();
            SuspendLayout();
            // 
            // panelHeader
            // 
            panelHeader.BackColor = Color.White;
            panelHeader.Controls.Add(lblTituloCabecera);
            panelHeader.Controls.Add(pictureBoxLogo);
            panelHeader.Dock = DockStyle.Top;
            panelHeader.Location = new Point(0, 0);
            panelHeader.Name = "panelHeader";
            panelHeader.Size = new Size(800, 93);
            panelHeader.TabIndex = 0;
            // 
            // lblTituloCabecera
            // 
            lblTituloCabecera.AutoSize = true;
            lblTituloCabecera.BackColor = Color.White;
            lblTituloCabecera.Font = new Font("Segoe UI", 16F, FontStyle.Bold);
            lblTituloCabecera.ForeColor = Color.DarkBlue;
            lblTituloCabecera.Location = new Point(265, 28);
            lblTituloCabecera.Name = "lblTituloCabecera";
            lblTituloCabecera.Size = new Size(266, 30);
            lblTituloCabecera.TabIndex = 1;
            lblTituloCabecera.Text = "Panel de Mantenimiento";
            // 
            // pictureBoxLogo
            // 
            pictureBoxLogo.Image = Properties.Resources.copicanariasicon;
            pictureBoxLogo.Location = new Point(103, 0);
            pictureBoxLogo.Name = "pictureBoxLogo";
            pictureBoxLogo.Size = new Size(145, 93);
            pictureBoxLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBoxLogo.TabIndex = 0;
            pictureBoxLogo.TabStop = false;
            // 
            // lblTecnico
            // 
            lblTecnico.AutoSize = true;
            lblTecnico.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblTecnico.ForeColor = Color.DimGray;
            lblTecnico.Location = new Point(30, 105);
            lblTecnico.Name = "lblTecnico";
            lblTecnico.Size = new Size(124, 15);
            lblTecnico.TabIndex = 7;
            lblTecnico.Text = "Técnico Responsable:";
            // 
            // cmbTecnico
            // 
            cmbTecnico.BackColor = SystemColors.Menu;
            cmbTecnico.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbTecnico.Font = new Font("Segoe UI", 10F);
            cmbTecnico.FormattingEnabled = true;
            cmbTecnico.Location = new Point(30, 125);
            cmbTecnico.Name = "cmbTecnico";
            cmbTecnico.Size = new Size(300, 25);
            cmbTecnico.TabIndex = 8;
            cmbTecnico.SelectedIndexChanged += cmbTecnico_SelectedIndexChanged;
            // 
            // btnCleanTemp
            // 
            btnCleanTemp.BackColor = Color.DarkBlue;
            btnCleanTemp.Cursor = Cursors.Hand;
            btnCleanTemp.FlatAppearance.BorderSize = 0;
            btnCleanTemp.FlatStyle = FlatStyle.Flat;
            btnCleanTemp.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCleanTemp.ForeColor = SystemColors.Control;
            btnCleanTemp.Location = new Point(30, 165);
            btnCleanTemp.Name = "btnCleanTemp";
            btnCleanTemp.Size = new Size(300, 45);
            btnCleanTemp.TabIndex = 1;
            btnCleanTemp.Text = "1. Limpiar Archivos Temporales";
            btnCleanTemp.UseVisualStyleBackColor = false;
            btnCleanTemp.Click += btnCleanTemp_Click;
            // 
            // btnSmart
            // 
            btnSmart.BackColor = Color.DarkBlue;
            btnSmart.Cursor = Cursors.Hand;
            btnSmart.FlatAppearance.BorderSize = 0;
            btnSmart.FlatStyle = FlatStyle.Flat;
            btnSmart.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSmart.ForeColor = SystemColors.Control;
            btnSmart.Location = new Point(30, 220);
            btnSmart.Name = "btnSmart";
            btnSmart.Size = new Size(300, 45);
            btnSmart.TabIndex = 2;
            btnSmart.Text = "2. Test S.M.A.R.T. de Discos";
            btnSmart.UseVisualStyleBackColor = false;
            btnSmart.Click += btnSmart_Click;
            // 
            // btnUpdate
            // 
            btnUpdate.BackColor = Color.RoyalBlue;
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUpdate.ForeColor = SystemColors.Control;
            btnUpdate.Location = new Point(30, 275);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(300, 45);
            btnUpdate.TabIndex = 3;
            btnUpdate.Text = "🔄  Re-analizar Windows Update";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnAbrirUpdate
            // 
            btnAbrirUpdate.BackColor = Color.SlateGray;
            btnAbrirUpdate.Cursor = Cursors.Hand;
            btnAbrirUpdate.FlatAppearance.BorderSize = 0;
            btnAbrirUpdate.FlatStyle = FlatStyle.Flat;
            btnAbrirUpdate.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnAbrirUpdate.ForeColor = Color.White;
            btnAbrirUpdate.Location = new Point(30, 326);
            btnAbrirUpdate.Name = "btnAbrirUpdate";
            btnAbrirUpdate.Size = new Size(300, 30);
            btnAbrirUpdate.TabIndex = 9;
            btnAbrirUpdate.Text = "Abrir Panel de Windows Update";
            btnAbrirUpdate.UseVisualStyleBackColor = false;
            btnAbrirUpdate.Click += btnAbrirUpdate_Click;
            // 
            // btnDrivers
            // 
            btnDrivers.BackColor = Color.RoyalBlue;
            btnDrivers.Cursor = Cursors.Hand;
            btnDrivers.FlatAppearance.BorderSize = 0;
            btnDrivers.FlatStyle = FlatStyle.Flat;
            btnDrivers.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDrivers.ForeColor = SystemColors.Control;
            btnDrivers.Location = new Point(30, 366);
            btnDrivers.Name = "btnDrivers";
            btnDrivers.Size = new Size(300, 45);
            btnDrivers.TabIndex = 10;
            btnDrivers.Text = "🔄 Re-escanear Drivers";
            btnDrivers.UseVisualStyleBackColor = false;
            btnDrivers.Click += btnDrivers_Click;
            // 
            // btnDeviceManager
            // 
            btnDeviceManager.BackColor = Color.SlateGray;
            btnDeviceManager.Cursor = Cursors.Hand;
            btnDeviceManager.FlatAppearance.BorderSize = 0;
            btnDeviceManager.FlatStyle = FlatStyle.Flat;
            btnDeviceManager.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnDeviceManager.ForeColor = Color.White;
            btnDeviceManager.Location = new Point(30, 417);
            btnDeviceManager.Name = "btnDeviceManager";
            btnDeviceManager.Size = new Size(300, 30);
            btnDeviceManager.TabIndex = 11;
            btnDeviceManager.Text = "Abrir Administrador de Dispositivos";
            btnDeviceManager.UseVisualStyleBackColor = false;
            btnDeviceManager.Click += btnDeviceManager_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = Color.ForestGreen;
            btnReport.Cursor = Cursors.Hand;
            btnReport.FlatAppearance.BorderSize = 0;
            btnReport.FlatStyle = FlatStyle.Flat;
            btnReport.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnReport.ForeColor = Color.White;
            btnReport.Location = new Point(30, 512);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(300, 47);
            btnReport.TabIndex = 4;
            btnReport.Text = "📄 Generar Informe PDF";
            btnReport.UseVisualStyleBackColor = false;
            btnReport.Click += btnReport_Click;
            // 
            // btnAuto
            // 
            btnAuto.BackColor = Color.Crimson;
            btnAuto.Cursor = Cursors.Hand;
            btnAuto.FlatAppearance.BorderSize = 0;
            btnAuto.FlatStyle = FlatStyle.Flat;
            btnAuto.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnAuto.ForeColor = Color.WhiteSmoke;
            btnAuto.Location = new Point(418, 512);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(304, 70);
            btnAuto.TabIndex = 6;
            btnAuto.Text = "⚡ Realizar Todos Los Pasos";
            btnAuto.UseVisualStyleBackColor = false;
            btnAuto.Click += btnAuto_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.FromArgb(238, 241, 248);
            rtbLog.Font = new Font("Segoe UI", 9.5F);
            rtbLog.ForeColor = Color.FromArgb(40, 40, 40);
            rtbLog.Location = new Point(350, 110);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(438, 385);
            rtbLog.TabIndex = 5;
            rtbLog.Text = "";
            // 
            // panelDF
            // 
            panelDF.BackColor = Color.FromArgb(232, 238, 255);
            panelDF.BorderStyle = BorderStyle.FixedSingle;
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
            panelDF.Location = new Point(12, 512);
            panelDF.Name = "panelDF";
            panelDF.Size = new Size(770, 262);
            panelDF.TabIndex = 20;
            panelDF.Visible = false;
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.DF_SERVER_logo_300x60;
            pictureBox1.Location = new Point(242, 8);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(257, 46);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 2;
            pictureBox1.TabStop = false;
            pictureBox1.Click += pictureBox1_Click;
            // 
            // lblDfTitulo
            // 
            lblDfTitulo.AutoSize = true;
            lblDfTitulo.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            lblDfTitulo.ForeColor = Color.FromArgb(28, 60, 160);
            lblDfTitulo.Location = new Point(10, 8);
            lblDfTitulo.Name = "lblDfTitulo";
            lblDfTitulo.Size = new Size(160, 19);
            lblDfTitulo.TabIndex = 0;
            lblDfTitulo.Text = "🔷  Sección DF-Server";
            // 
            // panelDfLinea
            // 
            panelDfLinea.BackColor = Color.FromArgb(28, 60, 160);
            panelDfLinea.Location = new Point(10, 30);
            panelDfLinea.Name = "panelDfLinea";
            panelDfLinea.Size = new Size(748, 2);
            panelDfLinea.TabIndex = 1;
            // 
            // chkDigitalizacion
            // 
            chkDigitalizacion.AutoSize = true;
            chkDigitalizacion.Font = new Font("Segoe UI", 9.5F);
            chkDigitalizacion.Location = new Point(10, 42);
            chkDigitalizacion.Name = "chkDigitalizacion";
            chkDigitalizacion.Size = new Size(205, 21);
            chkDigitalizacion.TabIndex = 21;
            chkDigitalizacion.Text = "Digitalización certificada activa";
            // 
            // chkFirmas
            // 
            chkFirmas.AutoSize = true;
            chkFirmas.Font = new Font("Segoe UI", 9.5F);
            chkFirmas.Location = new Point(10, 74);
            chkFirmas.Name = "chkFirmas";
            chkFirmas.Size = new Size(196, 21);
            chkFirmas.TabIndex = 22;
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
            lblFirmasRestantes.TabIndex = 23;
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
            numFirmas.TabIndex = 23;
            // 
            // chkCertificados
            // 
            chkCertificados.AutoSize = true;
            chkCertificados.Font = new Font("Segoe UI", 9.5F);
            chkCertificados.Location = new Point(10, 108);
            chkCertificados.Name = "chkCertificados";
            chkCertificados.Size = new Size(182, 21);
            chkCertificados.TabIndex = 24;
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
            lblNumCerts.TabIndex = 25;
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
            numCertificados.TabIndex = 25;
            numCertificados.Value = new decimal(new int[] { 1, 0, 0, 0 });
            numCertificados.ValueChanged += numCertificados_ValueChanged;
            // 
            // panelCertsDinamico
            // 
            panelCertsDinamico.AutoScroll = true;
            panelCertsDinamico.BackColor = Color.FromArgb(220, 228, 250);
            panelCertsDinamico.BorderStyle = BorderStyle.FixedSingle;
            panelCertsDinamico.Location = new Point(10, 140);
            panelCertsDinamico.Name = "panelCertsDinamico";
            panelCertsDinamico.Size = new Size(748, 112);
            panelCertsDinamico.TabIndex = 26;
            panelCertsDinamico.Visible = false;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(800, 626);
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
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Copicanarias Server Report";
            Load += Form1_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            panelDF.ResumeLayout(false);
            panelDF.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)numFirmas).EndInit();
            ((System.ComponentModel.ISupportInitialize)numCertificados).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Panel panelHeader;
        private Label lblTituloCabecera;
        private PictureBox pictureBoxLogo;
        private Label lblTecnico;
        private ComboBox cmbTecnico;
        private Button btnCleanTemp;
        private Button btnSmart;
        private Button btnUpdate;
        private Button btnAbrirUpdate;
        private Button btnReport;
        private RichTextBox rtbLog;
        private Button btnAuto;
        private Button btnDrivers;
        private Button btnDeviceManager;

        // ── DF-Server ────────────────────────────────────────────────
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
    }
}