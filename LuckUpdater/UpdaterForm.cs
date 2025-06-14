using DevExpress.XtraEditors;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckUpdater
{
    public partial class UpdaterForm : XtraForm
    {
        private readonly string zipPath;
        private readonly string appPath;

        public UpdaterForm(string zipPath, string appPath)
        {
            InitializeComponent();
            this.zipPath = zipPath;
            this.appPath = appPath;
        }
        private async void UpdaterForm_Load(object sender, EventArgs e)
        {
            progressBar.Position = 0;
            lblStatus.Text = "Kiểm tra bản cập nhật...";
            await Task.Delay(500);
            try
            {
                string appDir = Path.GetDirectoryName(appPath);
                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    int total = archive.Entries.Count;
                    int current = 0;
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // Bỏ qua folder

                        string fullPath = Path.Combine(appDir, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(fullPath));
                        if (File.Exists(fullPath)) File.Delete(fullPath);
                        using (var entryStream = entry.Open())
                        using (var outputFile = File.Create(fullPath))
                        {
                            await entryStream.CopyToAsync(outputFile);
                        }
                        current++;
                        progressBar.Position = (int)(current * 100.0 / total);
                        lblStatus.Text = $"Cập nhật file ...";
                        await Task.Delay(10);
                    }
                }
                File.Delete(zipPath);
                lblStatus.Text = "Cập nhật phiên bản mới thành công. Khởi động lại ứng dụng...";
                progressBar.Position = 100;
                await Task.Delay(1000);
                Process.Start(appPath);
                Application.Exit();
            }
            catch (Exception)
            {
                XtraMessageBox.Show("Cập nhật phiên bản mới thất bại.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Application.Exit();
            }
        }
    }
}