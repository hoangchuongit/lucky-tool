using DevExpress.ClipboardSource.SpreadsheetML;
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
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net.Http;
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

        private readonly SemaphoreSlim _simCheckLimiter = new SemaphoreSlim(16);

        /// Danh sách cổng COM
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        /// Dữ liệu GridView cổng COM
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        /// Lịch sử các tin nhắn của từng cổng COM
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();

        /// Danh sách cổng COM đang ghi âm
        private readonly List<string> RecordingPorts = new List<string>();

        /// Nội dung ghi âm của từng cổng COM
        private readonly ConcurrentDictionary<string, byte[]> RecordingCOMs = new ConcurrentDictionary<string, byte[]>();

        /// Danh sách các cổng COM đang upload file đến thiết bị
        private readonly ConcurrentDictionary<string, FileToCom> FileToCOMs = new ConcurrentDictionary<string, FileToCom>();

        private readonly PrefixNumberController _prefixController;

        private readonly string AccountId;

        private readonly string ApiKey;

        public BurnTKForm(string accountId, string apikey)
        {
            InitializeComponent();
            AccountId = accountId;
            ApiKey = apikey;
            _prefixController = new PrefixNumberController();
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
                _ = Task.Run(() =>
                {
                    _simCheckLimiter.Wait();
                    try
                    {
                        InitializeModem(port);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"{port.PortName} - LoadCOMForm Error: {ex.Message}");
                    }
                    finally
                    {
                        _simCheckLimiter.Release();
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
                var isValid = fullPortNames.FirstOrDefault(x => Regex.IsMatch(x["Caption"], regexPattern, RegexOptions.IgnoreCase) && x["Caption"].Contains("XR21V1414"));
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
                //SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
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
            bool inWAVDownload = false;
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

            if (MessageCOMs[sp.PortName].Contains("RING"))
            {
                SendATCommand(sp, "ATH");
                MessageCOMs[sp.PortName] = string.Empty;
            }

            if (MessageCOMs[sp.PortName].Contains("CMS ERROR"))
            {
                UpdateComData(sp.PortName, dto =>
                {
                    dto.Message = "Cổng COM gặp lỗi. Rút SIM và chờ 20s sau đó lắp lại.";
                    dto.SmsId = Guid.Empty;
                    dto.IsFinish = true;
                }, "Message", "SmsId");
            }

            // Nếu bắt đầu bằng chữ CONNECT là đang bắt đầu lưu file wav vào trong folder
            //if (MessageCOMs[sp.PortName].Contains("\r\nCONNECT\r\n")) inWAVDownload = true;

            // Nhận cuộc gọi
            if (MessageCOMs[sp.PortName].Contains("NO CARRIER") || MessageCOMs[sp.PortName].Contains("HANG UP"))
                StopRecording(sp);

            // Kiểm tra nếu cổng COM nằm trong danh sách đang ghi âm cuộc gọi và lưu file ghi âm
            //if (inWAVDownload || RecordingCOMs.ContainsKey(sp.PortName))
            //{
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
            // Kiểm tra nếu cổng COM nằm trong danh sách đang ghi âm và nhận được tín hiệu kết thúc ghi âm,
            // lưu lại file .wav và chuyển đổi thành văn bản
            if (MessageCOMs[sp.PortName].Contains("+QFDWL:") && MessageCOMs[sp.PortName].Contains("\r\nCONNECT\r\n"))
            {
                if (RecordingPorts.Contains(sp.PortName)) SaveRecord(sp);
                RecordingCOMs.TryRemove(sp.PortName, out byte[] _);
            }
            //}

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
            ListenEventUploadAmrFileToDevice(sp);

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
                UpdateComData(sp.PortName, dto => dto.Message101 = $"SIM đã sẵn sàng.s", "Message101");
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"Error on port {sp.PortName}: {e.EventType}");
            MessageCOMs[sp.PortName] = string.Empty;
            UpdateComData(sp.PortName, dto => dto.PhoneNumber = "Phone Unknow", "PhoneNumber");
        }

        /// <summary>
        /// Dừng ghi âm cuộc gọi
        /// </summary>
        /// <param name="sp"></param>
        private void StopRecording(SerialPort sp)
        {
            try
            {
                if (!RecordingPorts.Contains(sp.PortName)) return;
                // Dừng ghi âm
                SendATCommand(sp, "AT+QAUDRD=0", 1000);
                // Tải xuống file âm thanh voice.wav từ RAM của module.
                // Dữ liệu sẽ được truyền qua Serial và thu thập từng gói cho đến khi hoàn thành.
                sp.WriteLine("AT+QFDWL=\"RAM:record.amr\"");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi dừng cuộc gọi thất bại: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - StopRecording Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lưu bản ghi âm thành file
        /// </summary>
        /// <param name="sp"></param>
        private void SaveRecord(SerialPort sp)
        {
            try
            {
                // Kiểm tra key trong dictionary RecordingCOMs
                if (!RecordingCOMs.ContainsKey(sp.PortName))
                {
                    UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi Voice to Text: COM không của dịch vụ nào", "Message");
                    logger.Error($"Error Voice to Text: RecordingCOMs does not contain the key: {sp.PortName}");
                    // Xóa file ghi âm trên RAM
                    SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                    // Xóa khỏi danh sách cổng COM đang ghi âm
                    if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                    MessageCOMs[sp.PortName] = string.Empty;
                    return;
                }
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                string recordDirectory = Path.Combine(baseDirectory, "record");
                Directory.CreateDirectory(recordDirectory);

                // Lấy dữ liệu audio
                byte[] audioData = RecordingCOMs[sp.PortName];
                var phone = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName).PhoneNumber ?? Guid.NewGuid().ToString();
                string filePath = Path.Combine(recordDirectory, $"{phone}_{DateTime.Now:yyyyMMddHHmmss}.amr");
                File.WriteAllBytes(filePath, audioData);
                //byte[] audioData = RecordingCOMs[sp.PortName];
                //byte[] wavHeader = Common.CreateWavHeader(audioData.Length, 8000, 1, 16);
                //byte[] finalData = new byte[wavHeader.Length + audioData.Length];
                //Buffer.BlockCopy(wavHeader, 0, finalData, 0, wavHeader.Length);
                //Buffer.BlockCopy(audioData, 0, finalData, wavHeader.Length, audioData.Length);
                //string filePath = Path.Combine(recordDirectory, $"{sp.PortName}_{DateTime.Now:yyyyMMddHHmmss}.amr");
                //File.WriteAllBytes(filePath, finalData);

                // Xóa file ghi âm trên RAM
                SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                // Xóa khỏi danh sách cổng COM đang ghi âm
                if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                MessageCOMs[sp.PortName] = string.Empty;
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi Voice to Text: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - SaveRecord Error: {ex.Message}");
            }
        }

        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+EGMR=") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                MessageCOMs[sp.PortName] = string.Empty;
                Console.WriteLine($"mess: {mess}");
                UpdateComData(sp.PortName, dto => dto.IMEI = mess, "IMEI");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
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
                UpdateComData(sp.PortName, dto => dto.PhoneNumber = "Phone Unknow", "PhoneNumber");
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
                    UpdateComData(sp.PortName, dto => dto.Message101 = "SIM đã sẵn sàng.", "Message101");
                    SendATCommand(sp, "AT+QCCID");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = string.Empty, "Message101");
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
                // Gửi AT lấy thông tin nhà mạng
                SendATCommand(sp, "AT+COPS?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        private void ListenEventTelecom(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("+COPS:") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+COPS?").ToLower();
                MessageCOMs[sp.PortName] = string.Empty;
                string provider = "";
                if (mess.Contains("viettel")) provider = "Viettel";
                else if (mess.Contains("mobifone")) provider = "Mobifone";
                else if (mess.Contains("vinaphone")) provider = "Vinaphone";
                else provider = "Other";
                UpdateComData(sp.PortName, dto => dto.Telecom = provider, "Telecom");
                // Gửi AT lấy số điện thoại và gửi SMS
                SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Telecom = "Unknown", "Telecom");
            }
        }

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
                UpdateComData(sp.PortName, dto => dto.Message = "Lấy số điện thoại thất bại", "Message");
            }
        }

        private async Task SmsOrCallWithPrefix(SerialPort sp)
        {
            try
            {
                var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                MessageCOMs[sp.PortName] = string.Empty;
                mess = mess.Substring(mess.IndexOf("+CUSD"));
                if (mess.Split(',').Length <= 0) return;
                var mess2 = mess.Split('\"')[1];
                UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");

                var phoneStr = mess2.Replace("\"", string.Empty);
                if (string.IsNullOrEmpty(phoneStr)) return;
                var phone = Common.GetPhoneNumber(phoneStr);
                if (string.IsNullOrEmpty(phone))
                {
                    int delay = new Random(Guid.NewGuid().GetHashCode()).Next(3000, 5001);
                    Thread.Sleep(delay);
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
                else
                {
                    int? tkchinh = Common.ExtractBalance(mess);
                    int currentTKC = (int)(tkchinh.HasValue ? tkchinh : 0);
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.PhoneNumber = phone; dto.TKChinh = currentTKC;
                    }, "PhoneNumber", "TKChinh");

                    int minAccount = int.Parse(txtMinAccountControl.Text.Replace(".", string.Empty));
                    if (currentTKC <= minAccount)
                    {
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.Message = $"Stop burn"; dto.IsFinish = true;
                        }, "Message", "IsFinish");
                        return;
                    }
                    var telecom = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName).Telecom;
                    if (telecom == null) return;
                    var prefixSmsReq = new GetPrefixSmsReq()
                    {
                        phone_number = phone,
                        amount = currentTKC,
                        amount_left = minAccount,
                        telecom = telecom
                    };
                    var prefixSmsRes = await _prefixController.GetPrefixNumber(prefixSmsReq, ApiKey);
                    Console.WriteLine(prefixSmsRes);
                    if (prefixSmsRes == null)
                    {
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.Message = $"Burning ..."; dto.IsFinish = true;
                        }, "Message", "IsFinish");
                        // Thử lại sau 2 phút
                        Thread.Sleep(120000);
                        // Gửi AT lấy số điện thoại và gửi SMS
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    }
                    else
                    {
                        if (sp.PortName == "COM16")
                        {
                            // TODO: Thông tin file
                            string filename = "HỖ-TRỢ-1-KỲ-19S.amr";
                            string filePath = $"D:\\Freelancer\\LuckTools\\LuckTest\\{filename}";
                            //
                            byte[] data = File.ReadAllBytes(filePath);
                            FileToCOMs.TryAdd(sp.PortName, new FileToCom() { data = data });
                            //
                            sp.DiscardInBuffer();
                            SendATCommand(sp, $"AT+QFOPEN=\"RAM:content.amr\",0,{data.Length}");

                            //SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                            //SendATCommand(sp, "AT+CFUN=1,1", 10000);

                            //SendATCommand(sp, $"ATD{"18001091"};", 1000);
                            //Thread.Sleep(15000);

                            // Kết thúc cuộc gọi
                            //SendATCommand(sp, "ATH", 2000);
                            // Dừng ghi âm
                            //SendATCommand(sp, "AT+QAUDRD=0", 1000);
                        }

                        //if (prefixSmsRes.type == PrefixNumberType.CALL)
                        //{
                        //    UpdateComData(sp.PortName, dto =>
                        //    {
                        //        dto.Message = "Burning ..."; dto.IsFinish = false; dto.SmsId = prefixSmsRes.history_id;
                        //    }, "Message", "IsFinish", "SmsId");
                        //}
                        // Gọi phương thức GetPrefixAndMessage của DBController
                        //var (prefix, message, amountValue) = _dbController.GetPrefixAndMessage(phone, currentTKC, minAccount, Guid.Parse(AccountId));
                        //if (string.IsNullOrEmpty(prefix))
                        //{
                        //    UpdateComData(sp.PortName, dto =>
                        //    {
                        //        dto.PhoneNumber = phone;
                        //        dto.TKChinh = currentTKC;
                        //        dto.Message101 = mess2;
                        //        dto.Message = $"Chờ lượt gửi SMS tiếp theo...";
                        //        dto.IsFinish = true;
                        //    }, "PhoneNumber", "TKChinh", "Message101", "Message", "IsFinish");
                        //    Thread.Sleep(300000); // 5 phút
                        //    // Gửi AT lấy số điện thoại và gửi SMS
                        //    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                        //}
                        //else
                        //{
                        //SendATCommand(sp, $"AT+CMGS=\"{prefix}\"", 500);
                        //SendATCommand(sp, $"{message}{(char)26}", 500);
                        //var item = ComDataGrid.FirstOrDefault(dto => dto.COM == sp.PortName);
                        //var smsId = _dbController.InsertSMSHistory(item.ICCID, phone, prefix, message, amountValue, Guid.Parse(AccountId));

                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Burn thất bại: {ex.Message}");
                UpdateComData(sp.PortName, dto => { dto.Message = $"Stop burn"; dto.IsFinish = true; }, "Message", "IsFinish");
            }
        }

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
                    SendATCommand(sp, $"AT+QFCLOSE={fileToCOM.fd}",2000);
                    FileToCOMs.TryRemove(sp.PortName, out _);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ListenEventUploadAmrFileToDevice: {ex.Message}");
            }
        }

        private void ListenEventCallPrefix(SerialPort sp)
        {
            try
            {
                if (MessageCOMs[sp.PortName].Contains("+CLCC:") && MessageCOMs[sp.PortName].Contains(",0,0,0,0,"))
                {
                    var mess = MessageCOMs[sp.PortName];
                    MessageCOMs[sp.PortName] = string.Empty;
                    if (RecordingPorts.Contains(sp.PortName)) return;
                    RecordingPorts.Add(sp.PortName);
                    // Mở mic ghi âm
                    SendATCommand(sp, "AT+QAUDRD=1,\"RAM:record.amr\",3");
                    // Chờ 12s để tổng đài nói
                    Thread.Sleep(12000);
                    // Bấm phím 9
                    //SendATCommand(sp, "AT+VTS=9");
                    // Phát file đã ghi âm trước đó ở cổng COM
                    Thread.Sleep(20000);
                    //
                    SendATCommand(sp, "ATH", 2000);
                }
                else if (MessageCOMs[sp.PortName].Contains("+CLCC:") && MessageCOMs[sp.PortName].Contains(",0,6,0,0,"))
                {
                    StopRecording(sp);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ListenEventCallPrefix: {ex.Message}");
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
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.Message = "Gửi SMS thành công. Cổng COM lỗi không thể đồng bộ dữ liệu, kiểm tra lại cổng COM hoặc kết nối mạng. Tạm dừng Gửi SMS.";
                            dto.SmsId = Guid.Empty;
                            dto.IsFinish = true;
                        }, "Message", "SmsId", "IsFinish");
                        return;
                    }
                    //// update database
                    //var smsId = gvCOM.GetRowCellValue(rowHandle, "SmsId")?.ToString();
                    //_dbController.UpdateSMSStatus(Guid.Parse(smsId), "SUCCESS", Guid.Parse(AccountId));
                    //// update display on gridview
                    //UpdateComData(sp.PortName, dto =>
                    //{
                    //    dto.Message = "Gửi SMS thành công. Chờ 5-10 s trước khi gửi tiếp SMS khác...";
                    //    dto.SmsId = Guid.Empty;
                    //}, "Message", "SmsId");
                    //// tạm dừng
                    //int delay = new Random(Guid.NewGuid().GetHashCode()).Next(5000, 10001);
                    //Thread.Sleep(delay);
                    //// Gửi AT lấy số điện thoại và gửi SMS
                    //SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
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
                _ = Task.Run(() =>
                {
                    _simCheckLimiter.Wait();
                    try
                    {
                        UpdateComData(sp.PortName, dto => dto.Message = "Burning ...", "Message");
                        SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                    }
                    finally
                    {
                        _simCheckLimiter.Release();
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
                Properties.Settings.Default.COMs = String.Empty;
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
                        _simCheckLimiter.Wait();
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
                            Console.WriteLine($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                        finally
                        {
                            _simCheckLimiter.Release();
                        }
                    });
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
                    _ = Task.Run(() =>
                    {
                        _simCheckLimiter.Wait();
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
                            Console.WriteLine($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                        finally
                        {
                            _simCheckLimiter.Release();
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
                        _simCheckLimiter.Wait();
                        try
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
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Lỗi khi gửi lệnh tới {sp.PortName}: {ex.Message}");
                        }
                        finally
                        {
                            _simCheckLimiter.Release();
                        }
                    });
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
            var data = ComDataGrid.Where(item => string.IsNullOrEmpty(item.PhoneNumber) || item.PhoneNumber == "Phone Unknow").ToList();
            if (data.Count <= 0) return;
            foreach (var item in data)
            {
                var com = SerialPorts.Find(x => x.PortName == item.COM);
                if (com == null) continue;
                _ = Task.Run(() =>
                {
                    _simCheckLimiter.Wait();
                    try
                    {
                        SendATCommand(com, "AT+QSIMSTAT?");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi gửi lệnh tới {com.PortName}: {ex.Message}");
                    }
                    finally
                    {
                        _simCheckLimiter.Release();
                    }
                });
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
    }
}