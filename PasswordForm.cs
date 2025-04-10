using System;
using System.Windows.Forms;

namespace PrintControl
{
    public class PasswordForm : Form
    {
        public static string CurrentPassword = "admin123"; // Contraseña inicial

        public PasswordForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Ingrese Contraseña";
            
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Label
            Label lblPassword = new Label
            {
                Text = "Contraseña:",
                Location = new System.Drawing.Point(12, 15),
                Size = new System.Drawing.Size(70, 20)
            };

            // TextBox
            TextBox txtPassword = new TextBox
            {
                Location = new System.Drawing.Point(88, 12),
                Size = new System.Drawing.Size(184, 20),
                PasswordChar = '•',
                Name = "txtPassword"
            };

            // Buttons
            Button btnOK = new Button
            {
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(116, 45),
                Size = new System.Drawing.Size(75, 23)
            };

            Button btnCancel = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(197, 45),
                Size = new System.Drawing.Size(75, 23)
            };

            // Event handlers
            btnOK.Click += (s, e) =>
            {
                if (txtPassword.Text == CurrentPassword)
                {
                    this.DialogResult = DialogResult.OK;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Contraseña incorrecta", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtPassword.Clear();
                    txtPassword.Focus();
                    this.DialogResult = DialogResult.None;
                }
            };

            // Add controls
            this.Controls.AddRange(new Control[] { 
                lblPassword, 
                txtPassword, 
                btnOK, 
                btnCancel 
            });

            // Form properties
            this.ClientSize = new System.Drawing.Size(284, 80);
            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PasswordForm));
            this.SuspendLayout();
            // 
            // PasswordForm
            // 
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "PasswordForm";
            this.ResumeLayout(false);

        }
    }
}
