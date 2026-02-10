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

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Danh sách cổng COM đang gửi SMS
        private readonly ConcurrentDictionary<string, TranferMoneySMSPort> SMSPorts = new ConcurrentDictionary<string, TranferMoneySMSPort>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        private readonly PrefixNumberController _prefixController;

        public BurnTKForm(string apikey)
        {
            InitializeComponent();
            _prefixController = new PrefixNumberController(apikey);
            InitializeControls();
        }

        private void InitializeControls()
        {
            LoadNotification();
            txtMinAccountControl.EditValue = Properties.Settings.Default.MinAccount;
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
                var regexPattern = $@"\b{Regex.Escape(port)}\b";
                var isValid = fullPortNames.FirstOrDefault(x => Regex.IsMatch(x["Caption"], regexPattern, RegexOptions.IgnoreCase));
                if (isValid == null) continue;
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
                // Khởi động lại modem mà không thay đổi các cài đặt, chỉ tái thiết lập kết nối hoặc trạng thái của modem.
                SendATCommand(sp, "ATZ");
                // Đưa Baudrate về tốc độ  115200
                SendATCommand(sp, "AT+IPR=115200");
                // Đặt mã ký tự về ASCII
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                SendATCommand(sp, "AT+QSIMDET=1,0");
                // Kích hoạt chế độ thông báo sự kiện SIM
                SendATCommand(sp, "AT+QSIMSTAT=1");
                // bật Presentation of Calling Line (điều chỉnh trạng thái caller).
                SendATCommand(sp, "AT+COLP=1");
                // bật báo trạng thái hiện tại của cuộc gọi.
                SendATCommand(sp, "AT+CLCC=1");
                // cấu hình để modem báo các mã lỗi cuộc gọi như BUSY, NO CARRIER, v.v.
                SendATCommand(sp, "ATX3");
                // Lưu thay đổi
                SendATCommand(sp, "AT&W");
                // Modem
                //SendATCommand(sp, "ATI");
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
            try
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
            //if (sp.PortName != "COM83") return;
            //Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);
            //logger.Info(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);


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

            // Xử lý trường hợp upload file amr tới thiết bị
            //ListenEventUploadAmrFileToDevice(sp);

            // Lắng nghe phản hồi của tin nhắn SMS
            ListenEventSmsResponse(sp);

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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
                dto.PhoneNumber = string.Empty;
                dto.TKChinh = 0;
                dto.HSD = string.Empty;
                dto.Message101 = "";
                dto.Message = "";
                dto.IsFinish = true;
            }, "PhoneNumber", "TKChinh", "HSD", "Message101", "Message", "IsFinish");
        }

        /// <summary>
        /// Khi lấy được status của SIM
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            var content = MessageCOMs[sp.PortName];
            // Trạng thái SIM đã tháo
            if (
                (
                    (content.Contains("+CPIN: NOT INSERTED") || content.Contains("+CPIN: NOT READY")) && content.Contains("+QSIMSTAT: 1,0")
                )
                || (content == "\0")
            )
            {
                // Xóa các tin nhắn cũ đi
                SendATCommand(sp, $"AT+CMGD=2,4");
                MessageCOMs[sp.PortName] = string.Empty;
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                try
                {
                    // loại bỏ dữ liệu sms cũ của cổng COM
                    if (SMSPorts.ContainsKey(sp.PortName)) SMSPorts.TryRemove(sp.PortName, out _);
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.ICCID = string.Empty;
                        dto.PhoneNumber = string.Empty;
                        dto.TKChinh = 0;
                        dto.HSD = string.Empty;
                        dto.Message101 = string.Empty;
                        dto.Message = string.Empty;
                        dto.IsFinish = false;
                        dto.Telecom = string.Empty;
                    }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message", "IsFinish", "Telecom");
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
                                                .Replace("AT+QCCID", "").Replace("+QCCID: ", "").Replace("+QUSIM: 1", "").Substring(0, 20),
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
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15", 3000);
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
        private void ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("+CUSD:") && content.Contains("\nOK")) _ = SmsOrCallWithPrefix(sp);
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
        /// Lấy ra thông tin tổng đài để thực hiện CALL hoặc SMS
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
        private async Task SmsOrCallWithPrefix(SerialPort sp)
        {
            // Lấy số tiền min để lại trên tài khoản
            int minAccount = (int)Convert.ToDecimal(txtMinAccountControl.EditValue);
            int currentTKC = 0;
            try
            {
                // Tin nhắn gửi về từ SMS
                var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                MessageCOMs[sp.PortName] = string.Empty;
                //logger.Info($"mess [{sp.PortName}]: {mess}");
                mess = mess.Substring(mess.IndexOf("+CUSD")).ToLower().Replace("du lieu", " du lieu ");
                if (mess.Split(',').Length <= 0 && mess.Split('\"').Length <= 1) return;
                var mess2 = mess.Split('\"')[1];
                UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");
                // Lấy số điện thoại từ tin nhắn gửi về
                var phoneStr = mess2.Replace("\"", string.Empty).Replace("1. goi", " ");
                if (string.IsNullOrEmpty(phoneStr)) return;
                var phone = Common.GetPhoneNumber(phoneStr);
                if (string.IsNullOrEmpty(phone)) return;
                // Lấy thông tin tài khoản chính
                var oldTKC = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName)?.TKChinh ?? 0;
                int? tkchinh = Common.ExtractBalance(mess);
                // Lấy thông tin hạn sử dụng
                string hsd = Common.ExtractHanSD(mess);
                currentTKC = (int)(tkchinh.HasValue ? tkchinh : 0);
                //if (currentTKC == 0 || currentTKC == oldTKC)
                //{
                //    UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
                //    return;
                //}
                // Cập nhật tài khoản chính và số điện thoại trên gridview
                UpdateComData(sp.PortName, dto =>
                {
                    dto.PhoneNumber = phone; dto.TKChinh = currentTKC; dto.HSD = hsd; dto.Message = "Burning ..."; dto.IsFinish = false;
                }, "PhoneNumber", "TKChinh", "HSD", "Message", "IsFinish");
                // Lấy thông tin nhà mạng
                var telecom = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName).Telecom ?? "Other";
                // Nếu trường hợp TKC nhỏ hơn mức min được burn thì dừng burn
                if (currentTKC <= minAccount)
                {
                    UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
                    return;
                }
                // loại bỏ dữ liệu sms cũ của cổng COM
                if (SMSPorts.ContainsKey(sp.PortName)) SMSPorts.TryRemove(sp.PortName, out _);

                //if (sp.PortName != "COM273") return;

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
                    // Nếu null nghĩa là dịch vụ đang full các đầu số, chờ 55s sau thử lại
                    Thread.Sleep(55000);
                    // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    return;
                }
                if (prefixSmsRes.type == PrefixNumberType.SMS)
                {
                    SMSPorts.TryAdd(sp.PortName, new TranferMoneySMSPort()
                    {
                        prefix = prefixSmsRes.prefix,
                        prefix_unit = prefixSmsRes.prefix_unit,
                        request_id = prefixSmsRes.request_id,
                        history_id = prefixSmsRes.history_id.ToString(),
                        start_time = DateTime.Now
                    });
                    //Send message
                    var prefix = prefixSmsRes.prefix;
                    if (prefix.IndexOf("9029_NAP") > -1)
                        prefix = "9029";
                    sp.Write($"AT+CMGS=\"{prefix}\"\r");
                    Thread.Sleep(800);
                    sp.Write($"{prefixSmsRes.message}{(char)26}");
                    Thread.Sleep(500);
                    sp.Write(new byte[] { 0x1A }, 0, 1);
                    Thread.Sleep(500);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}]Burn thất bại: {ex.Message}");
                if (currentTKC == 0 || currentTKC <= minAccount)
                {
                    UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
                }
                else
                {
                    UpdateComData(sp.PortName, dto => { dto.Message = $"Restart burn after 30s"; }, "Message");
                    Thread.Sleep(30000);
                    // Tiếp tục đốt
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
            }
        }

        // <summary>
        /// Xử lý khi có tin phản hồi từ tổng đài SMS (8x79, 7x39)
        /// </summary>
        /// <param name="sp"></param>
        private async void ListenEventSmsResponse(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("+CMT: \"7539\"") || content.Contains("+CMT: \"+7539\""))
                {
                    if (content.Contains("Da kich hoat so dien thoai thanh cong"))
                    {
                        MessageCOMs[sp.PortName] = string.Empty;
                        if (SMSPorts.ContainsKey(sp.PortName))
                        {
                            // Release Slot treen server
                            var smsDetail = SMSPorts[sp.PortName];
                            var releaseSlotReq = new SmsReq()
                            {
                                history_id = smsDetail.history_id.ToString(),
                                prefix = smsDetail.prefix,
                                prefix_unit = smsDetail.prefix_unit,
                                request_id = smsDetail.request_id,
                                status = StatusEnum.SUCCESS.ToString(),
                            };
                            await _prefixController.UpdateSms(releaseSlotReq);
                        }
                        SMSPorts.TryRemove(sp.PortName, out _);
                        // Dừng 45s
                        Thread.Sleep(45000);
                        // Tiếp tục đốt
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    }
                    else if (content.Contains("Khong the su dung so dien thoai nay"))
                    {
                        MessageCOMs[sp.PortName] = string.Empty;
                        if (SMSPorts.ContainsKey(sp.PortName))
                        {
                            // Release Slot treen server
                            var smsDetail = SMSPorts[sp.PortName];
                            var releaseSlotReq = new SmsReq()
                            {
                                history_id = smsDetail.history_id.ToString(),
                                prefix = smsDetail.prefix,
                                prefix_unit = smsDetail.prefix_unit,
                                request_id = smsDetail.request_id,
                                status = StatusEnum.FAIL.ToString(),
                            };
                            await _prefixController.UpdateSms(releaseSlotReq);
                        }
                        SMSPorts.TryRemove(sp.PortName, out _);
                        // Dừng 45s
                        Thread.Sleep(45000);
                        // Tiếp tục đốt
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    }
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
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
                var thread = new Thread(() =>
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
                })
                {
                    IsBackground = true
                };
                thread.Start();
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
                    ComDataGrid.Clear();
                    var sortedData = new BindingList<ComDto>(newDataSource.OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1).ToList());
                    var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
                    if (File.Exists(configPath)) File.Delete(configPath);
                    List<ComConfig> list = new List<ComConfig>();
                    foreach (var item in sortedData)
                    {
                        ComDataGrid.Add(item);
                        if (item.STT == "") continue;
                        list.Add(new ComConfig { PortName = item.COM, STT = item.STT });
                    }
                    ComConfigManager.Save(list);
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
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn đặt lại STT cổng COM?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
                if (File.Exists(configPath)) File.Delete(configPath);
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
                                dto.HSD = string.Empty;
                                dto.Message101 = "Reset cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 10000);
                            // Đưa Baudrate về tốc độ  115200
                            SendATCommand(sp, "AT+IPR=115200");
                            // Đảm bảo các URC như RING, +CLIP, +CPIN, SIM hot-swap... được gửi qua UART chính thay vì qua USB AT port.
                            SendATCommand(sp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Đặt mã ký tự về ASCII
                            SendATCommand(sp, "AT+CSCS=\"GSM\"");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=1");
                            // bật Presentation of Calling Line (điều chỉnh trạng thái caller).
                            SendATCommand(sp, "AT+COLP=1");
                            // bật báo trạng thái hiện tại của cuộc gọi.
                            SendATCommand(sp, "AT+CLCC=1");
                            // cấu hình để modem báo các mã lỗi cuộc gọi như BUSY, NO CARRIER, v.v.
                            SendATCommand(sp, "ATX3");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
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
                                dto.HSD = string.Empty;
                                dto.Message101 = "Khôi phục cài đặt gốc cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                            // Khôi phục các cài đặt AT command về cấu hình nhà sản xuất
                            SendATCommand(sp, "AT&F", 60000);
                            // Đưa Baudrate về tốc độ  115200
                            SendATCommand(sp, "AT+IPR=115200");
                            // Đảm bảo các URC như RING, +CLIP, +CPIN, SIM hot-swap... được gửi qua UART chính thay vì qua USB AT port.
                            SendATCommand(sp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Đặt mã ký tự về ASCII
                            SendATCommand(sp, "AT+CSCS=\"GSM\"");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=1");
                            // bật Presentation of Calling Line (điều chỉnh trạng thái caller).
                            SendATCommand(sp, "AT+COLP=1");
                            // bật báo trạng thái hiện tại của cuộc gọi.
                            SendATCommand(sp, "AT+CLCC=1");
                            // cấu hình để modem báo các mã lỗi cuộc gọi như BUSY, NO CARRIER, v.v.
                            SendATCommand(sp, "ATX3");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
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

                _ = Task.Run(async () =>
                {
                    try
                    {
                        if (!sp.IsOpen) sp.Open();
                        if (string.IsNullOrEmpty(item.PhoneNumber) || item.Message101.Trim().ToLower().Equals("your input is error or system busy,pls try again!"))
                        {
                            sp.DiscardInBuffer();
                            sp.DiscardOutBuffer();
                            SendATCommand(sp, "AT+QCCID");
                            return;
                        }
                        var sms = SMSPorts.TryGetValue(sp.PortName, out var c) ? c : null;
                        if (sms == null) return;
                        bool timeout = DateTime.Now - sms.start_time > TimeSpan.FromSeconds(45);

                        if (!timeout) return;

                        sp.DiscardInBuffer();
                        sp.DiscardOutBuffer();

                        SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.ICCID = "COM ERROR";
                            dto.PhoneNumber = "COM ERROR";
                            dto.TKChinh = 0;
                            dto.HSD = "COM ERROR";
                            dto.Message101 = "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. This PC > Manager > Device Manager > Ports (COM & LPT)";
                            dto.Message = "COM ERROR";
                            dto.IsFinish = true;
                            dto.Telecom = "COM ERROR";
                        }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message", "IsFinish", "Telecom");
                    }
                });
            }
        }

        private void GvCOM_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "Message")
            {
                var isFinish = gvCOM.GetRowCellValue(e.RowHandle, "Message")?.ToString();
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
            Text = $"Luck 7539 - {Application.ProductVersion}";
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
                                dto.HSD = string.Empty;
                                dto.Message101 = "Reset cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 10000);
                            // Đưa Baudrate về tốc độ  115200
                            SendATCommand(sp, "AT+IPR=115200");
                            // Đảm bảo các URC như RING, +CLIP, +CPIN, SIM hot-swap... được gửi qua UART chính thay vì qua USB AT port.
                            SendATCommand(sp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Đặt mã ký tự về ASCII
                            SendATCommand(sp, "AT+CSCS=\"GSM\"");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(sp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(sp, "AT+QSIMSTAT=1");
                            // bật Presentation of Calling Line (điều chỉnh trạng thái caller).
                            SendATCommand(sp, "AT+COLP=1");
                            // bật báo trạng thái hiện tại của cuộc gọi.
                            SendATCommand(sp, "AT+CLCC=1");
                            // cấu hình để modem báo các mã lỗi cuộc gọi như BUSY, NO CARRIER, v.v.
                            SendATCommand(sp, "ATX3");
                            // Lưu thay đổi
                            SendATCommand(sp, "AT&W");
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
                            if (!sp.IsOpen) sp.Open();
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.TKChinh = 0; dto.Message101 = ""; dto.Message = string.Empty;
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

        private void TimerNotification_Tick(object sender, EventArgs e)
        {
            LoadNotification();
        }

        //private void AppendLogToMemo(string port, string message)
        //{
        //    if (MemoLog.InvokeRequired)
        //    {
        //        MemoLog.BeginInvoke(new Action(() =>
        //        {
        //            MemoLog.AppendText($"{port}: {message}\r\n");
        //        }));
        //    }
        //    else
        //    {
        //        MemoLog.AppendText($"{port}: {message}\r\n");
        //    }
        //}

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
                logger.Error($"[{sp.PortName}] SendATCommand: {ex.Message}");
            }
        }

    }
}