using System;
using System.Windows.Forms;

namespace PrintControl
{
    public class ChangePasswordForm : Form
    {
        private TextBox txtUsername;
        private TextBox txtOldPassword;
        private TextBox txtNewPassword;
        private TextBox txtConfirmPassword;

        public string Username { get; private set; }
        public string NewPassword { get; private set; }

        public ChangePasswordForm()
        {
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "Cambiar Contraseña";
            this.Size = new System.Drawing.Size(350, 250);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Labels
            var lblUsername = new Label
            {
                Text = "Usuario:",
                Location = new System.Drawing.Point(20, 20),
                Size = new System.Drawing.Size(100, 20)
            };

            var lblOldPassword = new Label
            {
                Text = "Contraseña Actual:",
                Location = new System.Drawing.Point(20, 50),
                Size = new System.Drawing.Size(100, 20)
            };

            var lblNewPassword = new Label
            {
                Text = "Nueva Contraseña:",
                Location = new System.Drawing.Point(20, 80),
                Size = new System.Drawing.Size(100, 20)
            };

            var lblConfirmPassword = new Label
            {
                Text = "Confirmar:",
                Location = new System.Drawing.Point(20, 110),
                Size = new System.Drawing.Size(100, 20)
            };

            // TextBoxes
            txtUsername = new TextBox
            {
                Location = new System.Drawing.Point(130, 20),
                Size = new System.Drawing.Size(180, 20)
            };

            txtOldPassword = new TextBox
            {
                Location = new System.Drawing.Point(130, 50),
                Size = new System.Drawing.Size(180, 20),
                PasswordChar = '•'
            };

            txtNewPassword = new TextBox
            {
                Location = new System.Drawing.Point(130, 80),
                Size = new System.Drawing.Size(180, 20),
                PasswordChar = '•'
            };

            txtConfirmPassword = new TextBox
            {
                Location = new System.Drawing.Point(130, 110),
                Size = new System.Drawing.Size(180, 20),
                PasswordChar = '•'
            };

            // Buttons
            var btnOK = new Button
            {
                Text = "Aceptar",
                DialogResult = DialogResult.OK,
                Location = new System.Drawing.Point(130, 150),
                Size = new System.Drawing.Size(85, 30)
            };

            var btnCancel = new Button
            {
                Text = "Cancelar",
                DialogResult = DialogResult.Cancel,
                Location = new System.Drawing.Point(225, 150),
                Size = new System.Drawing.Size(85, 30)
            };

            // Event handler
            btnOK.Click += (s, e) =>
            {
                if (string.IsNullOrEmpty(txtUsername.Text))
                {
                    MessageBox.Show("Por favor ingrese el usuario", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (txtOldPassword.Text != PasswordForm.CurrentPassword)
                {
                    MessageBox.Show("La contraseña actual es incorrecta", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtOldPassword.Clear();
                    txtOldPassword.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (string.IsNullOrEmpty(txtNewPassword.Text))
                {
                    MessageBox.Show("Por favor ingrese la nueva contraseña", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    this.DialogResult = DialogResult.None;
                    return;
                }

                if (txtNewPassword.Text != txtConfirmPassword.Text)
                {
                    MessageBox.Show("Las contraseñas no coinciden", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    txtNewPassword.Clear();
                    txtConfirmPassword.Clear();
                    txtNewPassword.Focus();
                    this.DialogResult = DialogResult.None;
                    return;
                }

                Username = txtUsername.Text;
                NewPassword = txtNewPassword.Text;
            };

            // Add controls
            this.Controls.AddRange(new Control[] {
                lblUsername,
                lblOldPassword,
                lblNewPassword,
                lblConfirmPassword,
                txtUsername,
                txtOldPassword,
                txtNewPassword,
                txtConfirmPassword,
                btnOK,
                btnCancel
            });

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }
    }
}
