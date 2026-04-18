using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using LuckBurn.Model;
using LuckBurn.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static LuckBurn.Models.PrefixNumberDto;

namespace LuckBurn
{
    public partial class BurnLandline : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // ─── Danh sách cổng COM ───────────────────────────────────────────────
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        // ─── Dữ liệu GridView cổng COM ───────────────────────────────────────
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        // ─── Lịch sử tin nhắn từng cổng COM ──────────────────────────────────
        private readonly ConcurrentDictionary<string, string> MessageCOMs
            = new ConcurrentDictionary<string, string>();

        // ─── Danh sách cổng COM đang ghi âm ──────────────────────────────────
        private readonly ConcurrentDictionary<string, CallDetail> RecordingPorts
            = new ConcurrentDictionary<string, CallDetail>();

        // ─── Danh sách cổng COM đang gửi SMS ─────────────────────────────────
        private readonly ConcurrentDictionary<string, TranferMoneySMSPort> SMSPorts
            = new ConcurrentDictionary<string, TranferMoneySMSPort>();

        // ─── Nội dung ghi âm của từng cổng COM ───────────────────────────────
        private readonly ConcurrentDictionary<string, byte[]> RecordingCOMs
            = new ConcurrentDictionary<string, byte[]>();

        // ─── CancellationToken để dừng ghi âm sớm nếu có NO CARRIER ─────────
        private readonly ConcurrentDictionary<string, CancellationTokenSource> RecordingTokens
            = new ConcurrentDictionary<string, CancellationTokenSource>();

        // ─── Lock com để tránh timeout khi gửi lệnh AT ───────────────────────
        private readonly ConcurrentDictionary<string, bool> _isCalling
            = new ConcurrentDictionary<string, bool>();

        private readonly ConcurrentDictionary<string, int> _callingSkipCount
            = new ConcurrentDictionary<string, int>();

        private readonly PrefixNumberController _prefixController;
        private readonly string ApiKey;

        // ─── [MỚI] Lock riêng mỗi cổng - tránh race condition MessageCOMs ────
        private readonly ConcurrentDictionary<string, object> _portLocks
            = new ConcurrentDictionary<string, object>();

        // ─── [MỚI] Hàng đợi xử lý riêng mỗi cổng - tách khỏi ThreadPool ─────
        private readonly ConcurrentDictionary<string, BlockingCollection<byte>> _portQueues
            = new ConcurrentDictionary<string, BlockingCollection<byte>>();

        // ─── [MỚI] Dirty rows để batch refresh UI ────────────────────────────
        private readonly ConcurrentDictionary<string, bool> _dirtyRows
            = new ConcurrentDictionary<string, bool>();

        // ─── [MỚI] Timer batch refresh UI 20fps ──────────────────────────────
        private System.Windows.Forms.Timer _uiRefreshTimer;

        // ─── [MỚI] Guard chống re-entrancy cho TimerCheckSim ─────────────────
        private int _timerCheckRunning = 0;

        // ─────────────────────────────────────────────────────────────────────
        public BurnLandline(string apikey)
        {
            InitializeComponent();
            ApiKey = apikey;
            _prefixController = new PrefixNumberController(apikey);
            InitializeControls();
        }

        // ─────────────────────────────────────────────────────────────────────
        //  KHỞI TẠO
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeControls()
        {
            // [MỚI] Timer batch-refresh UI 50ms = 20fps
            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();

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
                var capturedPort = port;
                var thread = new Thread(() =>
                {
                    try { InitializeModem(capturedPort); }
                    catch (Exception ex)
                    {
                        logger.Error($"{capturedPort.PortName} - LoadCOMForm Error: {ex.Message}");
                    }
                })
                { IsBackground = true, Name = $"Init_{port.PortName}" };
                thread.Start();
            }
        }

        private void InitializeSerialPorts(string[] portNames,
            IEnumerable<Dictionary<string, string>> fullPortNames)
        {
            foreach (string port in portNames)
            {
                var regexPattern = $@"\b{Regex.Escape(port)}\b";
                var isValid = fullPortNames.FirstOrDefault(
                    x => Regex.IsMatch(x["Caption"], regexPattern, RegexOptions.IgnoreCase));
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

                // [MỚI] Lock riêng cho cổng này
                _portLocks.TryAdd(sp.PortName, new object());

                // [MỚI] Hàng đợi xử lý riêng (bounded = 50, tránh tràn RAM)
                var queue = new BlockingCollection<byte>(boundedCapacity: 50);
                _portQueues[sp.PortName] = queue;

                // [MỚI] Dedicated processing thread - KHÔNG dùng ThreadPool
                var capturedSp = sp;
                var procThread = new Thread(() => ProcessPortQueue(capturedSp))
                {
                    IsBackground = true,
                    Name = $"Proc_{sp.PortName}"
                };
                procThread.Start();

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
            ComDataGrid = new BindingList<ComDto>(
                ComDataGrid
                    .OrderBy(c => !string.IsNullOrEmpty(c.STT) ? int.Parse(c.STT) : -1)
                    .ToList());
        }

        // ─────────────────────────────────────────────────────────────────────
        //  [MỚI] DEDICATED PROCESSING THREAD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Chạy suốt vòng đời app trên dedicated thread của cổng.
        /// DataReceived chỉ signal vào queue, thread này mới thực sự xử lý.
        /// → Thread.Sleep ở đây KHÔNG ảnh hưởng ThreadPool.
        /// </summary>
        private void ProcessPortQueue(SerialPort sp)
        {
            if (!_portQueues.TryGetValue(sp.PortName, out var queue)) return;
            foreach (var _ in queue.GetConsumingEnumerable())
            {
                try
                {
                    // Xử lý RING (đã bỏ ra khỏi DataReceived vì có SendATCommand)
                    HandleRing(sp);
                    // Kiểm tra hoàn thành ghi âm
                    HandleRecordingComplete(sp);
                    // Các Listen events
                    ListenEventSIMStatus(sp);
                    ListenEventICCID(sp);
                    ListenEventTelecom(sp);
                    ListenEventPhoneNumber(sp);
                    ListenEventChangeIMEI(sp);
                    ListenEventCallPrefix(sp);
                    ListenEventSmsResponse(sp);
                }
                catch (Exception ex)
                {
                    logger.Error($"[{sp.PortName}] ProcessPortQueue error: {ex.Message}");
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  MODEM
        // ─────────────────────────────────────────────────────────────────────

        private void InitializeModem(SerialPort sp)
        {
            try
            {
                if (sp == null) return;
                if (!sp.IsOpen) sp.Open();
                // Khởi động lại modem
                SendATCommand(sp, "ATZ");
                // Đưa Baudrate về tốc độ 115200
                SendATCommand(sp, "AT+IPR=115200");
                // Đặt mã ký tự về ASCII
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                SendATCommand(sp, "AT+QSIMDET=1,0");
                // Kích hoạt chế độ thông báo sự kiện SIM
                SendATCommand(sp, "AT+QSIMSTAT=1");
                // Bật Presentation of Calling Line
                SendATCommand(sp, "AT+COLP=1");
                // Bật báo trạng thái hiện tại của cuộc gọi
                SendATCommand(sp, "AT+CLCC=1");
                // Cấu hình modem báo các mã lỗi cuộc gọi
                SendATCommand(sp, "ATX3");
                // Lưu thay đổi
                SendATCommand(sp, "AT&W");
                // Lấy ICCID của sim
                SendATCommand(sp, "AT+QCCID");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Error InitializeModem: {ex.Message}",
                    "Message101");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  SERIAL PORT EVENTS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [CẢI TIẾN] Chỉ đọc bytes + append (có lock) + cập nhật buffer ghi âm + signal queue.
        /// KHÔNG gọi SendATCommand hay ListenEvent* ở đây (tránh chiếm ThreadPool).
        /// </summary>
        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;

            int bytesRead;
            byte[] buffer;
            try
            {
                int available = sp.BytesToRead;
                if (available <= 0) return;
                buffer = new byte[available];
                bytesRead = sp.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] Error khi đọc SerialPort: {ex.Message}");
                return;
            }

            if (bytesRead <= 0) return;

            // [MỚI] Lock tránh race condition trên MessageCOMs
            lock (_portLocks[sp.PortName])
            {
                MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }

#if DEBUG
            if(sp.PortName== "COM142")
                Console.WriteLine($"{sp.PortName} ---------- {MessageCOMs[sp.PortName]}");
#endif
            // ── Cập nhật buffer ghi âm (thuần memory, không sleep - giữ trong DataReceived) ──
            // Phải xử lý tại đây để không mất byte audio giữa các lần DataReceived fire
            string currentMsg;
            lock (_portLocks[sp.PortName]) { currentMsg = MessageCOMs[sp.PortName]; }

            if (!currentMsg.Contains("+QFDWL:") && currentMsg.Contains("\r\nCONNECT\r\n"))
            {
                lock (_portLocks[sp.PortName])
                {
                    if (RecordingCOMs.ContainsKey(sp.PortName))
                    {
                        if (buffer.Length > 0)
                        {
                            byte[] existing = RecordingCOMs[sp.PortName];
                            byte[] newData = new byte[existing.Length + buffer.Length];
                            Buffer.BlockCopy(existing, 0, newData, 0, existing.Length);
                            Buffer.BlockCopy(buffer, 0, newData, existing.Length, buffer.Length);
                            RecordingCOMs[sp.PortName] = newData;
                        }
                    }
                    else
                    {
                        byte[] cleanBuffer = Common.RemoveConnectHeader(buffer);
                        RecordingCOMs.TryAdd(sp.PortName, cleanBuffer);
                    }
                }
            }

            // [MỚI] Signal queue - xử lý logic nặng trên dedicated thread
            if (_portQueues.TryGetValue(sp.PortName, out var queue))
                queue.TryAdd(1);
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] Error: {e.EventType} - {MessageCOMs[sp.PortName]}");

            lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

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

        // ─────────────────────────────────────────────────────────────────────
        //  HANDLERS CHẠY TRÊN DEDICATED THREAD
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [MỚI] Xử lý RING - đã tách ra khỏi DataReceived vì có SendATCommand(sleep)
        /// </summary>
        private void HandleRing(SerialPort sp)
        {
            string content;
            lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }
            if (!content.Contains("RING")) return;

            SendATCommand(sp, "ATH");
            lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
        }

        /// <summary>
        /// [MỚI] Kiểm tra và lưu file ghi âm khi hoàn thành - tách khỏi DataReceived
        /// </summary>
        private void HandleRecordingComplete(SerialPort sp)
        {
            string content;
            lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }
            if (!content.Contains("+QFDWL:") || !content.Contains("\r\nCONNECT\r\n")) return;

            if (RecordingPorts.ContainsKey(sp.PortName) && RecordingCOMs.ContainsKey(sp.PortName))
                SaveRecord(sp);
        }

        // ─────────────────────────────────────────────────────────────────────
        //  LISTEN EVENTS - chạy trên dedicated thread của cổng
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Khi lấy được status của SIM</summary>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            string content;
            lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

            // Trạng thái SIM đã tháo
            bool simRemoved = ((content.Contains("+CPIN: NOT INSERTED")
                                || content.Contains("+CPIN: NOT READY"))
                               && content.Contains("+QSIMSTAT: 1,0"))
                              || (content == "\0");

            if (simRemoved)
            {
                // Dừng nếu đang có cuộc gọi
                StopCallAndRecord(sp, false);
                // Xóa các tin nhắn cũ đi (Thread.Sleep an toàn trên dedicated thread)
                SendATCommand(sp, "AT+CMGD=2,4");
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.DiscardInBuffer();
                sp.DiscardOutBuffer();
                try
                {
                    if (RecordingPorts.ContainsKey(sp.PortName))
                    {
                        RecordingPorts.TryRemove(sp.PortName, out _);
                        RecordingCOMs.TryRemove(sp.PortName, out _);
                        _isCalling.TryRemove(sp.PortName, out _);
                        _callingSkipCount.TryRemove(sp.PortName, out _);
                    }
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
                    UpdateComData(sp.PortName,
                        dto => dto.Message101 = "Tháo sim thất bại!", "Message101");
                }
            }

            // Trạng thái SIM đã cắm
            if (content.Contains("+CPIN: READY") && content.Contains("+QSIMSTAT: 1,1"))
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                try
                {
                    var stt = ComConfigManager.GetOrAssignSTT(sp.PortName);
                    UpdateComData(sp.PortName, dto => dto.STT = stt, "STT");
                    SendATCommand(sp, "AT+QCCID");
                }
                catch (Exception ex)
                {
                    logger.Error($"Cắm SIM thất bại: {ex.Message}");
                    UpdateComData(sp.PortName,
                        dto => dto.Message101 = "Cắm sim thất bại!", "Message101");
                }
            }
        }

        /// <summary>Khi lấy được thông tin của ICCID</summary>
        private void ListenEventICCID(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (!content.Contains("AT+QCCID") || !content.Contains("\nOK")) return;

                var mess = content.AT_Command("AT+QCCID");
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                UpdateComData(sp.PortName,
                    dto => dto.ICCID = mess
                        .Replace("ATZ", "")
                        .Replace("AT+CSCS=\"GSM\"", "")
                        .Replace("AT+QCCID", "")
                        .Replace("+QCCID: ", "")
                        .Replace("+QUSIM: 1", "")
                        .Substring(0, 20),
                    "ICCID");

                // Đặt module về chế độ Text Mode (ASCII)
                SendATCommand(sp, "AT+CMGF=1");
                // Nhận tin nhắn dưới dạng văn bản
                SendATCommand(sp, "AT+CNMI=2,2");
                // Gửi AT lấy thông tin nhà mạng
                SendATCommand(sp, "AT+COPS?");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
            }
        }

        /// <summary>Khi lấy được thông tin nhà mạng</summary>
        private void ListenEventTelecom(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (!content.Contains("+COPS:") || !content.Contains("\nOK")) return;

                var mess = content.AT_Command("AT+COPS?").ToLower();
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                string provider;
                if (mess.Contains("viettel")) provider = "Viettel";
                else if (mess.Contains("mobifone")) provider = "Mobifone";
                else if (mess.Contains("vinaphone")) provider = "Vinaphone";
                else if (mess.Contains("vietnamobile")) provider = "VietnamMobile";
                else provider = "Other";

                UpdateComData(sp.PortName, dto => dto.Telecom = provider, "Telecom");
                // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                SendATCommand(sp, "AT+CUSD=1,\"*101#\",15", 3000);
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Telecom = "Unknown", "Telecom");
            }
        }

        /// <summary>Khi lấy được thông tin số điện thoại</summary>
        private void ListenEventPhoneNumber(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (!content.Contains("+CUSD:") || !content.Contains("\nOK")) return;
                _ = SmsOrCallWithPrefix(sp);
            }
            catch (Exception ex)
            {
                logger.Error($"Lấy số điện thoại thất bại: {ex.Message}");
            }
        }

        /// <summary>Khi có IMEI thay đổi</summary>
        private void ListenEventChangeIMEI(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (!content.Contains("AT+EGMR=") || !content.Contains("\nOK")) return;

                content.AT_Command("AT+EGMR=");
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                UpdateComData(sp.PortName,
                    dto => dto.Message101 = "Thay đổi IMEI cổng COM thành công. Chờ 5s.",
                    "Message101");

                // Thread.Sleep an toàn trên dedicated thread
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QCCID");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = "Thay đổi IMEI thất bại. thử lại sau 10s.",
                    "Message101");
                Thread.Sleep(10000);
                SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
            }
        }

        /// <summary>Xử lý cuộc gọi đến tổng đài (nhấc máy, ngắt máy,...)</summary>
        private void ListenEventCallPrefix(SerialPort sp)
        {
            try
            {
                string message;
                lock (_portLocks[sp.PortName]) { message = MessageCOMs[sp.PortName]; }

                if (message.Contains("+CLCC:") && message.Contains("0,2,0,0,"))
                {
                    lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                    if (!RecordingPorts.TryGetValue(sp.PortName, out var callDetail)) return;

                    // Mở mic ghi âm
                    SendATCommand(sp, "AT+QAUDRD=1,\"RAM:record.amr\",3");
                    callDetail.start_record = DateTime.Now;

                    // Tạo CancellationToken để dừng khi có NO CARRIER
                    var cts = new CancellationTokenSource();
                    RecordingTokens[sp.PortName] = cts;

                    var capturedSp = sp;
                    var thread = new Thread(() =>
                    {
                        try
                        {
                            int timeoutMs = callDetail.call_duration * 1000;
                            if (cts.Token.WaitHandle.WaitOne(timeoutMs))
                            {
                                logger.Info($"[{capturedSp.PortName}] - Recording canceled (NO CARRIER).");
                                return;
                            }
                            StopCallAndRecord(capturedSp, false);
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"[{capturedSp.PortName}] Thread error: {ex}");
                        }
                        finally
                        {
                            if (RecordingTokens.TryRemove(capturedSp.PortName, out var removedCts))
                            {
                                removedCts.Cancel();
                                removedCts.Dispose();
                            }
                        }
                    })
                    { IsBackground = true };
                    thread.Start();
                }
                else if (message.Contains("NO CARRIER") || message.Contains("HANG UP"))
                {
                    lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                    StopCallAndRecord(sp, true);
                    if (RecordingTokens.TryRemove(sp.PortName, out var removedCts))
                    {
                        removedCts.Cancel();
                        removedCts.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"Error ListenEventCallPrefix: {ex.Message}");
            }
        }

        /// <summary>Xử lý khi có tin phản hồi từ tổng đài SMS</summary>
        private async void ListenEventSmsResponse(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (content.Contains("+CMT: \"7539\"") || content.Contains("+CMT: \"+7539\""))
                {
                    if (content.Contains("Da kich hoat so dien thoai thanh cong"))
                    {
                        lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                        if (SMSPorts.TryGetValue(sp.PortName, out var smsDetailOk))
                        {
                            await _prefixController.UpdateSms(new SmsReq()
                            {
                                history_id = smsDetailOk.history_id.ToString(),
                                prefix = smsDetailOk.prefix,
                                prefix_unit = smsDetailOk.prefix_unit,
                                request_id = smsDetailOk.request_id,
                                status = StatusEnum.SUCCESS.ToString(),
                            });
                        }
                        SMSPorts.TryRemove(sp.PortName, out _);
                        Thread.Sleep(45000);
                        SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                    }
                    else if (content.Contains("Khong the su dung so dien thoai nay"))
                    {
                        lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                        if (SMSPorts.TryGetValue(sp.PortName, out var smsDetailFail))
                        {
                            await _prefixController.UpdateSms(new SmsReq()
                            {
                                history_id = smsDetailFail.history_id.ToString(),
                                prefix = smsDetailFail.prefix,
                                prefix_unit = smsDetailFail.prefix_unit,
                                request_id = smsDetailFail.request_id,
                                status = StatusEnum.FAIL.ToString(),
                            });
                        }
                        SMSPorts.TryRemove(sp.PortName, out _);
                        Thread.Sleep(45000);
                        SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                    }
                }
                else if (content.Contains("+CMT: \"123\"") || content.Contains("+CMT: \"+123\""))
                {
                    var message = content.AT_Command();
                    int startIndex = message.IndexOf("+CMT");
                    if (startIndex == -1) return;

                    var messSplit = message.Substring(startIndex).Split(',');
                    if (messSplit.Length < 3) return;

                    var messContent = string.Join(",", messSplit.Skip(2)).Split('"');
                    if (messContent.Length < 3 || string.IsNullOrEmpty(messContent[2])) return;

                    var messData = messContent[2];
                    var checkUTF16 = Common.IsValidUtf16(messData);
                    if (checkUTF16) messData = Common.DecodeUnicode(messData);

                    lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                    string hsd = Common.ExtractHanSD(messData);
                    if (string.IsNullOrEmpty(hsd))
                        UpdateComData(sp.PortName,
                            dto => dto.Message101 = messData, "Message101");
                    else
                        UpdateComData(sp.PortName, dto =>
                        {
                            dto.Message101 = messData;
                            dto.HSD = hsd;
                        }, "Message101", "HSD");
                }
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName,
                    dto => { dto.Message = "Stop burn"; dto.IsFinish = true; },
                    "Message", "IsFinish");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BUSINESS LOGIC
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>Lấy ra thông tin tổng đài để thực hiện CALL hoặc SMS</summary>
        private async Task SmsOrCallWithPrefix(SerialPort sp)
        {
            int minAccount = int.Parse(txtMinAccountControl.Text.Replace(".", string.Empty));
            int currentTKC = 0;
            try
            {
                string rawContent;
                lock (_portLocks[sp.PortName]) { rawContent = MessageCOMs[sp.PortName]; }

                var mess = rawContent.AT_Command("AT+CUSD=1,\"*101#\",15");
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                mess = mess.Substring(mess.IndexOf("+CUSD")).ToLower()
                           .Replace("du lieu", " du lieu ");
                if (mess.Split(',').Length <= 0 && mess.Split('\"').Length <= 1) return;

                var mess2 = mess.Split('\"')[1];
                UpdateComData(sp.PortName, dto => dto.Message101 = mess2, "Message101");

                var phoneStr = mess2.Replace("\"", string.Empty).Replace("1. goi", " ");
                if (string.IsNullOrEmpty(phoneStr)) return;

                var phone = Common.GetPhoneNumber(phoneStr);
                if (string.IsNullOrEmpty(phone)) return;

                int? tkchinh = Common.ExtractBalance(mess);
                string hsd = Common.ExtractHanSD(mess);
                currentTKC = (int)(tkchinh ?? 0);

                UpdateComData(sp.PortName, dto =>
                {
                    dto.PhoneNumber = phone;
                    dto.TKChinh = currentTKC;
                    dto.HSD = hsd;
                    dto.Message = "Burning ...";
                    dto.IsFinish = false;
                }, "PhoneNumber", "TKChinh", "HSD", "Message", "IsFinish");

                var telecom = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName)?.Telecom ?? "Other";

                if (currentTKC <= minAccount)
                {
                    UpdateComData(sp.PortName,
                        dto => { dto.Message = "Stop burn"; dto.IsFinish = true; },
                        "Message", "IsFinish");
                    return;
                }

                if (RecordingPorts.ContainsKey(sp.PortName))
                {
                    RecordingPorts.TryRemove(sp.PortName, out _);
                    RecordingCOMs.TryRemove(sp.PortName, out _);
                    _isCalling.TryRemove(sp.PortName, out _);
                    _callingSkipCount.TryRemove(sp.PortName, out _);
                }
                if (SMSPorts.ContainsKey(sp.PortName)) SMSPorts.TryRemove(sp.PortName, out _);

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
                    // Dịch vụ đang full, chờ 55s
                    Thread.Sleep(55000);
                    SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                    return;
                }

                if (prefixSmsRes.type == PrefixNumberType.CALL)
                {
                    RecordingPorts.TryAdd(sp.PortName, new CallDetail()
                    {
                        call_duration = prefixSmsRes.duration,
                        prefix = prefixSmsRes.prefix,
                        prefix_unit = prefixSmsRes.prefix_unit,
                        request_id = prefixSmsRes.request_id,
                        history_id = prefixSmsRes.history_id,
                        message = prefixSmsRes.message,
                        start_call = DateTime.Now,
                    });
                    SendATCommand(sp, "AT+QFDEL=\"RAM:record.amr\"", 1000);
                    SendATCommand(sp, $"ATD{prefixSmsRes.prefix};", 1000);
                }
                else if (prefixSmsRes.type == PrefixNumberType.SMS)
                {
                    SMSPorts.TryAdd(sp.PortName, new TranferMoneySMSPort()
                    {
                        prefix = prefixSmsRes.prefix,
                        prefix_unit = prefixSmsRes.prefix_unit,
                        request_id = prefixSmsRes.request_id,
                        history_id = prefixSmsRes.history_id.ToString(),
                        start_time = DateTime.Now
                    });
                    var prefix = prefixSmsRes.prefix;
                    sp.Write($"AT+CMGS=\"{prefix}\"\r");
                    var resp1 = WaitForToken(sp, new[] { ">", "ERROR", "+CMS ERROR" }, 5000);
                    if (!resp1.Contains(">"))
                        throw new Exception("Không nhận được prompt >: " + resp1);
                    sp.Write(prefixSmsRes.message.Trim());
                    sp.Write(new byte[] { 0x1A }, 0, 1);
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] Burn thất bại: {ex.Message}");
                if (currentTKC == 0 || currentTKC <= int.Parse(txtMinAccountControl.Text.Replace(".", string.Empty)))
                {
                    UpdateComData(sp.PortName,
                        dto => { dto.Message = "Stop burn"; dto.IsFinish = true; },
                        "Message", "IsFinish");
                }
                else
                {
                    UpdateComData(sp.PortName,
                        dto => dto.Message = "Restart burn after 30s", "Message");
                    Thread.Sleep(30000);
                    SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                }
            }
        }

        private string WaitForToken(SerialPort sp, string[] tokens, int timeoutMs)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var sb = new StringBuilder();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var data = sp.ReadExisting();
                if (!string.IsNullOrEmpty(data))
                {
                    sb.Append(data);
                    var text = sb.ToString();
                    foreach (var token in tokens)
                        if (text.Contains(token)) return text;
                }
                Thread.Sleep(10);
            }
            return sb.ToString();
        }

        /// <summary>Dừng ghi âm cuộc gọi</summary>
        private async void StopCallAndRecord(SerialPort sp, bool noCarrier)
        {
            try
            {
                if (!RecordingPorts.ContainsKey(sp.PortName)) return;
                var callDetail = RecordingPorts[sp.PortName];
                callDetail.no_carrier = noCarrier;

                if (RecordingTokens.TryRemove(sp.PortName, out var token))
                {
                    token.Cancel();
                    token.Dispose();
                }

                SendATCommand(sp, "ATH");
                SendATCommand(sp, "AT+QAUDRD=0", 1000);
                callDetail.end_record = DateTime.Now;

                await _prefixController.ReleaseSlot(new ReleaseSlotReq()
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
                });

                sp.WriteLine("AT+QFDWL=\"RAM:record.amr\"");
                // Thread.Sleep an toàn trên dedicated thread
                Thread.Sleep(10000);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                if (!callDetail.no_carrier)
                    SendATCommand(sp, "AT+CUSD=1,\"*101#\",15");
                else
                    UpdateComData(sp.PortName,
                        dto => { dto.Message = "Stop burn"; dto.IsFinish = true; },
                        "Message", "IsFinish");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - StopCallAndRecord Error: {ex.Message}");
            }
        }

        /// <summary>Lưu bản ghi âm thành file</summary>
        private async void SaveRecord(SerialPort sp)
        {
            try
            {
                if (!RecordingCOMs.ContainsKey(sp.PortName))
                {
                    logger.Info($"Save Record: RecordingCOMs does not contain: {sp.PortName}");
                    lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                    return;
                }
                var callDetail = RecordingPorts[sp.PortName];
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string recordDir = Path.Combine(baseDir, "record");
                Directory.CreateDirectory(recordDir);
                byte[] audioData = RecordingCOMs[sp.PortName];
                var phone = ComDataGrid.FirstOrDefault(x => x.COM == sp.PortName)?.PhoneNumber
                                    ?? Guid.NewGuid().ToString();
                string filePath = Path.Combine(recordDir,
                    $"{phone}_{callDetail.prefix}_{DateTime.Now:yyyyMMddHHmmss}.amr");

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await fs.WriteAsync(audioData, 0, audioData.Length);
                    await fs.FlushAsync();
                }
                await Task.Delay(1000);
                await _prefixController.ReleaseUploadFile(callDetail.history_id.ToString(), filePath);
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                logger.Error($"[{sp.PortName}] - SaveRecord Error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  UI HELPERS
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>[MỚI] Flush dirty rows lên UI mỗi 50ms - tránh bão hoà message queue</summary>
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_dirtyRows.IsEmpty) return;
            var dirty = _dirtyRows.Keys.ToList();
            foreach (var key in dirty) _dirtyRows.TryRemove(key, out _);
            foreach (var portName in dirty)
            {
                int rowHandle = GridViewCOM.LocateByValue("COM", portName);
                if (rowHandle >= 0) GridViewCOM.RefreshRow(rowHandle);
            }
        }

        /// <summary>
        /// [CẢI TIẾN] Chỉ update data + đánh dấu dirty.
        /// UI timer sẽ flush định kỳ → tránh bão hoà UI message queue.
        /// </summary>
        private void UpdateComData(string portName, Action<ComDto> updateAction,
            params string[] propertyNames)
        {
            try
            {
                var item = ComDataGrid.FirstOrDefault(dto => dto.COM == portName);
                if (item == null) return;
                lock (_portLocks[portName])
                {
                    updateAction(item);
                }
                _dirtyRows[portName] = true;
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateComData error: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  AT COMMAND
        // ─────────────────────────────────────────────────────────────────────

        private void SendATCommand(SerialPort sp, string command, int timeout = 1000)
        {
            try
            {
                lock (_portLocks[sp.PortName])
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                }
                sp.Write($"{command}\r");
                if (timeout > 0) Thread.Sleep(timeout);
                string response;
                lock (_portLocks[sp.PortName])
                {
                    response = MessageCOMs[sp.PortName];
                }
                response.AT_Command(command);
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SendATCommand: {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  TIMER CHECK SIM
        // ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// [CẢI TIẾN] Re-entrancy guard + ToList() + capturedItem/capturedSp
        /// tránh tích lũy Task và closure bug.
        /// </summary>
        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            if (Interlocked.CompareExchange(ref _timerCheckRunning, 1, 0) != 0) return;
            try
            {
                var fullPortNames = CheckComOnline();
                if (fullPortNames == null) return;

                foreach (var item in ComDataGrid.ToList())
                {
                    if (_isCalling.TryGetValue(item.COM, out var calling) && calling)
                    {
                        int skip = _callingSkipCount.AddOrUpdate(item.COM, 1, (_, old) => old + 1);
                        if (skip < 2) continue;
                        logger.Warn($"[{item.COM}] bỏ qua quá 2 lần → cưỡng chế ATH");
                        _callingSkipCount[item.COM] = 0;
                    }

                    var sp = SerialPorts.Find(x => x.PortName == item.COM);
                    if (sp == null) continue;

                    _isCalling[item.COM] = true;

                    var capturedItem = item;
                    var capturedSp = sp;
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            if (string.IsNullOrEmpty(capturedItem.PhoneNumber)
                                || capturedItem.Message101.Trim().ToLower()
                                    .Equals("your input is error or system busy,pls try again!"))
                            {
                                capturedSp.DiscardInBuffer();
                                capturedSp.DiscardOutBuffer();
                                SendATCommand(capturedSp, "AT+QCCID");
                                return;
                            }

                            var sms = SMSPorts.FirstOrDefault(x => x.Key == capturedSp.PortName).Value;
                            if (sms != null)
                            {
                                bool greaterThan310s = (DateTime.Now - sms.start_time).Duration()
                                                       > TimeSpan.FromSeconds(310);
                                if (greaterThan310s)
                                {
                                    SMSPorts.TryRemove(capturedSp.PortName, out _);
                                    capturedSp.DiscardInBuffer();
                                    capturedSp.DiscardOutBuffer();
                                    SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                                    return;
                                }
                            }

                            var call = RecordingPorts.TryGetValue(capturedSp.PortName, out var c) ? c : null;
                            if (call != null)
                            {
                                bool timeout = DateTime.Now - call.start_call
                                               > TimeSpan.FromSeconds(call.call_duration);
                                if (!timeout) return;

                                SendATCommand(capturedSp, "ATH");
                                await Task.Delay(10000);
                                capturedSp.DiscardInBuffer();
                                capturedSp.DiscardOutBuffer();
                                SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.ICCID = "COM ERROR";
                                dto.PhoneNumber = "COM ERROR";
                                dto.TKChinh = 0;
                                dto.HSD = "COM ERROR";
                                dto.Message101 =
                                    "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. " +
                                    "This PC > Manager > Device Manager > Ports (COM & LPT)";
                                dto.Message = "COM ERROR";
                                dto.IsFinish = true;
                                dto.Telecom = "COM ERROR";
                            }, "ICCID", "PhoneNumber", "TKChinh", "HSD",
                               "Message101", "Message", "IsFinish", "Telecom");
                        }
                        finally
                        {
                            _isCalling[capturedItem.COM] = false;
                            _callingSkipCount[capturedItem.COM] = 0;
                        }
                    });
                }
            }
            finally
            {
                Interlocked.Exchange(ref _timerCheckRunning, 0);
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  BUTTON / POPUP HANDLERS
        // ─────────────────────────────────────────────────────────────────────

        private void GvCOM_RowCellStyle(object sender, RowCellStyleEventArgs e)
        {
            GridView view = sender as GridView;
            if (e.Column.FieldName == "Message")
            {
                var val = GridViewCOM.GetRowCellValue(e.RowHandle, "Message")?.ToString();
                if (!view.IsRowSelected(e.RowHandle))
                    e.Appearance.ForeColor = (val == "Stop burn") ? Color.Red : Color.Green;
            }
        }

        private void GvCOM_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                GridView view = sender as GridView;
                GridHitInfo hit = view.CalcHitInfo(e.Location);
                if (hit.InRow || hit.InRowCell)
                {
                    view.FocusedRowHandle = hit.RowHandle;
                    popupMenu1.ShowPopup(MousePosition);
                }
            }
        }

        private void BurnTKForm_Load(object sender, EventArgs e)
        {
            Text = $"{Common.Title} - {Application.ProductVersion}";
        }

        private void BtnStartBurn_Click(object sender, EventArgs e)
        {
            decimal minValue = Convert.ToDecimal(txtMinAccountControl.EditValue);
            Properties.Settings.Default.MinAccount = (int)minValue;
            Properties.Settings.Default.Save();

            var listStop = ComDataGrid
                .Where(dto => dto.IsFinish && !string.IsNullOrEmpty(dto.PhoneNumber))
                .ToList();
            foreach (var port in listStop)
            {
                var sp = SerialPorts.FirstOrDefault(dto => dto.PortName == port.COM);
                if (sp == null) continue;
                var capturedSp = sp;
                var thread = new Thread(() =>
                {
                    try
                    {
                        UpdateComData(capturedSp.PortName,
                            dto => dto.Message = "Burning ...", "Message");
                        SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                    }
                })
                { IsBackground = true };
                thread.Start();
            }
            XtraMessageBox.Show("Đã cập nhật giá trị số tiền cần để lại trên TK Chính",
                "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BindingList<ComDto> dataSource = GridViewCOM.DataSource as BindingList<ComDto>;
            if (dataSource == null)
            {
                logger.Error("Cảnh báo: DataSource là null!");
                return;
            }

            var newDataSource = new BindingList<ComDto>();
            foreach (var item in dataSource)
            {
                var duplicate = dataSource.FirstOrDefault(x => x != item && x.STT == item.STT);
                if (duplicate != null && duplicate.STT != "")
                {
                    MessageBox.Show($"Cảnh báo: STT {item.STT} bị trùng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                newDataSource.Add(item);
            }

            ComDataGrid.Clear();
            var sortedData = new BindingList<ComDto>(
                newDataSource
                    .OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1)
                    .ToList());

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
            if (File.Exists(configPath)) File.Delete(configPath);

            var list = new List<ComConfig>();
            foreach (var item in sortedData)
            {
                ComDataGrid.Add(item);
                if (item.STT == "") continue;
                list.Add(new ComConfig { PortName = item.COM, STT = item.STT });
            }
            ComConfigManager.Save(list);
            XtraMessageBox.Show("Đã cập nhật STT cổng COM", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn đặt lại STT cổng COM?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
            if (File.Exists(configPath)) File.Delete(configPath);
            foreach (var item in ComDataGrid) item.STT = "";
            GridViewCOM.RefreshData();
        }

        private void BtnChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn thay đổi IMEI của tất cả cổng COM?",
                    "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in SerialPorts)
            {
                var capturedSp = sp;
                var thread = new Thread(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName, dto =>
                        {
                            dto.Message101 = "Đổi IMEI cổng COM...";
                            dto.Message = "";
                        }, "Message101", "Message");
                        SendATCommand(capturedSp,
                            "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                    }
                })
                { IsBackground = true };
                thread.Start();
            }
        }

        private void BtnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn reset lại toàn bộ cổng COM?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in SerialPorts)
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName, dto =>
                        {
                            dto.ICCID = string.Empty;
                            dto.PhoneNumber = string.Empty;
                            dto.TKChinh = 0;
                            dto.HSD = string.Empty;
                            dto.Message101 = "Reset cổng COM";
                            dto.Message = "";
                        }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                        SendATCommand(capturedSp, "AT+CFUN=1,1", 15000);
                        SendATCommand(capturedSp, "AT+IPR=115200");
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT+COLP=1");
                        SendATCommand(capturedSp, "AT+CLCC=1");
                        SendATCommand(capturedSp, "ATX3");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+QCCID");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                    }
                });
            }
        }

        private void BtnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Bạn có chắc chắn muốn khôi phục cài đặt gốc?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in SerialPorts)
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName, dto =>
                        {
                            dto.ICCID = string.Empty;
                            dto.PhoneNumber = string.Empty;
                            dto.TKChinh = 0;
                            dto.HSD = string.Empty;
                            dto.Message101 = "Khôi phục cài đặt gốc cổng COM";
                            dto.Message = "";
                        }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                        SendATCommand(capturedSp, "AT&F", 60000);
                        SendATCommand(capturedSp, "AT+IPR=115200");
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT+COLP=1");
                        SendATCommand(capturedSp, "AT+CLCC=1");
                        SendATCommand(capturedSp, "ATX3");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+QCCID");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                    }
                });
            }
        }

        private void PopupResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = GridViewCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (GridViewCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    var capturedSp = sp;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.ICCID = string.Empty;
                                dto.PhoneNumber = string.Empty;
                                dto.TKChinh = 0;
                                dto.HSD = string.Empty;
                                dto.Message101 = "Reset cổng COM";
                                dto.Message = "";
                            }, "ICCID", "PhoneNumber", "TKChinh", "HSD", "Message101", "Message");
                            SendATCommand(capturedSp, "AT+CFUN=1,1", 10000);
                            SendATCommand(capturedSp, "AT+IPR=115200");
                            SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                            SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                            SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                            SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                            SendATCommand(capturedSp, "AT+COLP=1");
                            SendATCommand(capturedSp, "AT+CLCC=1");
                            SendATCommand(capturedSp, "ATX3");
                            SendATCommand(capturedSp, "AT&W");
                            SendATCommand(capturedSp, "AT+QCCID");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = GridViewCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (GridViewCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    var capturedSp = sp;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.Message101 = "Đổi IMEI cổng COM...";
                                dto.Message = "";
                            }, "Message101", "Message");
                            SendATCommand(capturedSp,
                                "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupBurn_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = GridViewCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (GridViewCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    var capturedSp = sp;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.TKChinh = 0;
                                dto.Message101 = "";
                                dto.Message = string.Empty;
                            }, "TKChinh", "Message101", "Message");
                            SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  HELPERS
        // ─────────────────────────────────────────────────────────────────────

        private IEnumerable<Dictionary<string, string>> CheckComOnline()
        {
            var fullPortNames = Common.GetFullPortNames();
            var countComsOnline = fullPortNames.Count();
            BarPCCom.Caption =
                $"PC COMs Online: <b><size=12><color=green>{countComsOnline}</color></size></b>";
            if (countComsOnline == 0)
            {
                TimerCheckSim.Enabled = false;
                XtraMessageBox.Show(
                    "Toàn bộ cổng COM bị lỗi. Đảm bảo các cổng COM không có dấu chấm than.\n" +
                    "This PC > Manager > Device Manager > Ports (COM & LPT)",
                    "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Application.Exit();
                return null;
            }
            return fullPortNames;
        }

        private async void LoadNotification()
        {
            var notification = await _prefixController.GetNotification();
            TxtNotification.Caption = notification;
        }

        private void TimerNotification_Tick(object sender, EventArgs e) => LoadNotification();

        private void BtnTotalRevenue_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
            => new ReportTotal(ApiKey).Show();

        private void BtnDoanhThu_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
            => new Report(ApiKey).Show();

        private void BtnDoiMatKhau_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
            => new ChangePassForm(ApiKey).ShowDialog();

        private void BtnUserInfor_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
            => new UserInforForm(ApiKey).ShowDialog();
    }
}