using System;
using System.Drawing;
using System.Windows.Forms;

namespace CopicanariasServerReport.Componentes
{
    public static class ModalMessage
    {
        public static DialogResult Show(string message, string title, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            using (Form form = new Form())
            {
                // Configuración de la ventana
                form.Text = title;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowIcon = false;
                form.BackColor = Color.White;

                // Fijamos el ancho del lienzo interno
                form.ClientSize = new Size(500, 200);

                // 1. Lógica del Icono
                string iconText = "";
                Color colorIcon = Color.FromArgb(64, 64, 64);

                switch (icon)
                {
                    case MessageBoxIcon.Warning: iconText = "⚠️"; colorIcon = Color.Orange; break;
                    case MessageBoxIcon.Error: iconText = "❌"; colorIcon = Color.FromArgb(226, 30, 45); break;
                    case MessageBoxIcon.Question: iconText = "❓"; colorIcon = Color.FromArgb(17, 35, 108); break;
                    case MessageBoxIcon.Information: iconText = "ℹ️"; colorIcon = Color.FromArgb(17, 35, 108); break;
                }

                Label lblIcon = new Label();
                lblIcon.Text = iconText;
                lblIcon.Font = new Font("Segoe UI Emoji", 24f);
                lblIcon.ForeColor = colorIcon;
                lblIcon.Location = new Point(15, 20);
                lblIcon.AutoSize = true;
                form.Controls.Add(lblIcon);

                // 2. Etiqueta del mensaje (Dinámica)
                Label lblMessage = new Label();
                lblMessage.Text = message;
                lblMessage.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);
                lblMessage.ForeColor = Color.FromArgb(64, 64, 64);

                int textPositionX = lblIcon.Right + 15;
                lblMessage.Location = new Point(textPositionX, 28);

                int availableWidth = form.ClientSize.Width - textPositionX - 20;
                lblMessage.MaximumSize = new Size(availableWidth, 0);
                lblMessage.AutoSize = true;
                form.Controls.Add(lblMessage);

                // 3. Panel de botones
                Panel buttonsPanel = new Panel();
                buttonsPanel.BackColor = Color.FromArgb(240, 240, 240);
                buttonsPanel.Height = 65;
                buttonsPanel.Dock = DockStyle.Bottom;
                form.Controls.Add(buttonsPanel);

                // Ajustamos la altura exacta de la ventana basándonos en el texto + panel
                int bottomContent = Math.Max(lblMessage.Bottom, lblIcon.Bottom) + 25;
                form.ClientSize = new Size(500, bottomContent + buttonsPanel.Height);

                // 4. Lógica de botones relativa (ANTI-SOLAPAMIENTO)
                if (buttons == MessageBoxButtons.YesNo)
                {
                    ModernButton btnNo = CreateButton("No", DialogResult.No, Color.FromArgb(226, 30, 45));
                    btnNo.Top = 15;
                    btnNo.Left = buttonsPanel.Width - btnNo.Width - 20;

                    ModernButton btnYes = CreateButton("Sí", DialogResult.Yes, Color.FromArgb(17, 35, 108));
                    btnYes.Top = 15;
                    btnYes.Left = btnNo.Left - btnYes.Width - 15;

                    buttonsPanel.Controls.Add(btnNo);
                    buttonsPanel.Controls.Add(btnYes);

                    // Nota: Ya no asignamos form.CancelButton = btnNo; 
                    // Esto permite que si el usuario pulsa la "X" de la ventana, devuelva DialogResult.Cancel nativo.
                    form.AcceptButton = btnYes;
                }
                else
                {
                    ModernButton btnOk = CreateButton("Aceptar", DialogResult.OK, Color.FromArgb(17, 35, 108));
                    btnOk.Top = 15;
                    btnOk.Left = buttonsPanel.Width - btnOk.Width - 20;

                    buttonsPanel.Controls.Add(btnOk);
                    form.AcceptButton = btnOk;
                }

                return form.ShowDialog();
            }
        }

        private static ModernButton CreateButton(string text, DialogResult result, Color backgroundColor)
        {
            ModernButton btn = new ModernButton();
            btn.Text = text;
            btn.DialogResult = result;
            btn.Size = new Size(100, 35);
            btn.BackColor = backgroundColor;
            btn.ForeColor = Color.White;
            return btn;
        }
    }
}