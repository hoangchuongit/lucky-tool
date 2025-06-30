using DevExpress.XtraEditors;
using LuckBurnTK.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows.Forms;

namespace LuckBurnTK
{
    public partial class ChangePassForm : XtraForm
    {
        private readonly HttpClient _httpClient;

        public ChangePassForm(string ApiKey)
        {
            InitializeComponent();
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri(Common.UrlBurnAUTH),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            txtPassOld.TabIndex = 0;
            txtPassNew.TabIndex = 1;
            btnLogin.TabIndex = 3;
            AcceptButton = btnLogin;
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string old_password = txtPassOld.Text.Trim();
            string new_password = txtPassNew.Text.Trim();

            if (string.IsNullOrEmpty(old_password))
            {
                XtraMessageBox.Show("Vui lòng nhập mật khẩu cũ!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassOld.Focus();
                return;
            }

            if (string.IsNullOrEmpty(new_password))
            {
                XtraMessageBox.Show("Vui lòng nhập mật khẩu mới!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassNew.Focus();
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                var changePassData = new { old_password, new_password };
                string jsonData = JsonConvert.SerializeObject(changePassData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync("change-password", content);
                if (response.IsSuccessStatusCode)
                {
                    Close();
                }
                else
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    try
                    {
                        var errorObj = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(errorContent);
                        string message = errorObj.GetProperty("message").GetString();
                        XtraMessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch
                    {
                        XtraMessageBox.Show($"Lỗi không xác định: {errorContent}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
    }
}