using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using DevExpress.XtraEditors;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckTelegram
{
    public partial class Main : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public Main()
        {
            InitializeComponent();
            CheckAdbStart();
        }

        private async void CheckAdbStart()
        {
            try
            {
                if (!AdbServer.Instance.GetStatus().IsRunning)
                {
                    AdbServer server = new AdbServer();
                    StartServerResult result = await server.StartServerAsync(@"F:\Nox\bin\adb.exe", false);
                    if (result != StartServerResult.Started)
                    {
                        Console.WriteLine("Can't start adb server");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"CheckAdbStart Error: {ex.Message}");
                XtraMessageBox.Show(ex.Message, "CheckAdbStart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnChooseFolderADB_Click(object sender, EventArgs e)
        {
            if (AdbFolderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                string selectedPath = AdbFolderBrowserDialog.SelectedPath;
                TxtFolderADB.Text = selectedPath;
                // Lưu vào settings
                Properties.Settings.Default.ADBFolderPath = selectedPath;
                Properties.Settings.Default.Save();
            }
        }

        private void Main_Load(object sender, EventArgs e)
        {
            string savedPath = Properties.Settings.Default.ADBFolderPath;
            if (!string.IsNullOrEmpty(savedPath))
                TxtFolderADB.Text = savedPath;
        }
    }
}