using DevExpress.Export;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;
using System;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using static LuckBurnTK.Models.PrefixNumberDto;

namespace LuckBurnTK
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

        private void BtnXuatExcel_Click(object sender, EventArgs e)
        {
            try
            {
                // Kiểm tra nếu không có dữ liệu thì yêu cầu tải dữ liệu trước
                if (gvReport.RowCount == 0)
                {
                    XtraMessageBox.Show("Không có dữ liệu để xuất. Vui lòng ấn nút Xem để tải dữ liệu trước.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                using (SaveFileDialog saveDialog = new SaveFileDialog())
                {
                    saveDialog.Filter = "Excel files (*.xlsx)|*.xlsx";
                    saveDialog.FileName = $"Luck_Burn_{DateTime.Now:dd_MM_yyyy}.xlsx";

                    if (saveDialog.ShowDialog() == DialogResult.OK)
                    {
                        // Hiển thị thông báo đang xuất
                        Cursor = Cursors.WaitCursor;
                        Application.DoEvents();

                        // Cấu hình xuất file Excel
                        XlsxExportOptionsEx options = new XlsxExportOptionsEx
                        {
                            SheetName = $"Luck_Burn",
                            ExportType = ExportType.WYSIWYG, // Giữ nguyên định dạng hiển thị
                            AllowGrouping = DevExpress.Utils.DefaultBoolean.True, // Xuất luôn phần nhóm
                            ShowGridLines = true, // Hiển thị đường lưới
                            AllowFixedColumnHeaderPanel = DevExpress.Utils.DefaultBoolean.False
                        };

                        gvReport.BestFitColumns();
                        // Xuất dữ liệu từ GridView
                        gvReport.ExportToXlsx(saveDialog.FileName, options);

                        // Trả lại con trỏ bình thường
                        Cursor = Cursors.Default;

                        // Thông báo thành công
                        XtraMessageBox.Show("Xuất Excel thành công!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);

                        // Mở file Excel sau khi xuất
                        if (XtraMessageBox.Show("Bạn có muốn mở file Excel?", "Mở file báo cáo", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                            Process.Start(saveDialog.FileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                XtraMessageBox.Show($"Lỗi xuất Excel: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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
                if (dataTable == null || dataTable.Length <= 0) XtraMessageBox.Show("Không có dữ liệu trong khoảng thời gian này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);

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