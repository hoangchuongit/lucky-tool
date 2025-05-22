using DevExpress.Data.Extensions;
using DevExpress.Internal.WinApi.Windows.UI.Notifications;
using DevExpress.XtraEditors;
using LuckBurn.Model;
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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckBurnTK
{
	public partial class BurnTKForm: DevExpress.XtraEditors.XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        private readonly string AccountId;

        public BurnTKForm()
		{
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            txtMinAccountControl.EditValue = 1000;
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
                MessageCOMs.TryAdd(sp.PortName, "");

                var data = new ComDto
                {
                    COM = sp.PortName,
                    STT = dataCOms.FindIndex(x => x == deviceID) > -1 ? (dataCOms.FindIndex(x => x == deviceID) + 1).ToString() : "-1",
                    DeviceID = deviceID,
                    ICCID = "",
                    PhoneNumber = "",
                    TKChinh = 0,
                    Message = ""
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
                // Xóa tất cả file trong RAM - chủ yếu các file ghi âm
                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                // Kiểm tra cổng COM đã cắm SIM hay chưa?
                SendATCommand(sp, "AT+QSIMSTAT?");
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
                logger.Error("Error khi đọc từ SerialPort: " + ex.Message);
            }

            MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine(sp.PortName +" ---------- " +MessageCOMs[sp.PortName]);


            if (MessageCOMs[sp.PortName].Contains("RING"))
            {
                SendATCommand(sp, "ATH");
                MessageCOMs[sp.PortName] = string.Empty;
            }

            // Nhận SMS tin nhắn văn bản
            if (MessageCOMs[sp.PortName].Contains("+CMT:"))
            {
                var message = MessageCOMs[sp.PortName].AT_Command();
                OTP_Service(sp, message);
            }

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

            // Trigger: Trạng thái SIM đã bị tháo
            if (MessageCOMs[sp.PortName].Contains("+CPIN: NOT READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                // Cập nhật gridview
                UpdateComData(sp.PortName, dto =>
                {
                    dto.ICCID = ""; dto.PhoneNumber = ""; dto.TKChinh = 0; dto.Message = "";
                }, "PhoneNumber", "ICCID", "TrangThai", "NhaMang", "Message", "Tele");
                gvCOM.RefreshRow(gvCOM.LocateByValue("COM", sp.PortName));
            }

            // Trigger: Trạng thái SIM đã cắm
            if (MessageCOMs[sp.PortName].Contains("Call Ready") && MessageCOMs[sp.PortName].Contains("+CPIN: READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"Error on port {sp.PortName}: {e.EventType}");
            MessageCOMs[sp.PortName] = string.Empty;
            // Cập nhật gridview
            UpdateComData(sp.PortName, dto => dto.PhoneNumber = "PHONE NOT FOUND", "PhoneNumber");
            gvCOM.RefreshRow(gvCOM.LocateByValue("COM", sp.PortName));
        }

        private void ListenEventSIMInsert(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+QSIMSTAT:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+QSIMSTAT?");
                if (mess.Contains("+QSIMSTAT: 0,1"))
                {
                    var messSplit = mess.Replace("+QSIMSTAT: ", "").Split(',');
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
                    UpdateComData(sp.PortName, dto => dto.STT = stt, "Stt");
                }
                if (mess.Contains("+CPIN: READY"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;

                    UpdateComData(sp.PortName, dto => dto.Message = "SIM READY", "Message");

                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message = "", "Message");
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
                // Gửi AT lấy số điện thoại
                SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = "", "ICCID");
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
                    var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                    logger.Info($"[{sp.PortName}]: {mess}");
                    var phoneStr = mess.Split(',')[1].Replace("\"", "");
                    var phone = Common.GetPhoneNumber(phoneStr);
                    int? tkchinh = Common.ExtractBalance(mess);

                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => { 
                        dto.PhoneNumber = phone; 
                        dto.TKChinh = (int)(tkchinh.HasValue ? tkchinh : 0); 
                        dto.Message = phoneStr; 
                    }, "PhoneNumber", "TKChinh", "Message");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
            }
        }

        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+EGMR=") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                logger.Info($"ListenEventChangeIMEI: {mess}");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.Message = "Thay đổi IMEI thành công.)", "Message");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = "", "ICCID");
            }
        }

        private void OTP_Service(SerialPort sp, string message)
        {
            try
            {
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
                        int rowHandle = gvCOM.LocateByValue("COM", sp.PortName);
                        if (rowHandle < 0)return;
                        UpdateComData(sp.PortName, dto => dto.Message = messData.ToString(), "Message");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"[{sp.PortName}] Error: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] Error: {ex.Message}");
            }
        }

        private void btnStartStop_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
        }

        private void btnStartStopControl_Click(object sender, EventArgs e)
        {
        }

        private void btnUpdateMinAccountControl_Click(object sender, EventArgs e)
        {
            decimal minValue = Convert.ToDecimal(txtMinAccountControl.EditValue);
        }

        private void btnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Xử lý cập nhật STT cổng COM
            XtraMessageBox.Show("Đã cập nhật STT cổng COM", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnResetComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            // Xử lý đặt lại STT cổng COM
            XtraMessageBox.Show("Đã đặt lại STT cổng COM", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                                dto.ICCID = "";
                                dto.PhoneNumber = "";
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
                            SendATCommand(sp, "AT+CFUN=1,1", 60000);
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

        private void btnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                            dto.ICCID = "";
                            dto.PhoneNumber = "";
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

        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            var comErrors = new List<SerialPort>();

            for (int i = 0; i < ComDataGrid.Count; i++)
            {
                var item = ComDataGrid[i];

                if (string.IsNullOrEmpty(item.PhoneNumber) || item.PhoneNumber == "PHONE NOT FOUND")
                {
                    if (string.IsNullOrEmpty(item.COM)) continue;

                    var com = SerialPorts.Find(x => x.PortName == item.COM);
                    if (com == null) continue;

                    comErrors.Add(com);
                }
            }

            Thread thread = new Thread(() =>
            {
                foreach (var com in comErrors)
                {
                    SendATCommand(com, "AT+QSIMSTAT?");
                }
            })
            {
                IsBackground = true
            };
            thread.Start();
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
                sp.WriteLine(command + Environment.NewLine);
                Thread.Sleep(timeout);
                MessageCOMs[sp.PortName].AT_Command(command);
            }
            catch (Exception ex)
            {
                logger.Error($"SendATCommand: {ex.Message}");
            }
        }
    }
}