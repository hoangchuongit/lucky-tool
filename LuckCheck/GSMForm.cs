using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using LuckCheck.Model;
using LuckCheck.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Input;

namespace LuckCheck
{
    public partial class GSMForm : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        public GSMForm()
        {
            InitializeComponent();
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
            //Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);
            //logger.Info(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);

            // Có cuộc gọi đến
            ListenEventCallResponse(sp);

            // Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
            ListenEventSIMStatus(sp);

            // Lắng nghe để lấy số serial SIM
            ListenEventICCID(sp);

            // Lắng nghe để lấy thông tin Số điện thoại
            ListenEventPhoneNumber(sp);

            // Lắng nghe change IMEI thành công
            ListenEventChangeIMEI(sp);

            // Lắng nghe phản hồi của tin nhắn SMS
            ListenEventSmsResponse(sp);
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
                dto.HSD = string.Empty;
                dto.TKChinh = 0;
                dto.Message101 = "";
            }, "PhoneNumber", "HSD", "TKChinh", "Message101");
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
                MessageCOMs[sp.PortName] = string.Empty;
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                try
                {
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.ICCID = string.Empty;
                        dto.PhoneNumber = string.Empty;
                        dto.HSD = string.Empty;
                        dto.TKChinh = 0;
                        dto.Message101 = string.Empty;
                    }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
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
                    //Đặt module về chế độ Text Mode(ASCII)
                    SendATCommand(sp, "AT+CMGF=1");
                    // Nhận tin nhắn dưới dạng văn bản
                    SendATCommand(sp, "AT+CNMI=2,2,0,1,0");
                    Thread.Sleep(500);
                    MessageCOMs[sp.PortName] = string.Empty;
                    // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15", 0);
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
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
                if (content.Contains("+CUSD:") && content.Contains("\nOK"))
                {
                    // Tin nhắn gửi về từ SMS
                    var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                    MessageCOMs[sp.PortName] = string.Empty;
                    //logger.Info($"mess [{sp.PortName}]: {mess}");
                    mess = mess.Substring(mess.IndexOf("+CUSD")).ToLower().Replace("du lieu", " du lieu ");
                    if (mess.Split(',').Length <= 0 && mess.Split('\"').Length <= 1) return;
                    // Lấy đầy đủ thông tin tin nhắn đến
                    var mess2 = mess.Split('\"')[1];
                    //var mess2 = "xin chao 09285945821. goi cuoc thoai & sms2. goi cuoc data3. dv de lai cuoc goi nho";
                    UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");
                    // Lấy số điện thoại từ tin nhắn gửi về
                    var phoneStr = mess2.Replace("\"", string.Empty).Replace("1. goi", " ");
                    if (string.IsNullOrEmpty(phoneStr)) return;
                    var phone = Common.GetPhoneNumber(phoneStr);
                    if (string.IsNullOrEmpty(phone)) return;
                    // Lấy thông tin tài khoản chính
                    int? tkchinh = Common.ExtractBalance(mess);
                    // Lấy thông tin hạn sử dụng
                    string hsd = Common.ExtractHanSD(mess);
                    // Cập nhật tài khoản chính và số điện thoại trên gridview
                    int currentTKC = (int)(tkchinh.HasValue ? tkchinh : 0);
                    UpdateComData(sp.PortName, dto => { dto.PhoneNumber = phone; dto.HSD = hsd; dto.TKChinh = currentTKC; }, "PhoneNumber", "HSD", "TKChinh");
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
        /// Xử lý khi có tin đến
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSmsResponse(SerialPort sp)
        {
            try
            {
                var content = MessageCOMs[sp.PortName];
                if (content.Contains("+CMT:"))
                {
                    var message = MessageCOMs[sp.PortName].AT_Command();
                    //logger.Info($"[{sp.PortName}] - MessageAll: {message}");
                    int startIndex = message.IndexOf("+CMT");
                    if (startIndex == -1) return;
                    var messSplit = message.Substring(startIndex).Split(',');
                    if (messSplit.Length >= 3)
                    {
                        var messContent = string.Join(",", messSplit.Skip(2)).Split('"');
                        // Nếu chuỗi chia theo " không có độ dài lớn hơn 3 nghĩa là chưa đến tin nhắn chính
                        if (messContent.Length >= 3 && !string.IsNullOrEmpty(messContent[2]))
                        {
                            var brandName = messSplit[0].Replace("+CMT: \"", "").Replace("\"", "").Trim();
                            var messData = messContent[2];
                            var checkUTF16 = Common.IsValidUtf16(messData);
                            if (checkUTF16) messData = Common.DecodeUnicode(messData);
                            MessageCOMs[sp.PortName] = string.Empty;
                            //logger.Info($"[{sp.PortName}] - MessageAll: {messData}");
                            // Lấy thông tin hạn sử dụng
                            string hsd = Common.ExtractHanSD(messData);
                            // Hiển thị tin nhắn trong Message
                            if(string.IsNullOrEmpty(hsd))
                                UpdateComData(sp.PortName, dto => dto.Message101 = messData.ToString(), "Message101");
                            else 
                                UpdateComData(sp.PortName, dto => { dto.Message101 = messData.ToString(); dto.HSD = hsd; }, "Message101", "HSD");
                        }
                    }
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => { dto.Message101 = ""; }, "Message101", "IsFinish");
            }
        }

        /// <summary>
        /// Xử lý khi có cuộc gọi
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventCallResponse(SerialPort sp)
        {
            try
            {
                var message = MessageCOMs[sp.PortName];
                if (message.Contains("RING") && message.Contains("+CLCC:") && message.Contains("1,4,0,0,"))
                {
                    //logger.Info($"[{sp.PortName}] - MessageAll: {message}");
                    var phoneCall = Common.ExtractValidPhoneNumberCall(message);
                    if (!string.IsNullOrEmpty(phoneCall))
                    {
                        //logger.Info($"[{sp.PortName}] - MessageAll: {message}");
                        SendATCommand(sp, "ATH", 5000);
                        MessageCOMs[sp.PortName] = string.Empty;
                        // Hiển thị tin nhắn trong Message
                        UpdateComData(sp.PortName, dto => dto.Message101 = $"Số điện thoại gọi đến: {phoneCall}", "Message101");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - ListenEventCallResponse Error: {ex.Message}");
                UpdateComData(sp.PortName, dto => dto.Message101 = $"Lỗi: Nhận cuộc gọi thất bại: {ex.Message}", "Message");
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
                    foreach (var item in sortedData)
                    {
                        ComDataGrid.Add(item);
                    }
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
                            UpdateComData(sp.PortName, dto => dto.Message101 = "Đổi IMEI cổng COM...", "Message101");
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
                                dto.HSD = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Reset cổng COM";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
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
                                dto.HSD = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Khôi phục cài đặt gốc cổng COM";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
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
                            dto.HSD = "COM ERROR";
                            dto.TKChinh = 0;
                            dto.Message101 = "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. This PC > Manager > Device Manager > Ports (COM & LPT)";
                        }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                    }
                });
            }
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

        private void Popup101_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                                dto.TKChinh = 0; dto.Message101 = "";
                            }, "TKChinh", "Message101");
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
                                dto.HSD = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Reset cổng COM";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
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
                            UpdateComData(sp.PortName, dto => dto.Message101 = "Đổi IMEI cổng COM...", "Message101");
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