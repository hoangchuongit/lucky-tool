using AutoUpdaterDotNET;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace LuckBurn
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            //var mess = Common.DecodeUnicode("00420061006E0020006400610020007400680061006E006800200074006F0061006E0020007400680061006E006800200063006F006E00670020006700690061006F002000640069006300680020002000540065006E0020006700690061006F00200064006900630068003A00200056004D0047005F004B0056005F0076006D0067006D006500640069006100200020004D00610020006700690061006F00200064006900630068003A00200056004D0047005F0032003000320035003000380031003200310034003500370031003100300039003500390034002000200053006F0020007400690065006E003A0020003100300030003000200056004E004400200020004E006F0069002000640075006E0067003A0020004400610020006E00680061006E002000740068006F006E0067002000740069006E");
            //Console.WriteLine(mess);

            RemoveFileInFolderRecord();
            CultureInfo viVN = new CultureInfo("vi-VN");
            Thread.CurrentThread.CurrentCulture = viVN;
            Thread.CurrentThread.CurrentUICulture = viVN;

#if !DEBUG
            AutoUpdater.Mandatory = true;
            AutoUpdater.UpdateMode = Mode.Forced;
            AutoUpdater.ShowRemindLaterButton = false;
            AutoUpdater.ShowSkipButton = false;
            AutoUpdater.ReportErrors = true;
            AutoUpdater.RunUpdateAsAdmin = true; // nếu ghi vào C:\Program Files
            AutoUpdater.CheckForUpdateEvent += AutoUpdaterOnCheckForUpdateEvent;
            AutoUpdater.Start("https://gmeta.io.vn/update-app/update.xml");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new CheckToolForm());
        }

        private static void RemoveFileInFolderRecord()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string recordDirectory = Path.Combine(baseDirectory, "record");
            if (!Directory.Exists(recordDirectory)) return;
            foreach (string file in Directory.GetFiles(recordDirectory))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RemoveFileInFolderRecord: {ex.Message}");
                }
            }
        }

        private static void AutoUpdaterOnCheckForUpdateEvent(UpdateInfoEventArgs args)
        {
            if (args == null)
            {
                MessageBox.Show("Không kiểm tra được bản cập nhật. Kiểm tra lại mạng Internet của bạn.", "Luck Auto Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (args.Error != null)
            {
                MessageBox.Show(args.Error.Message, "Luck Auto Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            if (!args.IsUpdateAvailable) return;
            string zipUrl = args.DownloadURL;
            string tempZipPath = Path.Combine(Path.GetTempPath(), "update.zip");
            using (var client = new WebClient())
            {
                try
                {
                    client.DownloadFile(zipUrl, tempZipPath);
                }
                catch (Exception)
                {
                    MessageBox.Show($"Không tải được bản cập nhật mới", "Luck Auto Update", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            string exePath = Application.ExecutablePath;
            string updaterPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LuckUpdater.exe");
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = updaterPath,
                    Arguments = $"\"{tempZipPath}\" \"{exePath}\"",
                    CreateNoWindow = true,         // Không hiện cửa sổ CMD
                    UseShellExecute = false,       // Bắt buộc để ẩn
                    WindowStyle = ProcessWindowStyle.Hidden // Ẩn cửa sổ luôn
                };
                Process.Start(psi);
            }
            catch (Exception)
            {
                //MessageBox.Show($"Không thể khởi động trình cập nhật: {ex.Message}");
                return;
            }
            Thread.Sleep(500); // chờ chút để Process.Start hoạt động
            Application.Exit();
            Environment.Exit(0);
        }
    }
}