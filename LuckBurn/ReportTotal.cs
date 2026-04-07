using DevExpress.XtraEditors;
using System;
using System.Linq;
using System.Windows.Forms;
using static LuckBurn.Models.PrefixNumberDto;

namespace LuckBurn
{
    public partial class ReportTotal : XtraForm
    {
        private readonly PrefixNumberController _dbController;

        public ReportTotal(string apikey)
        {
            InitializeComponent();
            _dbController = new PrefixNumberController(apikey);
            // Thiết lập txtFromDate thành ngày đầu tháng hiện tại
            DateTime firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            txtFromDate.EditValue = firstDayOfMonth;
            // Thiết lập txtToDate thành ngày cuối tháng hiện tại
            DateTime lastDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            txtToDate.EditValue = lastDayOfMonth;
            Report();
        }

        private void BtnXemBaoCao_Click(object sender, EventArgs e)
        {
            Report();
        }

        private async void Report()
        {
            try
            {
                // Hiển thị thông báo đang tải
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                // Lấy dữ liệu từ database
                DateTime fromDate = txtFromDate.DateTime;
                DateTime toDate = txtToDate.DateTime.AddHours(23).AddMinutes(59).AddSeconds(59);
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
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                XtraMessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}