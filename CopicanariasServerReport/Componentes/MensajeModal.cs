using System;
using System.Drawing;
using System.Windows.Forms;

namespace CopicanariasServerReport.Componentes
{
    public static class MensajeModal
    {
        public static DialogResult Show(string mensaje, string titulo, MessageBoxButtons botones, MessageBoxIcon icono)
        {
            using (Form form = new Form())
            {
                // Configuración de la ventana
                form.Text = titulo;
                form.FormBorderStyle = FormBorderStyle.FixedDialog;
                form.StartPosition = FormStartPosition.CenterParent;
                form.MaximizeBox = false;
                form.MinimizeBox = false;
                form.ShowIcon = false;
                form.BackColor = Color.White;

                // Fijamos el ancho del lienzo interno
                form.ClientSize = new Size(500, 200);

                // 1. Lógica del Icono
                string iconoTexto = "";
                Color colorIcono = Color.FromArgb(64, 64, 64);

                switch (icono)
                {
                    case MessageBoxIcon.Warning: iconoTexto = "⚠️"; colorIcono = Color.Orange; break;
                    case MessageBoxIcon.Error: iconoTexto = "❌"; colorIcono = Color.FromArgb(226, 30, 45); break;
                    case MessageBoxIcon.Question: iconoTexto = "❓"; colorIcono = Color.FromArgb(17, 35, 108); break;
                    case MessageBoxIcon.Information: iconoTexto = "ℹ️"; colorIcono = Color.FromArgb(17, 35, 108); break;
                }

                Label lblIcono = new Label();
                lblIcono.Text = iconoTexto;
                lblIcono.Font = new Font("Segoe UI Emoji", 24f);
                lblIcono.ForeColor = colorIcono;
                lblIcono.Location = new Point(15, 20);
                lblIcono.AutoSize = true;
                form.Controls.Add(lblIcono);

                // 2. Etiqueta del mensaje (Dinámica)
                Label lblMensaje = new Label();
                lblMensaje.Text = mensaje;
                lblMensaje.Font = new Font("Segoe UI", 11.5f, FontStyle.Regular);
                lblMensaje.ForeColor = Color.FromArgb(64, 64, 64);

                int posicionTextoX = lblIcono.Right + 15;
                lblMensaje.Location = new Point(posicionTextoX, 28);

                int anchoDisponible = form.ClientSize.Width - posicionTextoX - 20;
                lblMensaje.MaximumSize = new Size(anchoDisponible, 0);
                lblMensaje.AutoSize = true;
                form.Controls.Add(lblMensaje);

                // 3. Panel de botones
                Panel panelBotones = new Panel();
                panelBotones.BackColor = Color.FromArgb(240, 240, 240);
                panelBotones.Height = 65; // Un poco más alto para que respiren los botones
                panelBotones.Dock = DockStyle.Bottom;
                form.Controls.Add(panelBotones);

                // Ajustamos la altura exacta de la ventana basándonos en el texto + panel
                int contenidoBottom = Math.Max(lblMensaje.Bottom, lblIcono.Bottom) + 25;
                form.ClientSize = new Size(500, contenidoBottom + panelBotones.Height);

                // 4. Lógica de botones relativa (ANTI-SOLAPAMIENTO)
                if (botones == MessageBoxButtons.YesNo)
                {
                    BotonModerno btnNo = CrearBoton("No", DialogResult.No, Color.FromArgb(226, 30, 45));
                    // Lo anclamos a la derecha
                    btnNo.Top = 15;
                    btnNo.Left = panelBotones.Width - btnNo.Width - 20;

                    BotonModerno btnYes = CrearBoton("Sí", DialogResult.Yes, Color.FromArgb(17, 35, 108));
                    // Lo anclamos exactamente a la izquierda del botón "No"
                    btnYes.Top = 15;
                    btnYes.Left = btnNo.Left - btnYes.Width - 15;

                    panelBotones.Controls.Add(btnNo);
                    panelBotones.Controls.Add(btnYes);
                    form.CancelButton = btnNo;
                    form.AcceptButton = btnYes;
                }
                else
                {
                    BotonModerno btnOk = CrearBoton("Aceptar", DialogResult.OK, Color.FromArgb(17, 35, 108));
                    btnOk.Top = 15;
                    btnOk.Left = panelBotones.Width - btnOk.Width - 20;

                    panelBotones.Controls.Add(btnOk);
                    form.AcceptButton = btnOk;
                }

                return form.ShowDialog();
            }
        }

        // Helper sin posición X, porque ahora la calculamos arriba
        private static BotonModerno CrearBoton(string texto, DialogResult resultado, Color colorFondo)
        {
            BotonModerno btn = new BotonModerno();
            btn.Text = texto;
            btn.DialogResult = resultado;
            btn.Size = new Size(100, 35);
            btn.BackColor = colorFondo;
            btn.ForeColor = Color.White;
            return btn;
        }
    }
}