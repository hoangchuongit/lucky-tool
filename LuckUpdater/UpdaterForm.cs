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
                string currentUpdaterExe = Path.GetFullPath(Application.ExecutablePath);

                using (ZipArchive archive = ZipFile.OpenRead(zipPath))
                {
                    int total = archive.Entries.Count;
                    int current = 0;

                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrWhiteSpace(entry.Name))
                            continue; // Bỏ qua folder

                        string fullPath = Path.GetFullPath(Path.Combine(appDir, entry.FullName));

                        // Chặn zip slip
                        if (!fullPath.StartsWith(Path.GetFullPath(appDir), StringComparison.OrdinalIgnoreCase))
                            throw new InvalidOperationException("File zip không hợp lệ.");

                        // Không cho updater tự ghi đè chính nó
                        if (string.Equals(fullPath, currentUpdaterExe, StringComparison.OrdinalIgnoreCase))
                            continue;

                        string dir = Path.GetDirectoryName(fullPath);
                        if (!Directory.Exists(dir))
                            Directory.CreateDirectory(dir);

                        string tempPath = fullPath + ".tmp";

                        // Ghi ra file tạm trước
                        using (var entryStream = entry.Open())
                        using (var outputFile = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                        {
                            await entryStream.CopyToAsync(outputFile);
                        }

                        // Nếu file cũ tồn tại thì thử xóa/replace với retry
                        await ReplaceFileWithRetry(tempPath, fullPath, 5, 500);

                        current++;
                        progressBar.Position = (int)(current * 100.0 / total);
                        lblStatus.Text = $"Đang cập nhật: {entry.FullName}";
                        await Task.Delay(10);
                    }
                }

                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                lblStatus.Text = "Cập nhật phiên bản mới thành công. Khởi động lại ứng dụng...";
                progressBar.Position = 100;
                await Task.Delay(1000);

                Process.Start(new ProcessStartInfo
                {
                    FileName = appPath,
                    UseShellExecute = true
                });

                Application.Exit();
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(
                    $"Cập nhật phiên bản mới thất bại:\n{ex.Message}",
                    "Lỗi",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);

                Application.Exit();
            }
        }

        private async Task ReplaceFileWithRetry(string tempPath, string targetPath, int maxRetry, int delayMs)
        {
            Exception lastEx = null;

            for (int i = 0; i < maxRetry; i++)
            {
                try
                {
                    if (File.Exists(targetPath))
                    {
                        File.SetAttributes(targetPath, FileAttributes.Normal);
                        File.Delete(targetPath);
                    }

                    File.Move(tempPath, targetPath);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    await Task.Delay(delayMs);
                }
            }

            // Dọn file tmp nếu cần
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { }
            }

            throw new IOException($"Không thể thay thế file: {targetPath}", lastEx);
        }
    }
}