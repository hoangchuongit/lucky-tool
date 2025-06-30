using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using LuckBurn.Model;
using LuckBurnTK.Utils;
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
using static LuckBurnTK.Models.PrefixNumberDto;

namespace LuckBurnTK
{
    public partial class BurnTKForm : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly GuideFlyoutPanel panel;
        private readonly int countLessons;

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

        /// Danh sách các cổng COM đang upload file đến thiết bị
        private readonly ConcurrentDictionary<string, FileToCom> FileToCOMs = new ConcurrentDictionary<string, FileToCom>();

        private readonly PrefixNumberController _prefixController;

        private readonly string ApiKey;

        public BurnTKForm(string apikey)
        {
            InitializeComponent();
            countLessons = 5;
            panel = new GuideFlyoutPanel(this, countLessons);
            ApiKey = apikey;
            _prefixController = new PrefixNumberController(apikey);
            InitializeControls();
        }

        private void InitializeControls()
        {
            LoadNotification();
            txtMinAccountControl.EditValue = Properties.Settings.Default.MinAccount;
            LoadCOMForm();
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
                _ = Task.Run(() =>
                {
                    try
                    {
                        InitializeModem(port);
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"{port.PortName} - LoadCOMForm Error: {ex.Message}");
                    }
                });
            }
        }

        private void InitializeSerialPorts(string[] portNames, IEnumerable<Dictionary<string, string>> fullPortNames)
        {
            var dataCOms = Properties.Settings.Default.COMs.Trim().Split(',');
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
                    STT = dataCOms.FindIndex(x => x == deviceID) > -1 ? (dataCOms.FindIndex(x => x == deviceID) + 1).ToString() : "-1",
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
            ComDataGrid = new BindingList<ComDto>(ComDataGrid.OrderBy(c => int.Parse(c.STT)).ToList());
        }

        private void InitializeModem(SerialPort sp)
        {
            try
            {
                if (sp == null) return;
                if (!sp.IsOpen) sp.Open();
                // Khởi động lại modem mà không thay đổi các cài đặt, chỉ tái thiết lập kết nối hoặc trạng thái của modem.
                SendATCommand(sp, "ATZ");
                // Đặt mã ký tự về ASCII
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                // Đặt module về chế độ Text Mode (ASCII)
                SendATCommand(sp, "AT+CMGF=1");
                // Nhận tin nhắn dưới dạng văn bản
                SendATCommand(sp, "AT+CNMI=2,2");
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
            Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);
            //logger.Info(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);

            if (MessageCOMs[sp.PortName].Contains("RING"))
            {
                SendATCommand(sp, "ATH");
                MessageCOMs[sp.PortName] = string.Empty;
            }

            if (MessageCOMs[sp.PortName].Contains("CMS ERROR"))
            {
                UpdateComData(sp.PortName, dto =>
                {
                    dto.Message101 = "Cổng COM gặp lỗi. Rút SIM và chờ 20s sau đó lắp lại.";
                    dto.Message = "";
                    dto.IsFinish = true;
                }, "Message101", "Message", "IsFinish");
            }

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
                RecordingCOMs.TryRemove(sp.PortName, out byte[] _);
            }

            // Lắng nghe change IMEI thành công
            ListenEventChangeIMEI(sp);

            // Lắng nghe xem SIM có được cắm vào cổng COM hay không
            ListenEventSIMInsert(sp);

            // Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
            ListenEventSIMStatus(sp);

            // Lắng nghe để lấy số serial SIM
            ListenEventICCID(sp);

            // Lắng nghe để lấy thông tin nhà mạng
            ListenEventTelecom(sp);

            // Lắng nghe để lấy thông tin Số điện thoại
            ListenEventPhoneNumber(sp);

            // Lắng nghe cuộc gọi của thuê bao với tổng đài
            ListenEventCallPrefix(sp);

            // Xử lý trường hợp upload file amr tới thiết bị
            //ListenEventUploadAmrFileToDevice(sp);

            // Lắng nghe xem sim gửi SMS thành công chưa
            ListenEventSendSms(sp);

            // Trigger: Trạng thái SIM đã bị tháo
            if (MessageCOMs[sp.PortName].Contains("+CPIN: NOT READY"))
            {
                try
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    // Cập nhật gridview
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
                }
            }

            // Trigger: Trạng thái SIM đã cắm
            if (MessageCOMs[sp.PortName].Contains("Call Ready") && MessageCOMs[sp.PortName].Contains("+CPIN: READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                //UpdateComData(sp.PortName, dto => dto.Message101 = $"SIM đã sẵn sàng.s", "Message101");
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] Error: {e.EventType} - {MessageCOMs[sp.PortName]}");
            MessageCOMs[sp.PortName] = string.Empty;
            UpdateComData(sp.PortName, dto =>
            {
                dto.PhoneNumber = "Phone Unknow"; dto.TKChinh = 0; dto.Message101 = ""; dto.Message = ""; dto.IsFinish = true;
            }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
        }

        /// <summary>
        /// Khi có IMEI thay đổi
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+EGMR=") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.Message101 = "Thay đổi IMEI cổng COM thành công. Chờ 5s.", "Message101");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = "Thay đổi IMEI thất bại. thử lại sau 10s.", "Message101");
                Thread.Sleep(10000);
                SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
            }
        }

        /// <summary>
        /// Khi SIM bắt đầu insert
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSIMInsert(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+QSIMSTAT:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+QSIMSTAT?");
                if (mess.Contains("+QSIMSTAT: 0,1"))
                {
                    var messSplit = mess.Replace("+QSIMSTAT: ", string.Empty).Split(',');
                    if (messSplit.Length < 2 || messSplit[1] != "1") return;
                    MessageCOMs[sp.PortName] = string.Empty;
                    // đánh số thứ tự COM nếu SIM chưa được đặt
                    var currentRow = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName);
                    if (currentRow.STT == "-1")
                    {
                        var dataCOms = Properties.Settings.Default.COMs.Trim().Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                        //currentRow.Stt = (dataCOms.Count + 1).ToString();
                        var stt = (dataCOms.Count + 1).ToString();
                        // Cập nhật Properties.Settings
                        dataCOms.Add(currentRow.DeviceID);
                        Properties.Settings.Default.COMs = string.Join(",", dataCOms);
                        Properties.Settings.Default.Save();
                        // Làm mới GridView (cập nhật dòng cụ thể)
                        UpdateComData(sp.PortName, dto => dto.STT = stt, "STT");
                    }
                    SendATCommand(sp, "AT+CPIN?");
                }
                else MessageCOMs[sp.PortName] = string.Empty;
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.PhoneNumber = "Phone Unknow", "PhoneNumber");
            }
        }

        /// <summary>
        /// Khi lấy được status của SIM
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+CPIN:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command();
                if (mess.Contains("+CPIN: READY"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.Message101 = "SIM đã sẵn sàng.", "Message101");
                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = string.Empty, "Message101");
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
                if (!MessageCOMs[sp.PortName].Contains("AT+QCCID") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+QCCID");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.ICCID = mess.Replace("+QCCID", "").Substring(0, 20), "ICCID");
                // Gửi AT lấy thông tin nhà mạng
                SendATCommand(sp, "AT+COPS?");
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
                if (!MessageCOMs[sp.PortName].Contains("+COPS:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+COPS?").ToLower();
                MessageCOMs[sp.PortName] = string.Empty;
                string provider = "";
                //logger.Info($"ListenEventTelecom: {mess}");
                if (mess.Contains("viettel")) provider = "Viettel";
                else if (mess.Contains("mobifone")) provider = "Mobifone";
                else if (mess.Contains("vinaphone")) provider = "Vinaphone";
                else if (mess.Contains("vietnamobile")) provider = "VietnamMobile";
                else provider = "Other";
                UpdateComData(sp.PortName, dto => dto.Telecom = provider, "Telecom");
                // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
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
        private void ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("ERROR"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.PhoneNumber = "Phone Unknow", "PhoneNumber");
                    gvCOM.RefreshRow(gvCOM.LocateByValue("COM", sp.PortName));
                }
                else if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    _ = SmsOrCallWithPrefix(sp);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
            }
        }

        /// <summary>
        /// Lấy ra thông tin tổng đài để thực hiện CALL hoặc SMS
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        private async Task SmsOrCallWithPrefix(SerialPort sp)
        {
            try
            {
                // Tin nhắn gửi về từ SMS
                var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                MessageCOMs[sp.PortName] = string.Empty;
                //logger.Info($"mess [{sp.PortName}]: {mess}");
                mess = mess.Substring(mess.IndexOf("+CUSD"));
                if (mess.Split(',').Length <= 0) return;
                var mess2 = mess.Split('\"')[1];
                Console.WriteLine(mess2);
                UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");
                // Lấy số điện thoại từ tin nhắn gửi về
                var phoneStr = mess2.Replace("\"", string.Empty);
                if (string.IsNullOrEmpty(phoneStr)) return;
                var phone = Common.GetPhoneNumber(phoneStr);
                if (!string.IsNullOrEmpty(phone))
                {
                    // Lấy thông tin tài khoản chính
                    int? tkchinh = Common.ExtractBalance(mess);
                    int currentTKC = (int)(tkchinh.HasValue ? tkchinh : 0);
                    // Cập nhật tài khoản chính và số điện thoại trên gridview
                    UpdateComData(sp.PortName, dto => { dto.PhoneNumber = phone; dto.TKChinh = currentTKC; }, "PhoneNumber", "TKChinh");
                    // Nếu trường hợp TKC nhỏ hơn mức min được burn thì dừng burn
                    int minAccount = int.Parse(txtMinAccountControl.Text.Replace(".", string.Empty));
                    if (currentTKC <= minAccount)
                    {
                        UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
                        return;
                    }
                    // Lấy thông tin nhà mạng
                    // Nếu không lấy được thông tin nhà mạng thì cũng dừng đốt để lấy lại thông tin qua *101#
                    var telecom = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName).Telecom;
                    if (telecom == null)
                    {
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.PhoneNumber = "Phone Unknow"; dto.Message = $"Stop burn"; dto.IsFinish = true;
                        }, "PhoneNumber", "Message", "IsFinish");
                        return;
                    }
                    // Gọi API để lấy ra đầu số call hoặc sms và các thông tin cần để xử lý
                    var prefixSmsReq = new GetPrefixSmsReq()
                    {
                        phone_number = phone,
                        amount = currentTKC,
                        amount_left = minAccount,
                        telecom = telecom
                    };
                    var prefixSmsRes = await _prefixController.GetPrefixNumber(prefixSmsReq);
                    if (prefixSmsRes == null)
                    {
                        // Nếu null nghĩa là dịch vụ đang full các đầu số, chờ 2 phút sau thử lại
                        UpdateComData(sp.PortName, dto => { dto.Message = $"Burning ..."; dto.IsFinish = false; }, "Message", "IsFinish");
                        // Thử lại sau 2 phút
                        Thread.Sleep(120000);
                        // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                        return;
                    }
                    //if (sp.PortName != "COM39") return;
                    UpdateComData(sp.PortName, dto => { dto.Message = "Burning ..."; dto.IsFinish = false; }, "Message", "IsFinish");
                    if (RecordingPorts.ContainsKey(sp.PortName))
                    {
                        RecordingPorts.TryRemove(sp.PortName, out _);
                        RecordingCOMs.TryRemove(sp.PortName, out _);
                    }
                    RecordingPorts.TryAdd(sp.PortName, new CallDetail()
                    {
                        call_duration = prefixSmsRes.duration, // 15000 là 15s giới thiệu của tổng đài
                        prefix = prefixSmsRes.prefix,
                        prefix_unit = prefixSmsRes.prefix_unit,
                        request_id = prefixSmsRes.request_id,
                        history_id = prefixSmsRes.history_id,
                        message = prefixSmsRes.message,
                        start_call = DateTime.Now,
                    });
                    if (prefixSmsRes.type == PrefixNumberType.CALL)
                    {
                        // Xóa tất cả file trong RAM - chủ yếu các file ghi âm
                        SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                        // Call
                        SendATCommand(sp, $"ATD{prefixSmsRes.prefix};", 1000);
                    }
                    else if (prefixSmsRes.type == PrefixNumberType.SMS)
                    {
                        // Send message
                        SendATCommand(sp, $"AT+CMGS=\"{prefixSmsRes.prefix}\"", 500);
                        SendATCommand(sp, $"{prefixSmsRes.message}{(char)26}", 500);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}]Burn thất bại: {ex.Message}");
                UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
            }
        }

        /// <summary>
        /// Xử lý khi có tin phản hồi từ tổng đài SMS (8x79)
        /// </summary>
        /// <param name="sp"></param>
        private async void ListenEventSendSms(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CMGS:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    if (!RecordingPorts.ContainsKey(sp.PortName)) return;
                    // Release Slot treen server
                    var callDetail = RecordingPorts[sp.PortName];
                    var releaseSlotReq = new ReleaseSlotReq()
                    {
                        history_id = callDetail.history_id.ToString(),
                        prefix = callDetail.prefix,
                        prefix_unit = callDetail.prefix_unit,
                        request_id = callDetail.request_id,
                        start_call = callDetail.start_call.ToString("yyyy-MM-dd HH:mm:ss"),
                        end_call = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        no_carrier = 0,
                    };
                    await _prefixController.ReleaseSlot(releaseSlotReq);
                    // xóa khỏi cổng COm đang lưu trữ
                    RecordingPorts.TryRemove(sp.PortName, out _);
                    // tạm dừng
                    int delay = new Random(Guid.NewGuid().GetHashCode()).Next(5000, 10001);
                    Thread.Sleep(delay);
                    // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
            }
        }

        /// <summary>
        /// Upload content đến thiết bị
        /// </summary>
        /// <param name="sp"></param>
        private void UploadFileToDevice(SerialPort sp)
        {
            var count = 0;
            while (count < 2)
            {
                try
                {
                }
                catch (Exception)
                {
                    count += 1;
                }
            }
            if (count >= 2)
            {
                SendATCommand(sp, "AT+CFUN=1,1", 10000);
                UploadFileToDevice(sp);
            }
        }

        /// <summary>
        /// Khi có file upload lên thiết bị
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventUploadAmrFileToDevice(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+QFOPEN:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;

                    string[] parts = mess.AT_Command("+QFOPEN").Split(':');
                    int fd = int.Parse(parts[2].Trim());
                    var fileToCOM = FileToCOMs[sp.PortName];
                    fileToCOM.fd = fd;
                    SendATCommand(sp, $"AT+QFWRITE={fd},{fileToCOM.data.Length},20\r\n");
                }
                else if (MessageCOMs[sp.PortName].Contains("+CME ERROR:") && MessageCOMs[sp.PortName].Contains("AT+QFOPEN="))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    SendATCommand(sp, "AT+CFUN=1,1", 10000);
                    var fileToCOM = FileToCOMs[sp.PortName];
                    sp.DiscardInBuffer();
                    SendATCommand(sp, $"AT+QFOPEN=\"RAM:content.amr\",0,{fileToCOM.data.Length}");
                }
                else if (MessageCOMs[sp.PortName].Contains("CONNECT") && MessageCOMs[sp.PortName].Contains("+QFWRITE"))
                {
                    //var mess = MessageCOMs[sp.PortName];
                    //MessageCOMs[sp.PortName] = string.Empty;
                    var fileToCOM = FileToCOMs[sp.PortName];
                    sp.Write(fileToCOM.data, 0, fileToCOM.data.Length);
                }
                else if (MessageCOMs[sp.PortName].Contains("CONNECT") && MessageCOMs[sp.PortName].Contains("+QFWRITE:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    var fileToCOM = FileToCOMs[sp.PortName];
                    SendATCommand(sp, $"AT+QFCLOSE={fileToCOM.fd}", 2000);
                    FileToCOMs.TryRemove(sp.PortName, out _);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ListenEventUploadAmrFileToDevice: {ex.Message}");
            }
        }

        /// <summary>
        /// Xử lý cuộc gọi đến tổng đài (tổng đài nhấc máy, ngắt máy,...)
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventCallPrefix(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CLCC:") && MessageCOMs[sp.PortName].Contains("0,2,0,0,"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    var callDetail = RecordingPorts[sp.PortName];

                    // Mở mic ghi âm
                    SendATCommand(sp, "AT+QAUDRD=1,\"RAM:record.amr\",3");
                    callDetail.start_record = DateTime.Now;

                    // TODO: Bấm phím 9
                    //SendATCommand(sp, "AT+VTS=9");

                    // TODO: Phát file đã ghi âm trước đó ở cổng COM

                    // Nghe nhạc chờ thêm một khoảng thời gian
                    // Tạo CancellationToken để có thể dừng khi có NO CARRIER
                    var cts = new CancellationTokenSource();
                    RecordingTokens[sp.PortName] = cts;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await Task.Delay(callDetail.call_duration * 1000, cts.Token);
                            StopRecording(sp, false); // Chủ động ngắt
                        }
                        catch (TaskCanceledException)
                        {
                            logger.Info($"[{sp.PortName}] - Ngắt kết nối chủ động (NO CARRIER).");
                        }
                    });
                }
                else if (MessageCOMs[sp.PortName].Contains("NO CARRIER") || MessageCOMs[sp.PortName].Contains("HANG UP"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    // Bị ngắt chủ động từ tổng đài
                    StopRecording(sp, true);
                    // Hủy delay nếu đang chờ
                    if (RecordingTokens.TryRemove(sp.PortName, out var token))
                    {
                        token.Cancel();
                        token.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ListenEventCallPrefix: {ex.Message}");
            }
        }

        /// <summary>
        /// Dừng ghi âm cuộc gọi
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="noCarrier">True: Tổng đài tự ngắt kết nối - False: Ngắt chủ động từ hệ thống</param>
        private void StopRecording(SerialPort sp, bool noCarrier)
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
                SendATCommand(sp, "ATH", 2000);
                // Dừng ghi âm
                SendATCommand(sp, "AT+QAUDRD=0", 1000);
                // Cập nhật thời gian dừng
                callDetail.end_record = DateTime.Now;
                // Tải xuống file âm thanh record.amr từ RAM của module.
                sp.WriteLine("AT+QFDWL=\"RAM:record.amr\"");
                Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - StopRecording Error: {ex.Message}");
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
                    // Xóa file ghi âm trên RAM
                    SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                    // Xóa khỏi danh sách cổng COM đang ghi âm
                    if (RecordingPorts.ContainsKey(sp.PortName)) RecordingPorts.TryRemove(sp.PortName, out _);
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
                string filePath = Path.Combine(recordDirectory, $"{phone}_{callDetail.prefix}_{DateTime.Now:yyyyMMddHHmmss}.amr");
                File.WriteAllBytes(filePath, audioData);

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
                await _prefixController.ReleaseSlot(releaseSlotReq, filePath);
                // Xóa file trong folder
                File.Delete(filePath);
                // Xóa file ghi âm trên RAM của GSM
                Thread.Sleep(1000);
                SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                // Xóa khỏi danh sách cổng COM đang ghi âm
                if (RecordingPorts.ContainsKey(sp.PortName)) RecordingPorts.TryRemove(sp.PortName, out _);
                // Xóa bỏ tin nhắn từ GSM trả về để làm luồng mới
                MessageCOMs[sp.PortName] = string.Empty;
                // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                if (!callDetail.no_carrier)
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                else
                    UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - SaveRecord Error: {ex.Message}");
            }
        }

        private void BtnStartBurn_Click(object sender, EventArgs e)
        {
            decimal minValue = Convert.ToDecimal(txtMinAccountControl.EditValue);
            Properties.Settings.Default.MinAccount = (int)minValue;
            Properties.Settings.Default.Save();
            var listStop = ComDataGrid.Where(dto => dto.IsFinish && !string.IsNullOrEmpty(dto.PhoneNumber)).ToList();
            foreach (var port in listStop)
            {
                var sp = SerialPorts.FirstOrDefault(dto => dto.PortName == port.COM);
                if (sp == null) continue;
                _ = Task.Run(() =>
                {
                    try
                    {
                        UpdateComData(sp.PortName, dto => dto.Message = "Burning ...", "Message");
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                    }
                });
            }
            XtraMessageBox.Show("Đã cập nhật giá trị số tiền cần để lại trên TK Chính", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    if (duplicateStt != null && duplicateStt.STT != "-1")
                    {
                        status = false;
                        MessageBox.Show($"Cảnh báo: STT {item.STT} bị trùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    newDataSource.Add(item);
                }
                if (status)
                {
                    var sortedData = new BindingList<ComDto>(newDataSource.OrderBy(x => int.Parse(x.STT)).ToList());
                    ComDataGrid.Clear();
                    List<string> deviceIDs = new List<string>();
                    foreach (var item in sortedData)
                    {
                        ComDataGrid.Add(item);
                        if (item.STT == "-1") continue;
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
                Properties.Settings.Default.COMs = string.Empty;
                Properties.Settings.Default.Save();
                foreach (var item in ComDataGrid)
                {
                    item.STT = "-1";
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
                    _ = Task.Run(() =>
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
                    });
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
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0,1");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 10000);
                            InitializeModem(sp);
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
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0,1");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
                            InitializeModem(sp);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void BtnDoanhThu_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var report = new Report(ApiKey);
            report.Show();
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
                        if (sp.IsOpen == true)
                        {
                            if (string.IsNullOrEmpty(item.PhoneNumber) || item.PhoneNumber == "Phone Unknow")
                            {
                                SendATCommand(sp, "AT+QSIMSTAT?");
                            }
                        }
                        else
                        {
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.ICCID = "COM ERROR";
                                dto.PhoneNumber = "COM ERROR";
                                dto.TKChinh = 0;
                                dto.Message101 = "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. This PC > Manager > Device Manager > Ports (COM & LPT)";
                                dto.Message = "COM ERROR";
                                dto.IsFinish = false;
                                dto.Telecom = "COM ERROR";
                            }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish", "Telecom");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                    }
                });
            }
        }

        private void GvCOM_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "Message")
            {
                var isFinish = gvCOM.GetRowCellValue(e.RowHandle, "Message").ToString();
                if (isFinish == "Stop burn")
                {
                    if (!view.IsRowSelected(e.RowHandle))
                        e.Appearance.ForeColor = Color.Red;
                }
                else
                {
                    if (!view.IsRowSelected(e.RowHandle))
                        e.Appearance.ForeColor = Color.Green;
                }
            }
        }

        private void BurnTKForm_Load(object sender, EventArgs e)
        {
            Text = $"Luck Burn - {Application.ProductVersion}";
        }

        private void BarBtnRule_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var termForm = new TermForm();
            termForm.ShowDialog();
        }

        private void GvCOM_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                GridView view = sender as GridView;
                GridHitInfo hitInfo = view.CalcHitInfo(e.Location);
                if (hitInfo.InRow || hitInfo.InRowCell)
                {
                    view.FocusedRowHandle = hitInfo.RowHandle;
                    popupMenu1.ShowPopup(MousePosition);
                }
            }
        }

        private void PopupResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
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
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0,1");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 10000);
                            InitializeModem(sp);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.Message101 = "Đổi IMEI cổng COM..."; dto.Message = "";
                            }, "Message101", "Message");
                            SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupBurn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.TKChinh = 0; dto.Message101 = ""; dto.Message = "Burning ...";
                            }, "TKChinh", "Message101", "Message");
                            SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupPhatSinhCuoc_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
        }

        private void BtnTotalRevenue_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var report = new ReportTotal(ApiKey);
            report.Show();
        }

        private void BtnDoiMatKhau_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var form = new ChangePassForm(ApiKey);
            form.ShowDialog();
        }

        private void BtnUserInfor_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var form = new UserInforForm(ApiKey);
            form.ShowDialog();
        }

        private void TimerNotification_Tick(object sender, EventArgs e)
        {
            LoadNotification();
        }

        private IEnumerable<Dictionary<string, string>> CheckComOnline()
        {
            var fullPortNames = Common.GetFullPortNames();
            var countComsOnline = fullPortNames.Count();
            BarPCCom.Caption = $"PC COMs Online: <b><size=12><color=green>{countComsOnline}</color></size></b>";
            if (countComsOnline == 0)
            {
                TimerCheckSim.Enabled = false;
                XtraMessageBox.Show("Toàn bộ cổng COM bị lỗi. Đảm bảo các cổng COM không có dấu chấm than.\nThis PC > Manager > Device Manager > Ports (COM & LPT)", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
                return null;
            }
            else return fullPortNames;
        }

        private async void LoadNotification()
        {
            var notificaton = await _prefixController.GetNotification();
            TxtNotification.Caption = notificaton;
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
                logger.Error($"SendATCommand: {ex.Message}");
            }
        }

        #region Hướng dẫn sử dụng cho người dùng

        private void HelpUI_QueryGuideFlyoutControl(object sender, DevExpress.Utils.VisualEffects.QueryGuideFlyoutControlEventArgs e)
        {
            e.Control = panel;
        }

        private void BarBtnHDSD_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            HelpUI.ShowGuides = DevExpress.Utils.DefaultBoolean.True;
            SetLesson(panel.CurrentLessonIndex);
        }

        public void SetLesson(int index)
        {
            if (index < 0 || index > countLessons - 1) return;
            switch (index)
            {
                case 0:
                    FirstLesson(); break;
                case 1:
                    SecondLesson(); break;
                case 2:
                    ThirdLesson(); break;
                case 3:
                    FourthLesson(); break;
                case 4:
                    FifthLesson(); break;
            }
        }

        public void EndTutorial()
        {
            HelpUI.ShowGuides = DevExpress.Utils.DefaultBoolean.False;
        }

        private void FirstLesson()
        {
            panel.LabelText = $"<b><size=10>Hạn mức nhỏ nhất</size></b><br><br>Giữ lại số tiền tương ứng trong tài khoản chính của thuê bao. Đảm bảo số dư tài khoản chính không nhỏ hơn hạn mức giữ lại</color>.";
            guide1.TargetElement = txtMinAccountControl;
        }

        private void SecondLesson()
        {
            panel.LabelText = $"<b><size=10>Nút Burn</size></b><br><br>Khởi động Burn cho tất cả các thuê bao có trạng thái <color=red>\"Stop Burn\"</color>.";
            guide1.TargetElement = BtnStartBurn;
        }

        private void ThirdLesson()
        {
            panel.LabelText = $"<b><size=10>Danh sách cổng COMs</size></b>" +
                $"<br><br>Hiển thị thông tin thuê bao trên từng cổng COM.\n" +
                $"Nếu các cột hiển thị <color=red>\"COM ERROR\"</color>, kiểm tra kết nối của các cổng COM với PC\n" +
                $"<b>This PC > Manager > Device Manager > Ports (COM & LPT)</b>\n" +
                $"Nếu hiển thị màu vàng với dấu chấm than thì hãy rút cổng COM trên PC ra và cắm lại, sau đó khởi động lại phần mềm.";
            guide1.TargetElement = gcCOM;
        }

        private void FourthLesson()
        {
            panel.LabelText = $"<b><size=10>Thông báo</size></b><br><br>Hiển thị thông báo của hệ thống, tin khuyến mãi, phiên bản cập nhật...";
            guide1.TargetElement = TxtNotification;
        }

        private void FifthLesson()
        {
            panel.LabelText = $"<b><size=10>PC COMs Online</size></b><br><br>Hiển thị số cổng COM đang kết nối với hệ thống. Nếu số cổng COM ít hơn ở danh sách, hãy kiểm tra lại kết nối của PC với cổng COM.";
            guide1.TargetElement = BarPCCom;
        }

        #endregion Hướng dẫn sử dụng cho người dùng
    }
}