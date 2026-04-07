using DevExpress.XtraEditors;
using LuckBurn.Utils;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Windows.Forms;
using static LuckBurn.Models.PrefixNumberDto;

namespace LuckBurn
{
    public partial class UserInforForm : XtraForm
    {
        private readonly HttpClient _httpClient;

        public UserInforForm(string ApiKey)
        {
            InitializeComponent();
            _httpClient = new HttpClient
            {
                BaseAddress = new Uri(Common.UrlBurnAUTH),
                Timeout = TimeSpan.FromSeconds(30)
            };
            _httpClient.DefaultRequestHeaders.Add("x-api-key", ApiKey);
            LoadForm();
        }

        private async void LoadForm()
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync("get-info");
                if (response.IsSuccessStatusCode)
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    var data = JsonConvert.DeserializeObject<GetUserInfor>(responseBody);
                    txtFullname.Text = data.fullname?.ToString();
                    txtNganHang.Text = data.ngan_hang?.ToString();
                    txtSoTaiKhoan.Text = data.so_tai_khoan?.ToString();
                    txtChuTaiKhoan.Text = data.chu_tai_khoan?.ToString();
                }
                else XtraMessageBox.Show($"Tải thông tin người dùng thất bại!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void BtnLogin_Click(object sender, EventArgs e)
        {
            string fullname = txtFullname.Text.Trim();
            string ngan_hang = txtNganHang.Text.Trim();
            string so_tai_khoan = txtSoTaiKhoan.Text.Trim();
            string chu_tai_khoan = txtChuTaiKhoan.Text.Trim();

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                var changePassData = new { fullname, ngan_hang, so_tai_khoan, chu_tai_khoan };
                string jsonData = JsonConvert.SerializeObject(changePassData);
                var content = new StringContent(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await _httpClient.PostAsync("update-info", content);
                if (response.IsSuccessStatusCode) Close();
                else XtraMessageBox.Show("Cập nhật thông tin tài khoản thất bại", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
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