using DevExpress.Export;
using DevExpress.XtraEditors;
using DevExpress.XtraPrinting;
using LuckBurnTK.Controllers;
using System;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;

namespace LuckBurnTK
{
    public partial class Report : XtraForm
    {
        private readonly DBController _dbController;
        private Guid _currentUserId;

        public Report(Guid userId)
        {
            InitializeComponent();
            _dbController = new DBController();
            // Thiết lập txtFromDate thành ngày đầu tháng hiện tại
            DateTime firstDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            txtFromDate.EditValue = firstDayOfMonth;

            // Thiết lập txtToDate thành ngày cuối tháng hiện tại
            DateTime lastDayOfMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(DateTime.Now.Year, DateTime.Now.Month));
            txtToDate.EditValue = lastDayOfMonth;
            _currentUserId = userId;
        }

        private void BtnXemBaoCao_Click(object sender, EventArgs e)
        {
            try
            {
                // Hiển thị thông báo đang tải
                Cursor = Cursors.WaitCursor;
                Application.DoEvents();

                // Lấy dữ liệu từ database
                DateTime fromDate = txtFromDate.DateTime;
                DateTime toDate = txtToDate.DateTime;
                DataTable dataTable = _dbController.GetReportHistorySMS(fromDate, toDate, _currentUserId);

                // Gán dữ liệu vào GridControl
                gcReport.DataSource = dataTable;

                // Hiển thị thông báo đã tải xong
                Cursor = Cursors.Default;
                if (dataTable.Rows.Count <= 0)
                    XtraMessageBox.Show("Không có dữ liệu trong khoảng thời gian này.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                XtraMessageBox.Show($"Lỗi khi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
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

        private void gvReport_CustomColumnDisplayText(object sender, DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventArgs e)
        {
            if (e.Column.FieldName == "created_at" && e.Value is DateTime utc)
            {
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"));
                e.DisplayText = localTime.ToString("dd/MM/yyyy HH:mm:ss");
            }
        }
    }
}