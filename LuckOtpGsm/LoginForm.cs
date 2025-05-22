using LuckOTP.Repositories;
using System;
using System.Windows.Forms;

namespace LuckOTP
{
    public partial class LoginForm : DevExpress.XtraEditors.XtraForm
    {
        public LoginForm()
        {
            InitializeComponent();
        }

        private async void simpleButton1_Click(object sender, EventArgs e)
        {
            try
            {
                string username = textEdit1.Text.Trim();
                string password = textEdit2.Text.Trim();
                string country = comboBoxEdit1.SelectedItem.ToString();
                bool rememberMe = checkEdit1.Checked;

                if (string.IsNullOrWhiteSpace(username))
                {
                    MessageBox.Show("Account is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textEdit1.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(password))
                {
                    MessageBox.Show("Password is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    textEdit2.Focus();
                    return;
                }

                if (string.IsNullOrWhiteSpace(country))
                {
                    MessageBox.Show("Country is required!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    comboBoxEdit1.Focus();
                    return;
                }

                var sv = new BaseRepository();
                var accountId = await sv.Login(username, password);
                if (accountId != null)
                {
                    if (rememberMe)
                    {
                        Properties.Settings.Default.Username = username;
                        Properties.Settings.Default.Password = password;
                        Properties.Settings.Default.RememberMe = "true";
                        Properties.Settings.Default.Country = country;
                        Properties.Settings.Default.Save();
                    }
                    else Properties.Settings.Default.Reset();

                    Hide();

                    GSMForm gsmForm = new GSMForm(accountId.ToString(), country);
                    gsmForm.ShowDialog();

                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void Login_Load(object sender, EventArgs e)
        {
            if (Properties.Settings.Default.RememberMe == "true")
            {
                textEdit1.Text = Properties.Settings.Default.Username;
                textEdit2.Text = Properties.Settings.Default.Password;
                comboBoxEdit1.SelectedItem = Properties.Settings.Default.Country;
                checkEdit1.Checked = true;
            }
        }
    }
}