using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using LuckBurn.Model;
using LuckBurnTK.Controllers;
using LuckBurnTK.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace LuckBurnTK
{
    public partial class BurnTKForm : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        private readonly DBController _dbController;

        private readonly string AccountId;

        public BurnTKForm(string accountId)
        {
            InitializeComponent();
            AccountId = accountId;
            _dbController = new DBController();
            InitializeControls();
        }

        private void InitializeControls()
        {
            txtMinAccountControl.EditValue = Properties.Settings.Default.MinAccount;
            LoadCOMForm();
        }

        private void LoadCOMForm()
        {
            string[] portNames = SerialPort.GetPortNames();
            var fullPortNames = Common.GetFullPortNames();
            InitializeSerialPorts(portNames, fullPortNames);
            gcCOM.DataSource = ComDataGrid;
            foreach (var port in SerialPorts)
            {
                var thread = new Thread(() => InitializeModem(port)) { IsBackground = true };
                thread.Start();
            }
        }

        private void InitializeSerialPorts(string[] portNames, IEnumerable<Dictionary<string, string>> fullPortNames)
        {
            var dataCOms = Properties.Settings.Default.COMs.Trim().Split(',');
            foreach (string port in portNames)
            {
                //if (port != "COM33") return;
                var regexPattern = $@"\b{Regex.Escape(port)}\b";
                var isValid = fullPortNames.FirstOrDefault(x => Regex.IsMatch(x["Caption"], regexPattern, RegexOptions.IgnoreCase) && x["Caption"].Contains("XR21V1414"));
                if (isValid == null) continue;
                var deviceID = isValid["DeviceID"].ToString().Split('\\')[2].ToString();
                SerialPort sp = new SerialPort(port)
                {
                    BaudRate = 115200,
                    Parity = Parity.None,
                    StopBits = StopBits.One,
                    DataBits = 8,
                    Handshake = Handshake.None,
                    RtsEnable = true,
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
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
                // Hủy chuyển hướng cuộc gọi của sim
                //SendATCommand(sp, "AT+CCFC=0,0");
                //sp.WriteLine("AT+CCFC=0,0\n");
                // Xóa tất cả file trong RAM - chủ yếu các file ghi âm
                //SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                // Kiểm tra cổng COM đã cắm SIM hay chưa?
                //SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Error InitializeModem: {ex.Message}", "Message");
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
            //Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);

            if (MessageCOMs[sp.PortName].Contains("RING"))
            {
                SendATCommand(sp, "ATH");
                MessageCOMs[sp.PortName] = string.Empty;
            }

            if (MessageCOMs[sp.PortName].Contains("CMS ERROR"))
            {
                UpdateComData(sp.PortName, dto =>
                {
                    dto.Message = "Gửi SMS thất bại. Sim có thể bị khóa chiều gửi sms, kiểm tra lại sim. Dừng đốt";
                    dto.SmsId = Guid.Empty;
                    dto.IsFinish = true;
                }, "Message", "SmsId");
            }

            // Nhận SMS tin nhắn văn bản
            //if (MessageCOMs[sp.PortName].Contains("+CMT:"))
            //{
            //    var message = MessageCOMs[sp.PortName].AT_Command();
            //    Sms_Received(sp, message);
            //}

            // Lắng nghe xem SIM có được cắm vào cổng COM hay không
            ListenEventSIMInsert(sp);

            // Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
            ListenEventSIMStatus(sp);

            // Lắng nghe để lấy số serial SIM
            ListenEventICCID(sp);

            // Lắng nghe để lấy thông tin Số điện thoại
            ListenEventPhoneNumber(sp);

            // Lắng nghe change IMEI thành công
            ListenEventChangeIMEI(sp);

            // Lắng nghe xem sim gửi SMS thành công chưa
            ListenEventSendSms(sp);

            // Trigger: Trạng thái SIM đã bị tháo
            if (MessageCOMs[sp.PortName].Contains("+CPIN: NOT READY"))
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
                }, "ICCID", "PhoneNumber", "TKChinh", "Message101", "Message", "Tele");
                //gvCOM.RefreshRow(gvCOM.LocateByValue("COM", sp.PortName));
            }

            // Trigger: Trạng thái SIM đã cắm
            if (MessageCOMs[sp.PortName].Contains("Call Ready") && MessageCOMs[sp.PortName].Contains("+CPIN: READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;

                UpdateComData(sp.PortName, dto => dto.Message = $"Bắt đầu nạp dữ liệu cổng COM...", "Message");
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"Error on port {sp.PortName}: {e.EventType}");
            MessageCOMs[sp.PortName] = string.Empty;
            UpdateComData(sp.PortName, dto => dto.PhoneNumber = "PHONE NOT FOUND", "PhoneNumber");
        }

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
                    SendATCommand(sp, "AT+CPIN?");
                }
                else
                    MessageCOMs[sp.PortName] = string.Empty;
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.PhoneNumber = "PHONE NOT FOUND", "PhoneNumber");
            }
        }

        private void ListenEventSIMStatus(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+CPIN:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command();
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
                if (mess.Contains("+CPIN: READY"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.Message = "SIM đã sẵn sàng.", "Message");
                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message = string.Empty, "Message");
            }
        }

        private void ListenEventICCID(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+QCCID") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+QCCID");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.ICCID = mess.Substring(0, 19), "ICCID");
                // Gửi AT lấy số điện thoại và gửi SMS
                SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        private void ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("ERROR"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.PhoneNumber = "PHONE NOT FOUND", "PhoneNumber");
                    gvCOM.RefreshRow(gvCOM.LocateByValue("COM", sp.PortName));
                }
                else if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    GetPhoneAndSendSMS(sp);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
                UpdateComData(sp.PortName, dto => dto.Message = "Lấy số điện thoại thất bại", "Message");
            }
        }

        private void GetPhoneAndSendSMS(SerialPort sp)
        {
            try
            {
                var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                mess = mess.Substring(mess.IndexOf("+CUSD"));
                if (mess.Split(',').Length <= 0) return;
                var mess2 = mess.Split('\"')[1];
                var phoneStr = mess2.Replace("\"", string.Empty);
                if (string.IsNullOrEmpty(phoneStr)) return;
                var phone = Common.GetPhoneNumber(phoneStr);
                if (string.IsNullOrEmpty(phone))
                {
                    UpdateComData(sp.PortName, dto => dto.Message = $"Hệ thống tổng đài chưa kịp phản hồi. Chờ 3-5 s trước khi tiếp tục...", "Message");
                    // tạm dừng
                    int delay = new Random(Guid.NewGuid().GetHashCode()).Next(3000, 5001);
                    Thread.Sleep(delay);
                    // Gửi AT lấy số điện thoại và gửi SMS
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
                else
                {
                    int? tkchinh = Common.ExtractBalance(mess);
                    MessageCOMs[sp.PortName] = string.Empty;

                    int currentTKC = (int)(tkchinh.HasValue ? tkchinh : 0);
                    int minAccount = int.Parse(txtMinAccountControl.Text.Replace(".", string.Empty));

                    if (currentTKC <= minAccount)
                    {
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.PhoneNumber = phone;
                            dto.TKChinh = currentTKC;
                            dto.Message101 = mess2;
                            dto.Message = $"Số dư không đủ để tiếp tục. Dừng đốt TKC.";
                            dto.IsFinish = true;
                        }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
                    }
                    else
                    {
                        // Gọi phương thức GetPrefixAndMessage của DBController
                        var (prefix, message, amountValue) = _dbController.GetPrefixAndMessage(phone, currentTKC, minAccount, Guid.Parse(AccountId));
                        if (string.IsNullOrEmpty(prefix))
                        {
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.PhoneNumber = phone;
                                dto.TKChinh = currentTKC;
                                dto.Message101 = mess2;
                                dto.Message = $"Số dư không đủ để tiếp tục. Dừng đốt TKC.";
                                dto.IsFinish = true;
                            }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
                        }
                        else
                        {
                            SendATCommand(sp, $"AT+CMGS=\"{prefix}\"", 500);
                            SendATCommand(sp, $"{message}{(char)26}", 500);

                            var item = ComDataGrid.FirstOrDefault(dto => dto.COM == sp.PortName);
                            var smsId = _dbController.InsertSMSHistory(item.ICCID, phone, prefix, message, amountValue, Guid.Parse(AccountId));

                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.PhoneNumber = phone;
                                dto.TKChinh = currentTKC;
                                dto.Message101 = phoneStr;
                                dto.Message = "Gửi SMS ...";
                                dto.IsFinish = false;
                                dto.SmsId = smsId;
                            }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại và gửi SMS thất bại: {ex.Message}");
                UpdateComData(sp.PortName, dto => dto.Message = "Lấy số điện thoại và gửi SMS thất bại", "Message");
            }
        }

        private void ListenEventSendSms(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CMGS:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    int rowHandle = gvCOM.LocateByValue("COM", sp.PortName);
                    if (rowHandle < 0)
                    {
                        MessageCOMs[sp.PortName] = string.Empty;
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.Message = "Gửi SMS thành công. Cổng COM lỗi không thể đồng bộ dữ liệu, kiểm tra lại cổng COM hoặc kết nối mạng. Tạm dừng Gửi SMS.";
                            dto.SmsId = Guid.Empty;
                            dto.IsFinish = true;
                        }, "Message", "SmsId", "IsFinish");
                        return;
                    }
                    // update database
                    var smsId = gvCOM.GetRowCellValue(rowHandle, "SmsId")?.ToString();
                    _dbController.UpdateSMSStatus(Guid.Parse(smsId), "SUCCESS", Guid.Parse(AccountId));
                    // update display on gridview
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.Message = "Gửi SMS thành công. Chờ 5-10 s trước khi gửi tiếp SMS khác...";
                        dto.SmsId = Guid.Empty;
                    }, "Message", "SmsId");
                    // tạm dừng
                    int delay = new Random(Guid.NewGuid().GetHashCode()).Next(5000, 10001);
                    Thread.Sleep(delay);
                    // Gửi AT lấy số điện thoại và gửi SMS
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+EGMR=") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.Message = "Thay đổi IMEI thành công.)", "Message");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        private void BtnUpdateMinAccountControl_Click(object sender, EventArgs e)
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
                    UpdateComData(sp.PortName, dto => dto.Message = "Khởi động lại đốt TK Chính...", "Message");
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                })
                { IsBackground = true };
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
                Properties.Settings.Default.COMs = String.Empty;
                Properties.Settings.Default.Save();
                foreach (var item in ComDataGrid)
                {
                    item.STT = "-1";
                }
                gvCOM.RefreshData();
            }
        }

        private void BtnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn reset lại toàn bộ cổng COM?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                foreach (var sp in SerialPorts)
                {
                    Thread thread1 = new Thread(delegate ()
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            UpdateComData(sp.PortName, dto =>
                            {
                                dto.ICCID = string.Empty;
                                dto.PhoneNumber = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message = "Reset cổng COM";
                            }, "ICCID", "PhoneNumber", "TKChinh", "Message");
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
                            logger.Error($"[{sp.PortName}] reset com error: {ex.Message}");
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thread1.Start();
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
                    Thread thread1 = new Thread(delegate ()
                    {
                        if (!sp.IsOpen) sp.Open();
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.ICCID = string.Empty;
                            dto.PhoneNumber = string.Empty;
                            dto.TKChinh = 0;
                            dto.Message = "Khôi phục cài đặt gốc cổng COM";
                        }, "ICCID", "PhoneNumber", "TKChinh", "Message");
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
                    })
                    {
                        IsBackground = true
                    };
                    thread1.Start();
                }
            }
        }

        private void BtnDoanhThu_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var report = new Report(Guid.Parse(AccountId));
            report.Show();
        }

        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            var data = ComDataGrid.Where(item => string.IsNullOrEmpty(item.PhoneNumber) || item.PhoneNumber == "PHONE NOT FOUND").ToList();
            if (data.Count <= 0) return;
            foreach (var item in data)
            {
                var com = SerialPorts.Find(x => x.PortName == item.COM);
                Thread thread = new Thread(() =>
                {
                    SendATCommand(com, "AT+QSIMSTAT?");
                })
                { IsBackground = true };
                thread.Start();
            }
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

        private void GvCOM_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "PhoneNumber")
            {
                var isFinish = bool.Parse(gvCOM.GetRowCellValue(e.RowHandle, "IsFinish")?.ToString());
                if (isFinish)
                {
                    if (!view.IsRowSelected(e.RowHandle))
                    {
                        e.Appearance.ForeColor = Color.White;
                        e.Appearance.BackColor = Color.OrangeRed;
                    }
                }
                else
                {
                    if (!view.IsRowSelected(e.RowHandle))
                    {
                        e.Appearance.ForeColor = Color.Black;
                        e.Appearance.BackColor = Color.White;
                    }
                }
            }
        }
    }
}