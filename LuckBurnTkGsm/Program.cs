using AutoUpdaterDotNET;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace LuckBurnTK
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            //var mess = "[COM102]: +CUSD: 2,\"Your input is error or system busy,pls try again!\",15+CLCC: 1,0,6,0,0,\"19000180\",129,\"\"NO CARRIER";
            //mess = mess.Substring(mess.IndexOf("+CUSD"));
            //int? tkchinh = Common.ExtractBalance(mess);

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
            AutoUpdater.Start("https://luckburn.mobi/update-app/update.xml");
#endif

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
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