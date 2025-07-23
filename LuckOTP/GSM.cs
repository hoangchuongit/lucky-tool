using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using LuckBurn.Model;
using LuckOTP.Repositories;
using LuckOTP.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static LuckOTP.Models.PrefixNumberDto;

namespace LuckOTP
{
    public partial class GSM : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        /// Danh sách cổng COM đang ghi âm
        private readonly ConcurrentDictionary<string, CallDetail> RecordingPorts = new ConcurrentDictionary<string, CallDetail>();

        /// Nội dung ghi âm của từng cổng COM
        private readonly ConcurrentDictionary<string, byte[]> RecordingCOMs = new ConcurrentDictionary<string, byte[]>();

        /// Lưu CancellationTokenSource để dừng ghi âm sớm nếu có NO CARRIER
        private readonly ConcurrentDictionary<string, CancellationTokenSource> RecordingTokens
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        private readonly SimController simController;

        private readonly string AccountId;

        private readonly string ApiKey;

        public GSM(string apikey, string accountId)
        {
            InitializeComponent();
            AccountId = accountId;
            ApiKey = apikey;
            simController = new SimController(apikey);
            InitializeControls();
        }

        private void InitializeControls()
        {
            LoadCOMForm();
            TimerCheckSim.Enabled = true;
        }

        private void LoadCOMForm()
        {
            string[] portNames = SerialPort.GetPortNames();
            var fullPortNames = CheckComOnline();
            if (fullPortNames == null) return;
            InitializeSerialPorts(portNames, fullPortNames);
            gcCOM.DataSource = ComDataGrid;
            foreach (var port in SerialPorts)
            {
                var thread = new Thread(() =>
                {
                    try
                    {
                        InitializeModem(port);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{port.PortName} - LoadCOMForm Error: {ex.Message}");
                    }
                })
                { IsBackground = true };
                thread.Start();
            }
        }

        private void InitializeSerialPorts(string[] portNames, IEnumerable<Dictionary<string, string>> fullPortNames)
        {
            foreach (string port in portNames)
            {
                //if (port != "COM14") return;
                var regexPattern = $@"\b{Regex.Escape(port)}\b";
                var isValid = fullPortNames.FirstOrDefault(x => Regex.IsMatch(x["Caption"], regexPattern, RegexOptions.IgnoreCase));
                if (isValid == null) continue;
                var deviceID = isValid["DeviceID"].ToString().Split('\\')[2].ToString();
                SerialPort sp = new SerialPort(port)
                {
                    BaudRate = 115200,
                    Encoding = Encoding.ASCII,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    DataBits = 8,
                    Handshake = Handshake.None,
                    RtsEnable = true,
                    ReadTimeout = 5000,
                    WriteTimeout = 5000,
                    ReadBufferSize = 16384,
                };
                sp.DataReceived += SerialPort_DataReceived;
                sp.ErrorReceived += SerialPort_ErrorReceived;

                SerialPorts.Add(sp);
                MessageCOMs.TryAdd(sp.PortName, string.Empty);

                var data = new ComDto
                {
                    COM = sp.PortName,
                    STT = ComConfigManager.GetOrAssignSTT(sp.PortName, true),
                    DeviceID = deviceID,
                    ICCID = string.Empty,
                    PhoneNumber = string.Empty,
                    TKChinh = 0,
                    Message101 = string.Empty,
                    Message = string.Empty,
                    IsFinish = false
                };
                ComDataGrid.Add(data);
            }
            ComDataGrid = new BindingList<ComDto>(ComDataGrid.OrderBy(c => !string.IsNullOrEmpty(c.STT) ? int.Parse(c.STT) : -1).ToList());
        }

        private void InitializeModem(SerialPort sp)
        {
            try
            {
                if (sp == null) return;
                if (!sp.IsOpen) sp.Open();
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                SendATCommand(sp, "AT+IPR=115200");
                // Khởi động lại modem mà không thay đổi các cài đặt, chỉ tái thiết lập kết nối hoặc trạng thái của modem.
                SendATCommand(sp, "ATZ");
                // Đặt mã ký tự về ASCII
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                SendATCommand(sp, "AT+QSIMDET=1,0");
                // Kích hoạt chế độ thông báo sự kiện SIM
                SendATCommand(sp, "AT+QSIMSTAT=1");
                // lấy ICCID của sim
                SendATCommand(sp, "AT+QCCID");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = $"Error InitializeModem: {ex.Message}", "Message101");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            byte[] buffer = new byte[sp.BytesToRead];
            int bytesRead = 0;
            try
            {
                bytesRead = sp.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                logger.Error($"Error khi đọc từ SerialPort: {ex.Message}");
            }

            MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            //AppendLogToMemo(sp.PortName, MessageCOMs[sp.PortName]);
            Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);
            //logger.Info(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);

            // Nếu cổng COM chưa nằm trong danh sách ghi âm thì bổ sung vào danh sách. Nếu đã có thì ghi nối tiếp dữ liệu
            if (!MessageCOMs[sp.PortName].Contains("+QFDWL:") && MessageCOMs[sp.PortName].Contains("\r\nCONNECT\r\n"))
            {
                if (RecordingCOMs.ContainsKey(sp.PortName))
                {
                    if (buffer.Length > 0)
                    {
                        byte[] existingData = RecordingCOMs[sp.PortName];
                        byte[] newData = new byte[existingData.Length + buffer.Length];
                        Buffer.BlockCopy(existingData, 0, newData, 0, existingData.Length);
                        Buffer.BlockCopy(buffer, 0, newData, existingData.Length, buffer.Length);
                        RecordingCOMs[sp.PortName] = newData;
                    }
                }
                else
                {
                    byte[] cleanBuffer = Common.RemoveConnectHeader(buffer);
                    RecordingCOMs.TryAdd(sp.PortName, cleanBuffer);
                }
            }

            // Kiểm tra nếu cổng COM nằm trong danh sách ghi âm và kết thúc cuộc gọi thì lưu lại file .amr
            if (MessageCOMs[sp.PortName].Contains("+QFDWL:") && MessageCOMs[sp.PortName].Contains("\r\nCONNECT\r\n"))
            {
                if (RecordingPorts.ContainsKey(sp.PortName) && RecordingCOMs.ContainsKey(sp.PortName))
                    SaveRecord(sp);
            }

            // Có cuộc gọi đến thì cancel
            if (MessageCOMs[sp.PortName].Contains("RING"))
            {
                SendATCommand(sp, "ATH");
                MessageCOMs[sp.PortName] = string.Empty;
            }

            // Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
            ListenEventSIMStatus(sp);

            // Lắng nghe để lấy số serial SIM
            ListenEventICCID(sp);

            // Lắng nghe để lấy thông tin nhà mạng
            ListenEventTelecom(sp);

            // Lắng nghe để lấy thông tin Số điện thoại
            ListenEventPhoneNumber(sp);

            // Lắng nghe change IMEI thành công
            ListenEventChangeIMEI(sp);
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] Error: {e.EventType} - {MessageCOMs[sp.PortName]}");
            MessageCOMs[sp.PortName] = string.Empty;
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();
            if (sp.IsOpen) sp.Close();
            UpdateComData(sp.PortName, dto =>
            {
                dto.PhoneNumber = string.Empty; dto.TKChinh = 0; dto.Message101 = ""; dto.Message = ""; dto.IsFinish = true;
            }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
        }

        /// <summary>
        /// Khi lấy được status của SIM
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            var content = MessageCOMs[sp.PortName];
            // Trạng thái SIM đã tháo
            if ((content.Contains("+CPIN: NOT INSERTED") || content.Contains("+CPIN: NOT READY")) && content.Contains("+QSIMSTAT: 1,0"))
            {
                // Dừng nếu đang có cuộc gọi
                StopCallAndRecord(sp, false);
                // Xóa các tin nhắn cũ đi
                SendATCommand(sp, $"AT+CMGD=2,4");
                MessageCOMs[sp.PortName] = string.Empty;
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                try
                {
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.ICCID = string.Empty;
                        dto.PhoneNumber = string.Empty;
                        dto.TKChinh = 0;
                        dto.Message101 = string.Empty;
                        dto.Message = string.Empty;
                        dto.IsFinish = false;
                        dto.Telecom = string.Empty;
                    }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish", "Telecom");
                }
                catch (Exception ex)
                {
                    logger.Error($"Tháo SIM thất bại: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.Message101 = "Tháo sim thất bại!", "Message101");
                }
            }

            // Trạng thái SIM đã cắm
            if (content.Contains("+CPIN: READY") && content.Contains("+QSIMSTAT: 1,1"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                try
                {
                    // đánh số thứ tự COM nếu SIM chưa được đặt
                    var stt = ComConfigManager.GetOrAssignSTT(sp.PortName);
                    UpdateComData(sp.PortName, dto => dto.STT = stt, "STT");
                    // lấy ICCID của sim
                    SendATCommand(sp, "AT+QCCID");
                }
                catch (Exception ex)
                {
                    logger.Error($"Cắm SIM thất bại: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.Message101 = "Cắm sim thất bại!", "Message101");
                }
            }

        }

        /// <summary>
        /// Khi lấy được thông tin của ICCID
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventICCID(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("AT+QCCID") && content.Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName].AT_Command("AT+QCCID");
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName,
                        dto => dto.ICCID = mess.Replace("ATZ", "")
                                                .Replace("AT+CSCS=\"GSM\"", "")
                                                .Replace("AT+QCCID", "").Substring(0, 20),
                        "ICCID");
                    // Đặt module về chế độ Text Mode(ASCII)
                    SendATCommand(sp, "AT+CMGF=1");
                    // Nhận tin nhắn dưới dạng văn bản
                    SendATCommand(sp, "AT+CNMI=2,2");
                    // Gửi AT lấy thông tin nhà mạng
                    SendATCommand(sp, "AT+COPS?");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        /// <summary>
        /// Khi lấy được thông tin nhà mạng
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventTelecom(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("+COPS:") && content.Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName].AT_Command("AT+COPS?").ToLower();
                    MessageCOMs[sp.PortName] = string.Empty;
                    string provider = "";
                    if (mess.Contains("viettel")) provider = "Viettel";
                    else if (mess.Contains("mobifone")) provider = "Mobifone";
                    else if (mess.Contains("vinaphone")) provider = "Vinaphone";
                    else if (mess.Contains("vietnamobile")) provider = "VietnamMobile";
                    else provider = "Other";
                    UpdateComData(sp.PortName, dto => dto.Telecom = provider, "Telecom");
                    // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Telecom = "Unknown", "Telecom");
            }
        }

        /// <summary>
        /// Khi lấy được thông tin số điện thoại
        /// </summary>
        /// <param name="sp"></param>
        private async Task ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("+CUSD:") && content.Contains("\nOK"))
                {
                    // Tin nhắn gửi về từ SMS
                    var mess = content.AT_Command($"AT+CUSD=1,\"*101#\",15");
                    MessageCOMs[sp.PortName] = string.Empty;
                    //logger.Info($"mess [{sp.PortName}]: {mess}");
                    mess = mess.Substring(mess.IndexOf("+CUSD")).ToLower().Replace("du lieu", " du lieu ");
                    if (mess.Split(',').Length <= 0 && mess.Split('\"').Length <= 1) return;
                    var mess2 = mess.Split('\"')[1];
                    UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");
                    // Lấy số điện thoại từ tin nhắn gửi về
                    var phoneStr = mess2.Replace("\"", string.Empty);
                    if (string.IsNullOrEmpty(phoneStr)) return;
                    var phone = Common.GetPhoneNumber(phoneStr);
                    if (string.IsNullOrEmpty(phone)) return;
                    var item = ComDataGrid.FirstOrDefault(dto => dto.COM == sp.PortName);
                    await simController.UpsertSim(new Model.Sim()
                    {
                        phone_number = phone,
                        iccid = item.ICCID.ToString(),
                        sim_status = Model.sim_status_enum.SIM_INSERTED
                    });
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Khi có IMEI thay đổi
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("AT+EGMR=") && content.Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.Message101 = "Thay đổi IMEI cổng COM thành công. Chờ 5s.", "Message101");
                    Thread.Sleep(5000);
                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = "Thay đổi IMEI thất bại. thử lại sau 10s.", "Message101");
                Thread.Sleep(10000);
                SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
            }
        }

        /// <summary>
        /// Dừng ghi âm cuộc gọi
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="noCarrier">True: Tổng đài tự ngắt kết nối - False: Ngắt chủ động từ hệ thống</param>
        private async void StopCallAndRecord(SerialPort sp, bool noCarrier)
        {
            try
            {
                if (!RecordingPorts.ContainsKey(sp.PortName)) return;
                var callDetail = RecordingPorts[sp.PortName];
                callDetail.no_carrier = noCarrier;
                // Hủy token nếu có
                if (RecordingTokens.TryRemove(sp.PortName, out var token))
                {
                    token.Cancel();
                    token.Dispose();
                }
                // kết thúc cuộc gọi
                SendATCommand(sp, "ATH");
                // Dừng ghi âm
                SendATCommand(sp, "AT+QAUDRD=0", 1000);
                // Cập nhật thời gian dừng
                callDetail.end_record = DateTime.Now;
                // Release Slot treen server
                var releaseSlotReq = new ReleaseSlotReq()
                {
                    history_id = callDetail.history_id.ToString(),
                    prefix = callDetail.prefix,
                    prefix_unit = callDetail.prefix_unit,
                    request_id = callDetail.request_id,
                    start_call = callDetail.start_call.ToString("yyyy-MM-dd HH:mm:ss"),
                    end_call = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    start_record = callDetail.start_record.ToString("yyyy-MM-dd HH:mm:ss"),
                    end_record = callDetail.end_record.ToString("yyyy-MM-dd HH:mm:ss"),
                    duration = callDetail.call_duration,
                    no_carrier = callDetail.no_carrier ? 1 : 0,
                };
                //await simController.ReleaseSlot(releaseSlotReq);
                // Tải xuống file âm thanh record.amr từ RAM của module.
                sp.WriteLine("AT+QFDWL=\"RAM:record.amr\"");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - StopCallAndRecord Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lưu bản ghi âm thành file
        /// </summary>
        /// <param name="sp"></param>
        private async void SaveRecord(SerialPort sp)
        {
            try
            {
                // Kiểm tra key trong dictionary RecordingCOMs
                if (!RecordingCOMs.ContainsKey(sp.PortName))
                {
                    logger.Info($"Save Record: RecordingCOMs does not contain the key: {sp.PortName}");
                    MessageCOMs[sp.PortName] = string.Empty;
                    return;
                }
                var callDetail = RecordingPorts[sp.PortName];
                // save file record vào folder
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string recordDirectory = Path.Combine(baseDirectory, "record");
                Directory.CreateDirectory(recordDirectory);
                byte[] audioData = RecordingCOMs[sp.PortName];
                var phone = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName).PhoneNumber ?? Guid.NewGuid().ToString();
                string filePath = Path.Combine(recordDirectory, $"{phone ?? callDetail.history_id.ToString()}_{callDetail.prefix}_{DateTime.Now:yyyyMMddHHmmss}.amr");
                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fs.WriteAsync(audioData, 0, audioData.Length);
                    await fs.FlushAsync();
                }
                await Task.Delay(500);
                //var result = await simController.ReleaseUploadFile(callDetail.history_id.ToString(), filePath);
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                MessageCOMs[sp.PortName] = string.Empty;
                logger.Error($"[{sp.PortName}] - SaveRecord Error: {ex.Message}");
            }
        }

        private void BtnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BindingList<ComDto> dataSource = gvCOM.DataSource as BindingList<ComDto>;
            if (dataSource != null)
            {
                var newDataSource = new BindingList<ComDto>();
                var status = true;
                foreach (var item in dataSource)
                {
                    var duplicateStt = dataSource.Where(x => x != item && x.STT == item.STT).FirstOrDefault();
                    if (duplicateStt != null && duplicateStt.STT != "")
                    {
                        status = false;
                        MessageBox.Show($"Cảnh báo: STT {item.STT} bị trùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    newDataSource.Add(item);
                }
                if (status)
                {
                    var sortedData = new BindingList<ComDto>(newDataSource.OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1).ToList());
                    ComDataGrid.Clear();
                    List<string> deviceIDs = new List<string>();
                    foreach (var item in sortedData)
                    {
                        ComDataGrid.Add(item);
                        if (item.STT == "") continue;
                        deviceIDs.Add(item.DeviceID);
                    }
                    var COMs = string.Join(",", deviceIDs);
                    Properties.Settings.Default.COMs = COMs;
                    Properties.Settings.Default.Save();
                    XtraMessageBox.Show("Đã cập nhật STT cổng COM", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            else
            {
                logger.Error("Cảnh báo: DataSource là null!");
            }
        }

        private void BtnResetComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn đặt lại STT cổng COM?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                // Xoá file config nếu tồn tại
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
                if (File.Exists(configPath))
                    File.Delete(configPath);
                foreach (var item in ComDataGrid)
                {
                    item.STT = "";
                }
                gvCOM.RefreshData();
            }
        }

        private void BtnChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn thay đổi IMEI của tất cả cổng COM?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var sp in SerialPorts)
                {
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.Message101 = "Đổi IMEI cổng COM..."; dto.Message = "";
                            }, "Message101", "Message");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thread.Start();
                }
            }
        }

        private void BtnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn reset lại toàn bộ cổng COM?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var sp in SerialPorts)
                {
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.ICCID = string.Empty;
                                dto.PhoneNumber = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Reset cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message");
                            SendATCommand(sp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Module được thiết lập để sử dụng chế độ "Auto Baud Rate Detection" (Tự động nhận diện tốc độ truyền).
                            SendATCommand(sp, "AT+IPR=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=1");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 10000);
                            // Đặt mã ký tự về ASCII
                            SendATCommand(sp, "AT+CSCS=\"GSM\"");
                            // Đặt module về chế độ Text Mode (ASCII)
                            SendATCommand(sp, "AT+CMGF=1");
                            // Nhận tin nhắn dưới dạng văn bản
                            SendATCommand(sp, "AT+CNMI=2,2");
                            // lấy ICCID của sim
                            SendATCommand(sp, "AT+QCCID");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void BtnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn khôi phục cài đặt gốc?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var sp in SerialPorts)
                {
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.ICCID = string.Empty;
                                dto.PhoneNumber = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Khôi phục cài đặt gốc cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message");
                            SendATCommand(sp, "AT&F", 60000);
                            //
                            SendATCommand(sp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Module được thiết lập để sử dụng chế độ "Auto Baud Rate Detection" (Tự động nhận diện tốc độ truyền).
                            SendATCommand(sp, "AT+IPR=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=1");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
                            // Đặt mã ký tự về ASCII
                            SendATCommand(sp, "AT+CSCS=\"GSM\"");
                            // Đặt module về chế độ Text Mode (ASCII)
                            SendATCommand(sp, "AT+CMGF=1");
                            // Nhận tin nhắn dưới dạng văn bản
                            SendATCommand(sp, "AT+CNMI=2,2");
                            // lấy ICCID của sim
                            SendATCommand(sp, "AT+QCCID");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            var fullPortNames = CheckComOnline();
            if (fullPortNames == null) return;
            foreach (var item in ComDataGrid)
            {
                var sp = SerialPorts.Find(x => x.PortName == item.COM);
                if (sp == null) continue;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!sp.IsOpen) sp.Open();
                        if (string.IsNullOrEmpty(item.PhoneNumber))
                        {
                            sp.DiscardInBuffer();
                            sp.DiscardOutBuffer();
                            SendATCommand(sp, "AT+QCCID");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.ICCID = "COM ERROR";
                            dto.PhoneNumber = "COM ERROR";
                            dto.TKChinh = 0;
                            dto.Message101 = "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. This PC > Manager > Device Manager > Ports (COM & LPT)";
                            dto.Message = "COM ERROR";
                            dto.IsFinish = true;
                            dto.Telecom = "COM ERROR";
                        }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish", "Telecom");
                    }
                });
            }
        }

        private void BurnTKForm_Load(object sender, EventArgs e)
        {
            Text = $"Luck OTP - {Application.ProductVersion}";
        }

        private IEnumerable<Dictionary<string, string>> CheckComOnline()
        {
            var fullPortNames = Common.GetFullPortNames();
            var countComsOnline = fullPortNames.Count();
            if (countComsOnline == 0)
            {
                TimerCheckSim.Enabled = false;
                XtraMessageBox.Show("Toàn bộ cổng COM bị lỗi. Đảm bảo các cổng COM không có dấu chấm than.\nThis PC > Manager > Device Manager > Ports (COM & LPT)", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
                return null;
            }
            else return fullPortNames;
        }

        private void UpdateComData(string portName, Action<ComDto> updateAction, params string[] propertyNames)
        {
            try
            {
                var item = ComDataGrid.FirstOrDefault(dto => dto.COM == portName);
                if (item == null) return;
                updateAction(item);
                InvokeIfRequired(() =>
                {
                    int rowHandle = gvCOM.LocateByValue("COM", portName);
                    if (rowHandle >= 0 && propertyNames != null && propertyNames.Any())
                        foreach (var propertyName in propertyNames)
                        {
                            gvCOM.RefreshRowCell(rowHandle, gvCOM.Columns[propertyName]);
                        }
                });
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateComData error: {ex.Message}");
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (gcCOM.InvokeRequired) gcCOM.Invoke(action);
            else action();
        }

        private void SendATCommand(SerialPort sp, string command, int timeout = 1000)
        {
            try
            {
                MessageCOMs[sp.PortName] = string.Empty;
                sp.WriteLine($"{command}{Environment.NewLine}");
                Thread.Sleep(timeout);
                MessageCOMs[sp.PortName].AT_Command(command);
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SendATCommand: {ex.Message}");
            }
        }
    }
}