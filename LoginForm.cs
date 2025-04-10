using System;
using System.Windows.Forms;

namespace PrintControl
{
    public partial class LoginForm : Form
    {
        // Credenciales hardcodeadas (en una aplicación real deberían estar encriptadas y en un archivo de configuración)
        private const string VALID_USERNAME = "admin";
        private const string VALID_PASSWORD = "admin123";

        public LoginForm()
        {
            InitializeComponent();
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (txtUsername.Text == VALID_USERNAME && txtPassword.Text == VALID_PASSWORD)
            {
                MainForm mainForm = new MainForm();
                this.Hide();
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
