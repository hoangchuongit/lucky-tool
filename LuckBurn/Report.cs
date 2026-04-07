using DevExpress.Export;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static LuckBurn.Models.PrefixNumberDto;

namespace LuckBurn
{
    public partial class Report : XtraForm
    {
        private readonly PrefixNumberController _dbController;

        public Report(string ApiKey)
        {
            InitializeComponent();
            _dbController = new PrefixNumberController(ApiKey);
            txtFromDate.EditValue = DateTime.Today;
            ViewReport();
        }

        private void BtnXemBaoCao_Click(object sender, EventArgs e)
        {
            ViewReport();
        }

        private async void ViewReport()
        {
            try
            {
                // Hiển thị thông báo đang tải
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                DateTime fromDate = txtFromDate.DateTime.AddHours(00).AddMinutes(00).AddSeconds(00);
                DateTime toDate = txtFromDate.DateTime.AddHours(23).AddMinutes(59).AddSeconds(59);

                // Lấy dữ liệu từ database
                var dataTable = await _dbController.GetRevenueTotal(fromDate.ToString("yyyy-MM-dd HH:mm:ss"), toDate.ToString("yyyy-MM-dd HH:mm:ss"));

                // Hiển thị thông báo đã tải xong
                Cursor = Cursors.Default;
                if (dataTable == null || dataTable.Length <= 0) return;

                // success
                var success = dataTable.FirstOrDefault(x => x.status == StatusEnum.SUCCESS.ToString());
                lbSuccessTotal.Text = success != null ? success.count_status.ToString() : "0";
                lbSuccessAmount.Text = success != null ? $"{success.sum_status:n0} VNĐ" : $"0 VNĐ";

                // waiting
                var waiting = dataTable.FirstOrDefault(x => x.status == StatusEnum.PENDING.ToString());
                lbWarningTotal.Text = waiting != null ? waiting.count_status.ToString() : "0";
                lbWarningAmount.Text = waiting != null ? $"{waiting.sum_status:n0} VNĐ" : $"0 VNĐ";

                //fail
                var fail = dataTable.FirstOrDefault(x => x.status == StatusEnum.FAIL.ToString());
                lbFailTotal.Text = fail != null ? fail.count_status.ToString() : "0";

                // Lấy dữ liệu từ database
                var dataTable2 = await _dbController.GetRevenueDetail(fromDate.ToString("yyyy-MM-dd HH:mm:ss"), toDate.ToString("yyyy-MM-dd HH:mm:ss"));

                // Gán dữ liệu vào GridControl
                gcReport.DataSource = dataTable2;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                XtraMessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}