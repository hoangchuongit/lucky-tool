using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using LuckOTP.Model;
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
using System.Windows.Forms;

namespace LuckOTP
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

        private string PUDCheckBinance;

        public GSMForm()
        {
            InitializeComponent();
            ReadPUDBinanceCheck();
            LoadCOMForm();
            UpdatePhoneCount();
        }

        /// <summary>
        /// Phương thức xử lý sự kiện click nút Import Data Tele
        /// </summary>
        private void btnImportSimData_Click(object sender, EventArgs e)
        {
            try
            {
                using (OpenFileDialog openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";
                    openFileDialog.Title = "Chọn file Excel chứa dữ liệu Tele";

                    if (openFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        string filePath = openFileDialog.FileName;
                        // Sử dụng DBController để import dữ liệu vào CSDL
                        var dbController = new DBController();
                        int recordCount = dbController.ImportFromExcel(filePath);
                        UpdatePhoneCount();
                        // Cập nhật trạng thái cho sim theo số điện thoại
                        UpdatePhoneStatusFromDB();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lỗi khi nhập dữ liệu: {ex.Message}");
                MessageBox.Show($"Lỗi khi nhập dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Cập nhật trạng thái cho các số điện thoại từ cơ sở dữ liệu
        /// </summary>
        private void UpdatePhoneStatusFromDB()
        {
            try
            {
                var dbController = new DBController();
                foreach (var item in ComDataGrid)
                {
                    if (!string.IsNullOrEmpty(item.Phone) && item.Phone != "PHONE NOT FOUND")
                    {
                        var phone = Common.NormalizePhone(item.Phone);
                        // Tìm status trong CSDL
                        string status = dbController.FindStatusByPhone(phone);
                        // Cập nhật vào grid
                        UpdateComData(item.Com, dto => dto.TeleStatus = status, "TeleStatus");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lỗi khi cập nhật trạng thái từ CSDL: {ex.Message}");
            }
        }

        private void ReadPUDBinanceCheck()
        {
            if (!File.Exists("pud.txt")) return;
            string[] lines = File.ReadAllLines("pud.txt");
            if (lines.Length > 0) PUDCheckBinance = lines[0];
            else PUDCheckBinance = "*101#";
        }

        private void LoadCOMForm()
        {
            string[] portNames = SerialPort.GetPortNames();
            var fullPortNames = Common.GetFullPortNames();
            InitializeSerialPorts(portNames, fullPortNames);
            gridControlModems.DataSource = ComDataGrid;
            foreach (var port in SerialPorts)
            {
                var thread = new Thread(() => InitializeModem(port)) { IsBackground = true };
                thread.Start();
            }
            timerCheckSimError.Start();
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
                // tạo object tương ứng với các cột
                var data = new ComDto
                {
                    Stt = dataCOms.FindIndex(x => x == deviceID) > -1 ? (dataCOms.FindIndex(x => x == deviceID) + 1).ToString() : "-1",
                    DeviceID = deviceID,
                    Com = sp.PortName,
                    ICCID = string.Empty,
                    Phone = string.Empty,
                    TrangThai = string.Empty,
                    TeleStatus = string.Empty,
                    Message = string.Empty
                };
                ComDataGrid.Add(data);
            }
            ComDataGrid = new BindingList<ComDto>(ComDataGrid.OrderBy(c => int.Parse(c.Stt)).ToList());
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
                //SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                // Kiểm tra cổng COM đã cắm SIM hay chưa?
                //SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Error InitializeModem: {ex.Message}", "Message");
            }
        }

        /// <summary>
        /// Event lắng nghe cổng COM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            Console.WriteLine(sp.PortName + " ---------- " + MessageCOMs[sp.PortName]);

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

            // Trigger: Trạng thái SIM đã bị tháo
            if (MessageCOMs[sp.PortName].Contains("+CPIN: NOT READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                // Cập nhật gridview
                UpdateComData(sp.PortName, dto =>
                {
                    dto.ICCID = string.Empty;
                    dto.Phone = string.Empty;
                    dto.TrangThai = string.Empty;
                    dto.Message = string.Empty;
                    dto.TeleStatus = string.Empty;
                }, "Phone", "ICCID", "TrangThai", "Message", "TeleStatus");
                gridViewModems.RefreshRow(gridViewModems.LocateByValue("Com", sp.PortName));
            }

            // Trigger: Trạng thái SIM đã cắm
            if (MessageCOMs[sp.PortName].Contains("Call Ready") && MessageCOMs[sp.PortName].Contains("+CPIN: READY"))
            {
                MessageCOMs[sp.PortName] = string.Empty;
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
        }

        /// <summary>
        /// Event khi cổng COM có Error
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"Error on port {sp.PortName}: {e.EventType}");
            MessageCOMs[sp.PortName] = string.Empty;
            // Cập nhật gridview
            UpdateComData(sp.PortName, dto => dto.Phone = "PHONE NOT FOUND", "Phone");
        }

        /// <summary>
        /// Nhận tin nhắn SMS, phân tích loại dịch vụ và tách lấy OTP
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="message"></param>
        private void OTP_Service(SerialPort sp, string message)
        {
            try
            {
                int startIndex = message.IndexOf("+CMT");
                if (startIndex == -1) return;
                logger.Info($"[{sp.PortName}]: {message.Substring(startIndex)}");
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
                        int rowHandle = gridViewModems.LocateByValue("Com", sp.PortName);
                        if (rowHandle < 0)
                        {
                            MessageCOMs[sp.PortName] = string.Empty;
                            return;
                        }
                        var phone = gridViewModems.GetRowCellValue(rowHandle, "Phone")?.ToString();
                        string pattern = @"\d{4,6}";
                        Match match = Regex.Match(Regex.Replace(messData, @"\s+", "").Trim(), pattern);
                        if (!match.Success)
                        {
                            MessageCOMs[sp.PortName] = string.Empty;
                            return;
                        }
                        MessageCOMs[sp.PortName] = string.Empty;
                        // Hiển thị tin nhắn trong Message
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

        /// <summary>
        /// Lắng nghẹ trạng thái SIM có trên cổng COM hay không
        /// </summary>
        /// <param name="sp"></param>
        /// <returns></returns>
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
                else UpdateComData(sp.PortName, dto => dto.Phone = "PHONE NOT FOUND", "Phone");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Phone = "PHONE NOT FOUND", "Phone");
            }
        }

        /// <summary>
        /// Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+CPIN:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command();
                // đánh số thứ tự COM nếu SIM chưa được đặt
                var currentRow = ComDataGrid.FirstOrDefault(x => x.Com == sp.PortName);
                if (currentRow.Stt == "-1")
                {
                    var dataCOms = Properties.Settings.Default.COMs.Trim().Split(',').Where(x => !string.IsNullOrEmpty(x)).ToList();
                    //currentRow.Stt = (dataCOms.Count + 1).ToString();
                    var stt = (dataCOms.Count + 1).ToString();
                    // Cập nhật Properties.Settings
                    dataCOms.Add(currentRow.DeviceID);
                    Properties.Settings.Default.COMs = string.Join(",", dataCOms);
                    Properties.Settings.Default.Save();
                    // Làm mới GridView (cập nhật dòng cụ thể)
                    UpdateComData(sp.PortName, dto => dto.Stt = stt, "Stt");
                }
                if (mess.Contains("+CPIN: READY"))
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.TrangThai = "SIM Inserted", "TrangThai");
                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.TrangThai = "", "TrangThai");
            }
        }

        /// <summary>
        /// Lắng nghe để lấy số serial SIM
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventICCID(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+QCCID") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+QCCID");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.ICCID = mess.Substring(0, 19), "ICCID");
                // Gửi AT lấy số điện thoại
                SendATCommand(sp, $"AT+CUSD=1,\"{this.PUDCheckBinance}\",15");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = "", "ICCID");
            }
        }

        /// <summary>
        /// Lắng nghe để lấy thông tin Số điện thoại
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("ERROR"))
                {
                    UpdateComData(sp.PortName, dto => dto.Phone = "PHONE NOT FOUND", "Phone");
                    gridViewModems.RefreshRow(gridViewModems.LocateByValue("Com", sp.PortName));
                }
                else if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"{this.PUDCheckBinance}\",15");
                    var mess2 = mess.Split('\"')[1];
                    var phoneStr = mess.Split(',')[1].Replace("\"", "");
                    var phone = Common.GetPhoneNumber(phoneStr);

                    MessageCOMs[sp.PortName] = string.Empty;

                    // Tìm status trong CSDL
                    if (!string.IsNullOrEmpty(phone))
                    {
                        var phoneFormat = Common.NormalizePhone(phone);
                        var dbController = new DBController();
                        string status = dbController.FindStatusByPhone(phoneFormat);
                        UpdateComData(sp.PortName, dto => { dto.Phone = phone; dto.TeleStatus = status; dto.Message = mess2; }, "Phone", "TeleStatus", "Message");
                    }
                    else
                        UpdateComData(sp.PortName, dto => { dto.Phone = phone; dto.Message = mess2; }, "Phone", "Message");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
                //UpdateComData(sp.PortName, dto => dto.Phone = "", "Phone");
            }
        }

        /// <summary>
        /// Gửi lệnh AT
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="command"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Style row của gridview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void gridViewModems_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "Phone")
            {
                var data = gridViewModems.GetRowCellValue(e.RowHandle, "Phone")?.ToString();
                if (data == "PHONE NOT FOUND")
                    if (!view.IsRowSelected(e.RowHandle)) e.Appearance.ForeColor = Color.Red;
            }
            else if (e.Column.FieldName == "TeleStatus")
            {
                var data = gridViewModems.GetRowCellValue(e.RowHandle, "TeleStatus")?.ToString();
                var color = Color.Black;
                if (data == "NEW") color = Color.Green;
                else if (data == "TWO_FA") color = Color.Black;
                else if (data == "TELE_APP") color = Color.Blue;
                else if (data == "BANNED") color = Color.Red;
                else if (data == "RESET_7DAY") color = Color.DarkOrange;
                if (!view.IsRowSelected(e.RowHandle)) e.Appearance.ForeColor = color;
            }
        }

        /// <summary>
        /// Reset lại cổng COM. Giữ nguyên cấu hình
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnResetCom_Click(object sender, EventArgs e)
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
                                dto.Phone = string.Empty;
                                dto.TrangThai = string.Empty;
                                dto.TeleStatus = string.Empty;
                                dto.Message = "Reset cổng COM...";
                            }, "ICCID", "Phone", "TrangThai", "Message");
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

        /// <summary>
        /// Refactor cổng COM về trạng thái sau khi xuất xưởng
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnRestoreFactoryCom_Click(object sender, EventArgs e)
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
                            dto.Phone = string.Empty;
                            dto.TrangThai = string.Empty;
                            dto.TeleStatus = string.Empty;
                            dto.Message = "Khôi phục cài đặt gốc cổng COM";
                        }, "ICCID", "Phone", "TrangThai", "Message");
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

        /// <summary>
        /// Cập nhật lại danh sách cổng COM
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnUpdateNo_Click(object sender, EventArgs e)
        {
            var dataSource = gridViewModems.DataSource as BindingList<ComDto>;
            if (dataSource != null)
            {
                var newDataSource = new BindingList<ComDto>();
                var status = true;
                foreach (var item in dataSource)
                {
                    // Kiểm tra nếu có giá trị Stt trùng nhau
                    var duplicateStt = dataSource.Where(x => x != item && x.Stt == item.Stt).FirstOrDefault();
                    if (duplicateStt != null && duplicateStt.Stt != "-1")
                    {
                        status = false;
                        MessageBox.Show($"Cảnh báo: STT {item.Stt} bị trùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    newDataSource.Add(item);
                }
                if (status)
                {
                    var sortedData = new BindingList<ComDto>(newDataSource.OrderBy(x => int.Parse(x.Stt)).ToList());
                    ComDataGrid.Clear();
                    List<string> deviceIDs = new List<string>();
                    foreach (var item in sortedData)
                    {
                        ComDataGrid.Add(item);
                        if (item.Stt != "-1")
                            deviceIDs.Add(item.DeviceID);
                    }
                    var COMs = string.Join(",", deviceIDs);
                    Properties.Settings.Default.COMs = COMs;
                    Properties.Settings.Default.Save();
                }
            }
            else
            {
                logger.Error("Cảnh báo: DataSource là null!");
            }
        }

        /// <summary>
        /// Cập nhật Row trong danh sách COM
        /// </summary>
        /// <param name="portName"></param>
        /// <param name="updateAction"></param>
        /// <param name="propertyNames"></param>
        private void UpdateComData(string portName, Action<ComDto> updateAction, params string[] propertyNames)
        {
            try
            {
                var item = ComDataGrid.FirstOrDefault(dto => dto.Com == portName);
                if (item == null) return;
                updateAction(item);
                InvokeIfRequired(() =>
                {
                    int rowHandle = gridViewModems.LocateByValue("Com", portName);
                    if (rowHandle >= 0 && propertyNames != null && propertyNames.Any())
                        foreach (var propertyName in propertyNames)
                        {
                            gridViewModems.RefreshRowCell(rowHandle, gridViewModems.Columns[propertyName]);
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
            if (gridControlModems.InvokeRequired) gridControlModems.Invoke(action);
            else action();
        }

        /// <summary>
        /// Cập nhật label hiển thị số lượng phone
        /// </summary>
        private void UpdatePhoneCount()
        {
            var dbController = new DBController();
            int totalPhones = dbController.CountRecords();
            lblPhoneCount.Text = $"Số SIM trong DB: {totalPhones} sim";
        }

        /// <summary>
        /// Mỗi 10s kiểm tra xem Phone nào đang có trạng thái SIM ERROR thì thử lại
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timerCheckSimError_Tick(object sender, EventArgs e)
        {
            var data = ComDataGrid.Where(item => string.IsNullOrEmpty(item.Phone) || item.Phone == "PHONE NOT FOUND").ToList();
            if (data.Count <= 0) return;
            foreach (var item in data)
            {
                var com = SerialPorts.Find(x => x.PortName == item.Com);
                Thread thread = new Thread(() =>
                {
                    SendATCommand(com, "AT+QSIMSTAT?");
                })
                { IsBackground = true };
                thread.Start();
            }
        }

        private void btnResetNo_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Bạn có muốn cài đặt lại STT cho các cổng COM không?", "Reset STT", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                Properties.Settings.Default.COMs = String.Empty;
                Properties.Settings.Default.Save();
                foreach (var item in ComDataGrid)
                {
                    item.Stt = "-1";
                }
                gridViewModems.RefreshData();
            }
        }

        private void btnClearSimData_Click(object sender, EventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn xóa dữ liệu?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var dbController = new DBController();
                dbController.ClearDatabase();
                UpdatePhoneCount();
            }
        }
    }
}