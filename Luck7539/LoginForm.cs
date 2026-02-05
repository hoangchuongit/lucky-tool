using DevExpress.XtraEditors;
using LuckBurnTK.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;

namespace LuckBurnTK
{
    public partial class LoginForm : XtraForm
    {
        private readonly HttpClient _httpClient;

        public LoginForm()
        {
            InitializeComponent();
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(Common.UrlBurnAUTH),
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Thiết lập tab cho các control
            txtEmail.TabIndex = 0;
            txtPassword.TabIndex = 1;
            chkRememberMe.TabIndex = 2;
            btnLogin.TabIndex = 3;
            AcceptButton = btnLogin;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            Text = $"Luck 7539 - {Application.ProductVersion}";
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string email = txtEmail.Text.Trim();
            string password = txtPassword.Text.Trim();

            if (string.IsNullOrEmpty(email))
            {
                XtraMessageBox.Show("Vui lòng nhập tài khoản email!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtEmail.Focus();
                return;
            }

            if (string.IsNullOrEmpty(password))
            {
                XtraMessageBox.Show("Vui lòng nhập mật khẩu!", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Focus();
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                var loginData = new { email, password };
                string jsonData = JsonConvert.SerializeObject(loginData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await _httpClient.PostAsync("login", content);

                if (response.IsSuccessStatusCode)
                {
                    var dateCurrent = response.Headers.Date?.ToLocalTime().ToString("yyyy-MM-dd");
                    string responseBody = await response.Content.ReadAsStringAsync();
                    dynamic result = JsonConvert.DeserializeObject(responseBody);

                    // Lưu thông tin đăng nhập
                    Properties.Settings.Default.UserEmail = chkRememberMe.Checked ? email : string.Empty;
                    Properties.Settings.Default.UserPassword = chkRememberMe.Checked ? password : string.Empty;
                    Properties.Settings.Default.RememberMe = chkRememberMe.Checked;
                    Properties.Settings.Default.Save();
                    Hide();
                    var mainForm = new BurnTKForm(result.api_key.ToString(), dateCurrent);
                    mainForm.ShowDialog();
                    Close();
                }
                else
                {
                    string errorMessage = "Đăng nhập thất bại. ";
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        errorMessage += "Tài khoản hoặc mật khẩu không đúng!";
                    else if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                        errorMessage += "Dữ liệu đăng nhập không hợp lệ!";
                    else
                        errorMessage += "Lỗi máy chủ. Vui lòng thử lại sau!";
                    XtraMessageBox.Show(errorMessage, "Lỗi",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (Properties.Settings.Default.RememberMe)
            {
                txtEmail.Text = Properties.Settings.Default.UserEmail;
                txtPassword.Text = Properties.Settings.Default.UserPassword;
                chkRememberMe.Checked = true;
                txtPassword.Focus();
            }
            else
                txtEmail.Focus();
        }
    }
}