using DevExpress.Data.Extensions;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using LuckOTP.Model;
using LuckOTP.Repositories;
using LuckOTP.Utils;
using Newtonsoft.Json;
using Npgsql;
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
using TranscriptionService;

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

        /// Danh sách cổng COM đang ghi âm
        private readonly List<string> RecordingPorts = new List<string>();

        /// Danh sachs dich vụ của hệ thống
        private BindingList<Service> ServiceGrid { get; set; } = new BindingList<Service>();

        /// Danh sách các dịch vụ mà GSM cho sử dung
        private readonly List<Service> ActiveServices = new List<Service>();

        /// Nội dung ghi âm của từng cổng COM
        private readonly ConcurrentDictionary<string, byte[]> RecordingCOMs = new ConcurrentDictionary<string, byte[]>();

        private readonly string AccountId;

        private readonly string Country;

        private string PUDCheckBinance;

        public GSMForm(string accountId, string country)
        {
            Text = $"LUCK OTP GSM - {Properties.Settings.Default.Username}";
            InitializeComponent();
            AccountId = accountId;
            Country = country;
            SetupServices();
            ReadPUDBinanceCheck();
            StartListeningForNotifications();
            LoadCOMForm();
            TimerCheckSimError.Start();
            TimerSyncDB.Start();
        }

        /// <summary>
        /// Cấu hình danh sách dịch vụ
        /// </summary>
        private void SetupServices()
        {
            try
            {
                var serviceRepo = new ServiceRepository();
                var services = serviceRepo.GetAllService();
                foreach (var item in services)
                {
                    ServiceGrid.Add(item);
                }
                GridControlServices.DataSource = ServiceGrid;

                if (!File.Exists("service.config")) return;
                var lines = File.ReadAllLines("service.config").ToList();

                for (int i = 0; i < GridViewServices.RowCount; i++)
                {
                    object cellValue = GridViewServices.GetRowCellValue(i, "code");
                    if (cellValue != null && lines.Contains(cellValue))
                    {
                        GridViewServices.SelectRow(i);
                        var service = GridViewServices.GetRow(i) as Service;
                        ActiveServices.Add(service);
                    }
                }
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show(ex.Message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ReadPUDBinanceCheck()
        {
            if (File.Exists("pud.txt"))
            {
                string[] lines = File.ReadAllLines("pud.txt");
                if (lines.Length > 0) PUDCheckBinance = lines[0];
                else PUDCheckBinance = "*101#";
            }
        }

        private async void StartListeningForNotifications()
        {
            using (var conn = new NpgsqlConnection(Common.connectionString))
            {
                await conn.OpenAsync();
                conn.Notification += (o, e) =>
                {
                    Invoke(new Action(() =>
                    {
                        try
                        {
                            // Kiểm tra channel nhận được thông báo
                            if (e.Channel == "service_channel")
                            {
                                var data = JsonConvert.DeserializeObject<SimListenCurrentServiceCode>(e.Payload);
                                var comGrid = ComDataGrid.FirstOrDefault(x => x.Phone == data.phone_number);
                                var sp = SerialPorts.Find(x => x.PortName == comGrid.Com);
                                if (data.service_code == "zalo")
                                {
                                    SendATCommand(sp, "AT+CMGS=\"6020\"", 500);
                                    SendATCommand(sp, "ZALO" + (char)26, 500);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi LISTEN service_channel: {ex.Message}");
                        }
                    }));
                };
                using (var cmd = new NpgsqlCommand("LISTEN service_channel;", conn))
                {
                    await cmd.ExecuteNonQueryAsync();
                }
                await Task.Run(() =>
                {
                    while (true)
                    {
                        conn.Wait();
                    }
                });
            }
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

                // tạo object tương ứng với các cột
                var data = new ComDto
                {
                    Stt = dataCOms.FindIndex(x => x == deviceID) > -1 ? (dataCOms.FindIndex(x => x == deviceID) + 1).ToString() : "-1",
                    Com = sp.PortName,
                    DeviceID = deviceID,
                    ICCID = "",
                    Phone = "",
                    TrangThai = "",
                    Message = ""
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
                SendATCommand(sp, "AT+CCFC=0,0");
                // Xóa tất cả file trong RAM - chủ yếu các file ghi âm
                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
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
            bool inWAVDownload = false;
            try
            {
                bytesRead = sp.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                logger.Error("Error khi đọc từ SerialPort: " + ex.Message);
            }

            MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            //Console.WriteLine(MessageCOMs[sp.PortName]);

            // Nhận SMS tin nhắn văn bản
            if (MessageCOMs[sp.PortName].Contains("+CMT:"))
            {
                var message = MessageCOMs[sp.PortName].AT_Command();
                OTP_Service(sp, message);
            }

            // Nếu bắt đầu bằng chữ RIFF là đang bắt đầu lưu file wav vào trong folder
            if (MessageCOMs[sp.PortName].Contains("RIFF")) inWAVDownload = true;

            // Nhận cuộc gọi
            if (MessageCOMs[sp.PortName].Contains("RING"))
                StartRecording(sp);
            else if (MessageCOMs[sp.PortName].Contains("NO CARRIER") || MessageCOMs[sp.PortName].Contains("HANG UP"))
                StopRecording(sp);

            // Kiểm tra nếu cổng COM nằm trong danh sách đang ghi âm cuộc gọi và lưu file ghi âm
            if (inWAVDownload || RecordingCOMs.ContainsKey(sp.PortName))
            {
                // Nếu cổng COM chưa nằm trong danh sách ghi âm thì bổ sung vào danh sách. Nếu đã có thì ghi nối tiếp dữ liệu
                if (!MessageCOMs[sp.PortName].Contains("+QFDWL:"))
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
                        RecordingCOMs.TryAdd(sp.PortName, buffer);
                    }
                }
                // Kiểm tra nếu cổng COM nằm trong danh sách đang ghi âm và nhận được tín hiệu kết thúc ghi âm,
                // lưu lại file .wav và chuyển đổi thành văn bản
                if (MessageCOMs[sp.PortName].Contains("+QFDWL:"))
                {
                    if (RecordingPorts.Contains(sp.PortName))
                    {
                        ConvertVoiceToText(sp);
                    }
                    RecordingCOMs.TryRemove(sp.PortName, out byte[] _);
                }
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
                // Cập nhật SIM lên hệ thống database
                UploadSimToSystem(sp.PortName,string.Empty, string.Empty, true);
                // Cập nhật gridview
                UpdateComData(sp.PortName, dto =>
                {
                    dto.ICCID = ""; dto.Phone = ""; dto.TrangThai = ""; dto.Message = "";
                }, "ICCID", "Phone", "TrangThai", "Message");
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
            // Cập nhật SIM lên hệ thống database
            UploadSimToSystem(sp.PortName, string.Empty, string.Empty, true);
            // Cập nhật gridview
            UpdateComData(sp.PortName, dto => dto.Phone = "PHONE ERROR", "Phone");
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
                        int rowHandle = gvCOM.LocateByValue("Com", sp.PortName);
                        if (rowHandle < 0)
                        {
                            MessageCOMs[sp.PortName] = string.Empty;
                            return;
                        }
                        var phone = gvCOM.GetRowCellValue(rowHandle, "Phone")?.ToString();
                        var simRepo = new SimRepository();

                        string pattern = @"\d{4,6}";
                        Match match = Regex.Match(Regex.Replace(messData, @"\s+", "").Trim(), pattern);
                        if (!match.Success)
                        {
                            MessageCOMs[sp.PortName] = string.Empty;
                            return;
                        }
                        MessageCOMs[sp.PortName] = string.Empty;
                        // cập nhật otp lên hệ thống
                        simRepo.UpdateOtpTransaction(phone, AccountId, brandName, messData.ToString(), match.Value);
                        // Hiển thị tin nhắn trong Message
                        UpdateComData(sp.PortName, dto => dto.Message = messData.ToString(), "Message");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"[{sp.PortName}] Error: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - OTP_Service Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Bắt đầu ghi âm cuộc gọi
        /// </summary>
        /// <param name="sp"></param>
        private void StartRecording(SerialPort sp)
        {
            try
            {
                int rowHandle = gvCOM.LocateByValue("Com", sp.PortName);
                var phone = gvCOM.GetRowCellValue(rowHandle, "Phone")?.ToString();
                var simRepo = new SimRepository();
                var service = simRepo.CheckServiceOfPhone(phone, AccountId);
                if (string.IsNullOrEmpty(service))
                {
                    // Từ chối cuộc gọi nếu không có dịch vụ
                    SendATCommand(sp, "ATH", 5000);
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.Message = "Từ chối cuộc gọi lạ!", "Message");
                    return;
                }
                if (RecordingPorts.Contains(sp.PortName)) return;
                RecordingPorts.Add(sp.PortName);
                // Trả lời cuộc gọi
                SendATCommand(sp, "ATA");
                // Mở mic ghi âm
                SendATCommand(sp, "AT+QAUDRD=1,\"RAM:record.wav\",13");
                // Cập nhật gridview
                UpdateComData(sp.PortName, dto => dto.Message = "Nhận cuộc gọi ...", "Message");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi: Nhận cuộc gọi thất bại: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - StartRecording Error: {ex.Message}");
            }
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
                // Kết thúc cuộc gọi
                SendATCommand(sp, "ATH", 2000);
                // Dừng ghi âm
                SendATCommand(sp, "AT+QAUDRD=0", 1000);
                // Tải xuống file âm thanh voice.wav từ RAM của module.
                // Dữ liệu sẽ được truyền qua Serial và thu thập từng gói cho đến khi hoàn thành.
                sp.WriteLine("AT+QFDWL=\"RAM:record.wav\"");
                Thread.Sleep(1000);
                // Cập nhật gridview
                UpdateComData(sp.PortName, dto => dto.Message = "Kết thúc cuộc gọi và xử lý dữ liệu...", "Message");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi dừng cuộc gọi thất bại: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - StopRecording Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Convert voice to text
        /// </summary>
        /// <param name="sp"></param>
        private async void ConvertVoiceToText(SerialPort sp)
        {
            try
            {
                // Kiểm tra key trong dictionary RecordingCOMs
                if (!RecordingCOMs.ContainsKey(sp.PortName))
                {
                    UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi Voice to Text: COM không của dịch vụ nào", "Message");
                    logger.Error($"Error Voice to Text: RecordingCOMs does not contain the key: {sp.PortName}");
                    // Xóa file ghi âm trên RAM
                    SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
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
                byte[] wavHeader = Common.CreateWavHeader(audioData.Length, 8000, 1, 16);
                byte[] finalData = new byte[wavHeader.Length + audioData.Length];
                Buffer.BlockCopy(wavHeader, 0, finalData, 0, wavHeader.Length);
                Buffer.BlockCopy(audioData, 0, finalData, wavHeader.Length, audioData.Length);
                string filePath = Path.Combine(recordDirectory, $"{sp.PortName}_{DateTime.Now:yyyyMMddHHmmss}.wav");
                File.WriteAllBytes(filePath, finalData);
                // Kiểm tra key trong dictionary MessageCOMs
                if (!MessageCOMs.ContainsKey(sp.PortName))
                {
                    logger.Error($"Error Voice to Text: MessageCOMs does not contain the key: {sp.PortName}");
                    // Xóa file ghi âm trên RAM
                    SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                    // Xóa khỏi danh sách cổng COM đang ghi âm
                    if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                    MessageCOMs[sp.PortName] = string.Empty;
                    return;
                }
                UpdateComData(sp.PortName, dto => dto.Message = "Chuyển đổi giọng nói thành văn bản...", "Message");
                // Gửi API để chuyển đổi voice thành text
                using (HttpClient client = new HttpClient())
                {
                    using (var form = new MultipartFormDataContent())
                    {
                        string voiceText = await TranscribeAudioFileAsync(filePath);
                        if (!string.IsNullOrEmpty(voiceText))
                        {
                            string pattern = @"\d{4,6}";
                            Match match = Regex.Match(Regex.Replace(voiceText, @"\s+", "").Trim(), pattern);
                            if (!match.Success)
                            {
                                UpdateComData(sp.PortName, dto => dto.Message = "Lỗi Voice to Text: Voice không thể đọc được mã OTP", "Message");
                                logger.Error("Error Voice to Text: Voice không thể đọc được mã OTP");
                                // Xóa file ghi âm trên RAM
                                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                                // Xóa khỏi danh sách cổng COM đang ghi âm
                                if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                                MessageCOMs[sp.PortName] = string.Empty;
                                return;
                            }
                            MessageCOMs[sp.PortName] = string.Empty;
                            // Cập nhật OTP vào cơ sở dữ liệu
                            var simRepo = new SimRepository();
                            int rowHandle = gvCOM.LocateByValue("Com", sp.PortName);
                            if (rowHandle < 0)
                            {
                                logger.Error($"Error Voice to Text: No row found for COM port: {sp.PortName}");
                                // Xóa file ghi âm trên RAM
                                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                                // Xóa khỏi danh sách cổng COM đang ghi âm
                                if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                                MessageCOMs[sp.PortName] = string.Empty;
                                return;
                            }
                            var phone = gvCOM.GetRowCellValue(rowHandle, "Phone")?.ToString();
                            var iccid = gvCOM.GetRowCellValue(rowHandle, "ICCID")?.ToString();
                            if (string.IsNullOrEmpty(phone))
                            {
                                logger.Error($"Error Voice to Text: Phone is empty, cannot update otp to phone");
                                // Xóa file ghi âm trên RAM
                                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                                // Xóa khỏi danh sách cổng COM đang ghi âm
                                if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                                MessageCOMs[sp.PortName] = string.Empty;
                                return;
                            }
                            var serviceCode = simRepo.UpdateOtpTransaction(String.IsNullOrEmpty(phone) ? iccid : phone, AccountId, voiceText, match.Value);
                            if (string.IsNullOrEmpty(serviceCode))
                                UpdateComData(sp.PortName, dto => dto.Message = "Lỗi Voice to Text: Danh sách sử dụng Voice không bao gồm dịch vụ", "Message");
                            else
                                UpdateComData(sp.PortName, dto => dto.Message = "Chuyển đổi Voice To Text thành công", "Message");
                        }
                        else
                            UpdateComData(sp.PortName, dto => dto.Message = "Lỗi Voice to Text: Voice không thể đọc được mã OTP", "Message");
                    }
                }
                // Xóa file ghi âm trên RAM
                SendATCommand(sp, "AT+QFDEL=\"RAM:record.wav\"", 1000);
                // Xóa khỏi danh sách cổng COM đang ghi âm
                if (RecordingPorts.Contains(sp.PortName)) RecordingPorts.Remove(sp.PortName);
                MessageCOMs[sp.PortName] = string.Empty;
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => dto.Message = $"Lỗi Voice to Text: {ex.Message}", "Message");
                logger.Error($"[{sp.PortName}] - ConvertVoiceToText Error: {ex.Message}");
            }
        }

        public async Task<string> TranscribeAudioFileAsync(string filePath)
        {
            string apiBaseUrl = "http://14.225.207.182:8000";
            string apiKey = "8ad6e7c42d7b65311725b44f3f517b88";
            try
            {
                using (var client = new LuckVoiceToText(apiBaseUrl, apiKey, logger))
                {
                    return await client.TranscribeAudioAsync(filePath);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"TranscribeAudioFileAsync Error: {ex.Message}");
                throw;
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
                else
                    MessageCOMs[sp.PortName] = string.Empty;
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Phone = "PHONE ERROR", "Phone");
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

                    UpdateComData(sp.PortName, dto => dto.TrangThai = "SIM READY", "TrangThai");

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
                    MessageCOMs[sp.PortName] = string.Empty;
                    UpdateComData(sp.PortName, dto => dto.Phone = "PHONE ERROR", "Phone");
                }
                else if (MessageCOMs[sp.PortName].Contains("+CUSD:") && MessageCOMs[sp.PortName].Contains("\nOK"))
                {
                    var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"{PUDCheckBinance}\",15");
                    var phoneStr = mess.Split(',')[1].Replace("\"", "");
                    var phone = Common.GetPhoneNumber(phoneStr);

                    MessageCOMs[sp.PortName] = string.Empty;

                    if (!string.IsNullOrEmpty(phone))
                    {
                        var item = ComDataGrid.FirstOrDefault(dto => dto.Com == sp.PortName);
                        var result = UploadSimToSystem(sp.PortName, phone, item.ICCID.ToString(), false);
                        if (result)
                            UpdateComData(sp.PortName, dto => { dto.Phone = phone; dto.Message = "Đồng bộ SIM lên hệ thống thành công."; }, "Phone", "Message");
                        else
                            UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = "Đồng bộ SIM lên hệ thống thất bại, kiểm tra lại!"; }, "Phone", "Message");
                    }
                    else
                    {
                        UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = string.Empty; }, "Phone", "Message");
                    }
                }
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = ex.Message; }, "Phone", "Message");
                logger.Error($"[{sp.PortName}] - ListenEventPhoneNumber Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Lắng nghe change IMEI
        /// </summary>
        /// <param name="sp"></param>
        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                if (!MessageCOMs[sp.PortName].Contains("AT+EGMR=") || !MessageCOMs[sp.PortName].Contains("\nOK")) return;
                var mess = MessageCOMs[sp.PortName].AT_Command("AT+EGMR=");
                MessageCOMs[sp.PortName] = string.Empty;
                UpdateComData(sp.PortName, dto => dto.Message = "Thay đổi IMEI thành công", "Message");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QSIMSTAT?");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => { dto.ICCID = ""; dto.Message = $"ListenEventChangeIMEI - {ex.Message}"; }, "ICCID", "Message");
                logger.Error($"[{sp.PortName}] - ListenEventChangeIMEI Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Cập nhật thông tin SIM lên hệ thống OTP
        /// </summary>
        /// <param name="portName"></param>
        private bool UploadSimToSystem(string portName, string phone, string iccid, bool simDisable)
        {
            try
            {
                var simRepository = new SimRepository();
                var sim = new Sim()
                {
                    phone_number = phone,
                    iccid = iccid,
                    supplier_id = Guid.Parse(AccountId)
                };
                var services = new Dictionary<string, int>();
                foreach (var sv in ServiceGrid)
                {
                    int value = (!simDisable && ActiveServices.Any(x => x.code == sv.code)) ? 1 : 0;
                    services.Add(sv.code, value);
                }
                var result = simRepository.UpsertSim(sim, simDisable, Country, services);
                return result;
            }
            catch (Exception ex)
            {
                UpdateComData(portName, dto => dto.Message = $"UploadSimToSystem - {ex.Message}", "Message");
                logger.Error($"[{portName}] - UploadSimToSystem Error: {ex.Message}");
                return false;
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
        private void GvCOM_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "Phone")
            {
                var data = gvCOM.GetRowCellValue(e.RowHandle, "Phone")?.ToString();
                if (data == "PHONE ERROR")
                    if (!view.IsRowSelected(e.RowHandle)) e.Appearance.ForeColor = Color.Red;
            }
        }

        /// <summary>
        /// Cập nhật danh sách dịch vụ
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnUpdateService_Click(object sender, EventArgs e)
        {
            var servicers = GridViewServices.GetSelectedRows();
            if (servicers.Length <= 0) XtraMessageBox.Show("Vui lòng chọn dịch vụ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            else
            {
                DialogResult result = XtraMessageBox.Show("Xác nhận cập nhật danh sách dịch vụ mới. Bạn có đồng ý không?", "Cập nhật dịch vụ", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
                if (result == DialogResult.OK)
                {
                    using (StreamWriter writer = new StreamWriter("service.config", false))
                    {
                        foreach (var item in servicers)
                        {
                            var cellValue = GridViewServices.GetRowCellValue(item, "code");
                            writer.WriteLine(cellValue);
                        }
                    }
                    SetupServices();
                    XtraMessageBox.Show("Reset lại phần mềm GSM để dịch vụ được cập nhật!", "Xác nhận", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
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
                    int rowHandle = gvCOM.LocateByValue("Com", portName);
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

        /// <summary>
        /// Mỗi 10s kiểm tra xem Phone nào đang có trạng thái PHONE ERROR thì thử lại
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimerCheckSimError_Tick(object sender, EventArgs e)
        {
            var comErrors = new List<SerialPort>();

            for (int i = 0; i < ComDataGrid.Count; i++)
            {
                var item = ComDataGrid[i];

                if (string.IsNullOrEmpty(item.Phone) || item.Phone == "PHONE ERROR")
                {
                    if (string.IsNullOrEmpty(item.Com)) continue;

                    var com = SerialPorts.Find(x => x.PortName == item.Com);
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

        /// <summary>
        /// Mỗi 2 phút đồng bộ tình trạng SIM Insert lên hệ thống trên Server
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TimeSyncDB_Tick(object sender, EventArgs e)
        {
            try
            {
                var simRepo = new SimRepository();
                var simItems = ComDataGrid.Where(item => !string.IsNullOrEmpty(item.Phone) && item.Phone != "PHONE ERROR" && item.TrangThai == "SIM READY").Select(x => x.Phone).ToList();
                if (simItems.Count > 0) simRepo.UpsertTimeSimInsert(string.Join(",", simItems), AccountId);
                var iccids = string.Join("|", ComDataGrid.Select(x => x.ICCID).ToList());
                var service = simRepo.GsmReportICCD(iccids, AccountId);
                foreach (var item in ServiceGrid)
                {
                    item.count = service.Find(x => x.code == item.code)?.total.ToString() ?? "";
                }
                GridViewServices.RefreshData();
            }
            catch (Exception ex)
            {
                logger.Error($"TimeSyncDB_Tick Error: {ex.Message}");
            }
        }

        private void BtnChangeIMEI_Click(object sender, EventArgs e)
        {
            DialogResult result = XtraMessageBox.Show("Bạn có muốn thay đổi IMEI của tất cả các cổng COM?", "Thay đổi IMEI", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                foreach (var sp in SerialPorts)
                {
                    var newIMEI = Common.GenerateIMEI();
                    Thread thread = new Thread(delegate ()
                    {
                        try
                        {
                            if (!sp.IsOpen) sp.Open();
                            SendATCommand(sp, $"AT+EGMR=1,7,\"{newIMEI}\"");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"[{sp.PortName}] Thay đổi IMEI thất bại: {ex.Message}");
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thread.Start();
                }
            }
        }

        private void BtnUpdateNo_Click(object sender, EventArgs e)
        {
            var dataSource = gvCOM.DataSource as BindingList<ComDto>;
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
                        XtraMessageBox.Show($"Cảnh báo: STT {item.Stt} bị trùng!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

        private void BtnResetNo_Click(object sender, EventArgs e)
        {
            DialogResult result = XtraMessageBox.Show("Bạn có muốn khởi động lại các cổng COM không?", "Khởi động lại COM", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                Properties.Settings.Default.COMs = String.Empty;
                Properties.Settings.Default.Save();
                foreach (var item in ComDataGrid)
                {
                    item.Stt = "-1";
                }
                gvCOM.RefreshData();
            }
        }

        private void BtnResetCom_Click(object sender, EventArgs e)
        {
            DialogResult result = XtraMessageBox.Show("Bạn có muốn khởi động lại các cổng COM không?", "Khởi động lại COM", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
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
                                dto.Phone = "";
                                dto.TrangThai = "";
                                dto.Message = "Khởi động lại cổng COM ...";
                            }, "ICCID", "Phone", "TrangThai", "Message");
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
                            // Reset COM
                            SendATCommand(sp, "AT+CFUN=1,1", 60000);
                            InitializeModem(sp);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"[{sp.PortName}] Reset com error: {ex.Message}");
                        }
                    })
                    {
                        IsBackground = true
                    };
                    thread1.Start();
                }
            }
        }

        private void BtnRefactoryComs_Click(object sender, EventArgs e)
        {
            DialogResult result = XtraMessageBox.Show("Bạn có muốn khôi phục các cổng COM về trạng thái ban đầu không?", "Khôi phục COM", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning);
            if (result == DialogResult.OK)
            {
                foreach (var sp in SerialPorts)
                {
                    Thread thread1 = new Thread(delegate ()
                    {
                        if (!sp.IsOpen) sp.Open();
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.ICCID = "";
                            dto.Phone = "";
                            dto.TrangThai = "";
                            dto.Message = "Khôi phục cổng COM về trạng thái ban đầu...";
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
    }
}