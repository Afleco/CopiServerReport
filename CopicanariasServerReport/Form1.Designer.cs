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
            panelHeader = new Panel();
            lblTituloCabecera = new Label();
            pictureBoxLogo = new PictureBox();
            lblTecnico = new Label();
            cmbTecnico = new ComboBox();
            btnCleanTemp = new Button();
            btnSmart = new Button();
            btnUpdate = new Button();
            btnAbrirUpdate = new Button();
            btnReport = new Button();
            btnAuto = new Button();
            rtbLog = new RichTextBox();
            panelHeader.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).BeginInit();
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
            btnUpdate.BackColor = Color.DarkBlue;
            btnUpdate.Cursor = Cursors.Hand;
            btnUpdate.FlatAppearance.BorderSize = 0;
            btnUpdate.FlatStyle = FlatStyle.Flat;
            btnUpdate.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnUpdate.ForeColor = SystemColors.Control;
            btnUpdate.Location = new Point(30, 275);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(300, 45);
            btnUpdate.TabIndex = 3;
            btnUpdate.Text = "3. Analizar Windows Update";
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
            btnAbrirUpdate.Location = new Point(30, 325);
            btnAbrirUpdate.Name = "btnAbrirUpdate";
            btnAbrirUpdate.Size = new Size(300, 30);
            btnAbrirUpdate.TabIndex = 9;
            btnAbrirUpdate.Text = "Abrir Panel de Windows Update";
            btnAbrirUpdate.UseVisualStyleBackColor = false;
            btnAbrirUpdate.Click += btnAbrirUpdate_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = Color.ForestGreen;
            btnReport.Cursor = Cursors.Hand;
            btnReport.FlatAppearance.BorderSize = 0;
            btnReport.FlatStyle = FlatStyle.Flat;
            btnReport.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnReport.ForeColor = Color.White;
            btnReport.Location = new Point(30, 370);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(300, 55);
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
            btnAuto.Location = new Point(30, 440);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(300, 55);
            btnAuto.TabIndex = 6;
            btnAuto.Text = "⚡ Realizar Mantenimiento Total";
            btnAuto.UseVisualStyleBackColor = false;
            btnAuto.Click += btnAuto_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.FromArgb(30, 30, 30);
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.ForeColor = Color.LimeGreen;
            rtbLog.Location = new Point(350, 110);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(420, 385);
            rtbLog.TabIndex = 5;
            rtbLog.Text = ">>> Sistema de Mantenimiento Inicializado.\n>>> Esperando órdenes del administrador...\n";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(800, 515);
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
            MaximizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Copicanarias Server Report";
            Load += Form1_Load;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTituloCabecera;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label lblTecnico;
        private System.Windows.Forms.ComboBox cmbTecnico;
        private System.Windows.Forms.Button btnCleanTemp;
        private System.Windows.Forms.Button btnSmart;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnAbrirUpdate;
        private System.Windows.Forms.Button btnReport;
        private System.Windows.Forms.RichTextBox rtbLog;
        private System.Windows.Forms.Button btnAuto;
    }
}