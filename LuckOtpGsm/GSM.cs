using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using LuckOTP.Model;
using LuckOTP.Repositories;
using LuckOTP.Utils;
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

        public GSMForm(string accountId, string country)
        {
            Text = $"LUCK OTP GSM - {Properties.Settings.Default.Username}";
            InitializeComponent();
            AccountId = accountId;
            Country = country;
            LoadCOMForm();
            TimerCheckSimError.Start();
            TimerSyncDB.Start();
        }

        private void LoadCOMForm()
        {
            string[] portNames = SerialPort.GetPortNames();
            var fullPortNames = Common.GetFullPortNames();
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
                    Stt = ComConfigManager.GetOrAssignSTT(sp.PortName, true),
                    Com = sp.PortName,
                    ICCID = "",
                    Phone = "",
                    TrangThai = "",
                    Message = ""
                };
                ComDataGrid.Add(data);
            }
            ComDataGrid = new BindingList<ComDto>(ComDataGrid.OrderBy(c => !string.IsNullOrEmpty(c.Stt) ? int.Parse(c.Stt) : -1).ToList());
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
                logger.Error("Error khi đọc từ SerialPort: " + ex.Message);
            }

            MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine(MessageCOMs[sp.PortName]);

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

            // Lắng nghe trạng thái sim đã sẵn sàng để làm việc chưa?
            ListenEventSIMStatus(sp);

            // Lắng nghe để lấy số serial SIM
            ListenEventICCID(sp);

            // Lắng nghe để lấy thông tin Số điện thoại
            ListenEventPhoneNumber(sp);

            // Lắng nghe change IMEI thành công
            ListenEventChangeIMEI(sp);
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
            var content = MessageCOMs[sp.PortName];
            // Trạng thái SIM đã tháo
            if ((content.Contains("+CPIN: NOT INSERTED") || content.Contains("+CPIN: NOT READY")) && content.Contains("+QSIMSTAT: 1,0"))
            {
                // Xóa các tin nhắn cũ đi
                SendATCommand(sp, $"AT+CMGD=2,4");
                MessageCOMs[sp.PortName] = string.Empty;
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                try
                {
                    // Cập nhật SIM lên hệ thống database
                    UploadSimToSystem(sp.PortName, string.Empty, string.Empty, true);
                    // Cập nhật gridview
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.ICCID = ""; dto.Phone = ""; dto.TrangThai = ""; dto.Message = "";
                    }, "ICCID", "Phone", "TrangThai", "Message");
                }
                catch (Exception ex)
                {
                    logger.Error($"Tháo SIM thất bại: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.TrangThai = "", "TrangThai");
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
                    UpdateComData(sp.PortName, dto => dto.Stt = stt, "Stt");
                    // lấy ICCID của sim
                    SendATCommand(sp, "AT+QCCID");
                }
                catch (Exception ex)
                {
                    logger.Error($"Cắm SIM thất bại: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.TrangThai = "", "TrangThai");
                }
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
                    // Gửi AT lấy số điện thoại
                    SendATCommand(sp, $"AT+CUSD=1,\"*101#\",15");
                }
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
                    // Tin nhắn gửi về từ SMS
                    var mess = MessageCOMs[sp.PortName].AT_Command($"AT+CUSD=1,\"*101#\",15");
                    MessageCOMs[sp.PortName] = string.Empty;
                    //logger.Info($"mess [{sp.PortName}]: {mess}");
                    mess = mess.Substring(mess.IndexOf("+CUSD")).ToLower().Replace("du lieu", " du lieu ");
                    if (mess.Split(',').Length <= 0 && mess.Split('\"').Length <= 1) return;
                    var mess2 = mess.Split('\"')[1];
                    UpdateComData(sp.PortName, dto => dto.Message = mess2, "Message");
                    // Lấy số điện thoại từ tin nhắn gửi về
                    var phoneStr = mess2.Replace("\"", string.Empty).Replace("1. goi", " ");
                    if (string.IsNullOrEmpty(phoneStr)) return;
                    var phone = Common.GetPhoneNumber(phoneStr);
                    if (string.IsNullOrEmpty(phone)) return;
                    MessageCOMs[sp.PortName] = string.Empty;
                    if (!string.IsNullOrEmpty(phone) && !phone.Equals("PHONE ERROR"))
                    {
                        var item = ComDataGrid.FirstOrDefault(dto => dto.Com == sp.PortName);
                        var result = UploadSimToSystem(sp.PortName, phone, item.ICCID.ToString(), false);
                        if (result)
                            UpdateComData(sp.PortName, dto => { dto.Phone = phone; dto.Message = "Đồng bộ SIM lên hệ thống thành công."; }, "Phone", "Message");
                        else
                            UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = "Đồng bộ SIM lên hệ thống thất bại!"; }, "Phone", "Message");
                    }
                    else UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = string.Empty; }, "Phone", "Message");
                }
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName, dto => { dto.Phone = "PHONE ERROR"; dto.Message = $"Đồng bộ SIM lên hệ thống thất bại: {ex.Message}"; }, "Phone", "Message");
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
                var result = simRepository.UpsertSim(sim, simDisable, Country);
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
                var simItems = ComDataGrid.Where(item => !string.IsNullOrEmpty(item.Phone) && item.Phone != "PHONE ERROR").Select(x => x.Phone).ToList();
                if (simItems.Count > 0) simRepo.UpsertTimeSimInsert(string.Join(",", simItems), AccountId);
                var iccids = string.Join("|", ComDataGrid.Select(x => x.ICCID).ToList());
                var service = simRepo.GsmReportICCD(iccids, AccountId);
                if (service.Count > 0)
                {
                    Thread thread = new Thread(() =>
                    {
                        foreach (var item in service)
                        {
                            var comData = ComDataGrid.FirstOrDefault(x => x.Phone.Equals(item.phone_number));
                            if (comData != null) UpdateComData(comData.Com, dto => dto.UseTotal = item.total, "UseTotal");
                        }
                    })
                    { IsBackground = true };
                    thread.Start();
                }
            }
            catch (Exception ex)
            {
                logger.Error($"TimeSyncDB_Tick Error: {ex.Message}");
            }
        }

        private void btnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {

        }

        private void btnResetComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn đặt lại STT cổng COM?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
                if (File.Exists(configPath)) File.Delete(configPath);
                foreach (var item in ComDataGrid)
                {
                    item.Stt = "";
                }
                gvCOM.RefreshData();
            }
        }

        private void barButtonItem1_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                            UpdateComData(sp.PortName, dto => dto.Message = "Đổi IMEI cổng COM...", "Message");
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

        private void barButtonItem3_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                                dto.Phone = string.Empty;
                                dto.TrangThai = string.Empty;
                                dto.Message = "";
                            }, "ICCID", "Phone", "TrangThai","Message");
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

        private void btnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
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
                                dto.Phone = string.Empty;
                                dto.TrangThai = string.Empty;
                                dto.Message = "Khôi phục cài đặt gốc cổng COM";
                            }, "ICCID", "Phone", "TrangThai", "Message");
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
    }
}