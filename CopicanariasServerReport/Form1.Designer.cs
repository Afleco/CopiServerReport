namespace CopicanariasServerReport
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            panelHeader = new Panel();
            lblTituloCabecera = new Label();
            pictureBoxLogo = new PictureBox();
            btnCleanTemp = new Button();
            btnSmart = new Button();
            btnUpdate = new Button();
            btnReport = new Button();
            rtbLog = new RichTextBox();
            btnAuto = new Button();
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
            panelHeader.Size = new Size(784, 93);
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
            // btnCleanTemp
            // 
            btnCleanTemp.BackColor = Color.DarkBlue;
            btnCleanTemp.Cursor = Cursors.Hand;
            btnCleanTemp.FlatAppearance.BorderSize = 0;
            btnCleanTemp.FlatStyle = FlatStyle.Flat;
            btnCleanTemp.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCleanTemp.ForeColor = SystemColors.Control;
            btnCleanTemp.Location = new Point(30, 110);
            btnCleanTemp.Name = "btnCleanTemp";
            btnCleanTemp.Size = new Size(300, 50);
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
            btnSmart.Location = new Point(30, 179);
            btnSmart.Name = "btnSmart";
            btnSmart.Size = new Size(300, 50);
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
            btnUpdate.Location = new Point(30, 248);
            btnUpdate.Name = "btnUpdate";
            btnUpdate.Size = new Size(300, 50);
            btnUpdate.TabIndex = 3;
            btnUpdate.Text = "3. Ejecutar Windows Update";
            btnUpdate.UseVisualStyleBackColor = false;
            btnUpdate.Click += btnUpdate_Click;
            // 
            // btnReport
            // 
            btnReport.BackColor = Color.DarkBlue;
            btnReport.Cursor = Cursors.Hand;
            btnReport.FlatAppearance.BorderSize = 0;
            btnReport.FlatStyle = FlatStyle.Flat;
            btnReport.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnReport.ForeColor = Color.White;
            btnReport.Location = new Point(30, 316);
            btnReport.Name = "btnReport";
            btnReport.Size = new Size(300, 60);
            btnReport.TabIndex = 4;
            btnReport.Text = "📄 Generar Informe PDF";
            btnReport.UseVisualStyleBackColor = false;
            btnReport.Click += btnReport_Click;
            // 
            // rtbLog
            // 
            rtbLog.BackColor = Color.FromArgb(30, 30, 30);
            rtbLog.Font = new Font("Consolas", 10F);
            rtbLog.ForeColor = Color.LimeGreen;
            rtbLog.Location = new Point(360, 110);
            rtbLog.Name = "rtbLog";
            rtbLog.ReadOnly = true;
            rtbLog.Size = new Size(412, 366);
            rtbLog.TabIndex = 5;
            rtbLog.Text = ">>> Sistema de Mantenimiento Inicializado.\n>>> Esperando órdenes del administrador...\n";
            // 
            // btnAuto
            // 
            btnAuto.BackColor = Color.Crimson;
            btnAuto.BackgroundImageLayout = ImageLayout.Zoom;
            btnAuto.Cursor = Cursors.Hand;
            btnAuto.FlatAppearance.BorderSize = 0;
            btnAuto.FlatStyle = FlatStyle.Flat;
            btnAuto.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            btnAuto.ForeColor = Color.WhiteSmoke;
            btnAuto.Location = new Point(30, 399);
            btnAuto.Name = "btnAuto";
            btnAuto.Size = new Size(300, 60);
            btnAuto.TabIndex = 6;
            btnAuto.Text = "Realizar todos los pasos";
            btnAuto.UseVisualStyleBackColor = false;
            btnAuto.Click += btnAuto_Click;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.White;
            ClientSize = new Size(784, 498);
            Controls.Add(btnAuto);
            Controls.Add(rtbLog);
            Controls.Add(btnReport);
            Controls.Add(btnUpdate);
            Controls.Add(btnSmart);
            Controls.Add(btnCleanTemp);
            Controls.Add(panelHeader);
            ForeColor = SystemColors.ButtonFace;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Copicanarias Server Report";
            Load += Form1_Load_1;
            Click += btnAuto_Click;
            panelHeader.ResumeLayout(false);
            panelHeader.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxLogo).EndInit();
            ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelHeader;
        private System.Windows.Forms.Label lblTituloCabecera;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Button btnCleanTemp;
        private System.Windows.Forms.Button btnSmart;
        private System.Windows.Forms.Button btnUpdate;
        private System.Windows.Forms.Button btnReport;
        private System.Windows.Forms.RichTextBox rtbLog;
        private Button btnAuto;
    }
}