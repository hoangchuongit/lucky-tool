using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckTelegram
{
    public partial class Main : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private BindingList<Device> DeviceDataSource { get; set; } = new BindingList<Device>();
        private List<DeviceData> DeviceDatas { get; set; } = new List<DeviceData>();
        private bool adbReady = true;

        public Main()
        {
            InitializeComponent();
        }

        private void Main_Load(object sender, EventArgs e)
        {
            string savedPath = Properties.Settings.Default.ADBFolderPath;
            if (!string.IsNullOrEmpty(savedPath))
                BtnEditAdb.Text = savedPath;
        }

        private void Main_Shown(object sender, EventArgs e)
        {
            CheckAdbStart();
            LoadDevices();
        }

        private void BtnEditAdb_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            try
            {
                AdbServer server = new AdbServer();
                server.StopServerAsync();
                if (AdbFolderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = AdbFolderBrowserDialog.SelectedPath;
                    BtnEditAdb.Text = selectedPath;
                    // Lưu vào settings
                    Properties.Settings.Default.ADBFolderPath = selectedPath;
                    Properties.Settings.Default.Save();
                    // Check lại adb config
                    CheckAdbStart();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"ChooseFolderADB Error: {ex.Message}");
                XtraMessageBox.Show("Chọn đường dẫn đến folder Adb thất bại!", "ChooseFolderADB Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnProxy_Click(object sender, EventArgs e)
        {
            try
            {
                using (var form = new ProxyForm())
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        Console.WriteLine(form.ProxyText);
                        var proxyList = form.ProxyText
                            .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(p => p.Trim())
                            .Where(p => !string.IsNullOrWhiteSpace(p))
                            .ToList();
                        for (int i = 0; i < proxyList.Count && i < DeviceDataSource.Count; i++)
                        {
                            DeviceDataSource[i].Proxy = proxyList[i];
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"BtnProxy Error: {ex.Message}");
                XtraMessageBox.Show("Nhập proxy thất bại!", "BtnProxy Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExportExcel_Click(object sender, EventArgs e)
        {
        }

        private void BtnClearDBSim_Click(object sender, EventArgs e)
        {
        }

        private void BtnRunAll_Click(object sender, EventArgs e)
        {
            try
            {
                if (adbReady)
                {
                    foreach (var deviceSource in DeviceDataSource)
                    {
                        if (!deviceSource.IsRunning)
                        {
                            var deviceData = DeviceDatas.FirstOrDefault(d => d.Serial == deviceSource.Serial);
                            StartDeviceAsTask(deviceData);
                        }
                    }
                }
                else XtraMessageBox.Show("Adb chưa sẵn sàng. Kiểm tra lại đường dẫn đến thưc mục chưa adb.exe!", "BtnRunAll Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                logger.Error($"BtnRunAll Error: {ex.Message}");
                XtraMessageBox.Show("Bắt đầu chạy thất bại!", "BtnRunAll Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStopAll_Click(object sender, EventArgs e)
        {
        }

        private async void CheckAdbStart()
        {
            try
            {
                if (string.IsNullOrEmpty(BtnEditAdb.Text))
                {
                    adbReady = false;
                    XtraMessageBox.Show("Chưa cấu hình đường dẫn đến folder chứa adb.exe", "Adb Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                else
                {
                    if (AdbServer.Instance.GetStatus().IsRunning) return;
                    loadingPanel.Show();
                    AdbServer server = new AdbServer();
                    StartServerResult result = await server.StartServerAsync($"{BtnEditAdb.Text}\\adb.exe", false);
                    if (result == StartServerResult.Started)
                    {
                        adbReady = true;
                        loadingPanel.Caption = "Adb đã sẵn sàng";
                    }
                    else
                    {
                        adbReady = false;
                        XtraMessageBox.Show("Không thể start adb.exe, kiểm tra lại cấu hình adb", "Adb Config", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    loadingPanel.Hide();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"CheckAdbStart Error: {ex.Message}");
                XtraMessageBox.Show("Cấu hình đường dẫn đến folder chứa adb.exe không đúng. Kiểm tra lại!", "CheckAdbStart Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void LoadDevices()
        {
            try
            {
                if (!adbReady)
                {
                    XtraMessageBox.Show("Adb chưa sẵn sàng làm việc.", "Adb Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                AdbClient client = new AdbClient();
                IEnumerable<DeviceData> devices = await client.GetDevicesAsync();
                DeviceDatas = devices.ToList();
                var tasks = new List<Task>();
                foreach (DeviceData deviceData in devices)
                {
                    var data = new Device
                    {
                        Serial = deviceData.Serial,
                        Proxy = "",
                        Message = string.Empty,
                        Status = deviceData.State,
                        IsRunning = false
                    };
                    DeviceDataSource.Add(data);
                }
                GcDevice.DataSource = DeviceDataSource;
            }
            catch (Exception ex)
            {
                logger.Error($"LoadDevices Error: {ex.Message}");
                XtraMessageBox.Show("Tải danh sách thiết bị thất bại. Kiểm tra lại!", "LoadDevices Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void StartDeviceAsTask(DeviceData deviceData)
        {
            Task.Factory.StartNew(() => StartDeviceThreadSafe(deviceData), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void StartDeviceThreadSafe(DeviceData deviceData)
        {
            //try
            //{
            //    this.InvokeIfRequired(() =>
            //    {
            //        device.IsRunning = true;
            //        var rowHandle = GvDevice.GetRowHandle(DeviceDataSource.IndexOf(device));
            //        GvDevice.RefreshRowCell(rowHandle, GcAction);
            //    });

            //    // Giả lập lệnh ADB thực thi
            //    var client = new AdbClient();
            //    var receiver = new ConsoleOutputReceiver();
            //    client.ExecuteRemoteCommand("echo Hello from " + device.Serial, new DeviceData { Serial = device.Serial }, receiver);
            //    Thread.Sleep(2000);

            //    this.InvokeIfRequired(() =>
            //    {
            //        device.IsRunning = false;
            //        var rowHandle = GvDevice.GetRowHandle(DeviceDataSource.IndexOf(device));
            //        GvDevice.RefreshRowCell(rowHandle, GcAction);
            //    });
            //}
            //catch (Exception ex)
            //{
            //    UpdateGridView(deviceData.Serial, dto => dto.Message = "", "Message");
            //    logger.Error($"Thread Error [{device.Serial}]: {ex.Message}");
            //}
        }

        private void UpdateGridView(string serial, Action<Device> updateAction, params string[] propertyNames)
        {
            try
            {
                var device = DeviceDataSource.FirstOrDefault(dto => dto.Serial == serial);
                if (device == null) return;
                updateAction(device);
                this.InvokeIfRequired(() =>
                {
                    var rowHandle = GvDevice.GetRowHandle(DeviceDataSource.IndexOf(device));
                    if (rowHandle >= 0 && propertyNames != null && propertyNames.Any())
                    {
                        foreach (var propertyName in propertyNames)
                        {
                            GvDevice.RefreshRowCell(rowHandle, GvDevice.Columns[propertyName]);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateGridView error: {ex.Message}");
            }
        }
    }
}