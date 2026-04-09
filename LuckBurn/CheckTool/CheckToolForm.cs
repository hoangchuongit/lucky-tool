using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Views.Grid;
using DevExpress.XtraGrid.Views.Grid.ViewInfo;
using LuckBurn.Utils;
using LuckCheck.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LuckBurn
{
    public partial class CheckToolForm : XtraForm
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        // ─── Danh sách cổng COM ───────────────────────────────────────────────
        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();

        // ─── Dữ liệu GridView cổng COM ───────────────────────────────────────
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();

        // ─── Lịch sử tin nhắn từng cổng COM ──────────────────────────────────
        private readonly ConcurrentDictionary<string, string> MessageCOMs
            = new ConcurrentDictionary<string, string>();

        // ─── Lock riêng mỗi cổng - tránh race condition khi đọc/ghi MessageCOMs ──
        private readonly ConcurrentDictionary<string, object> _portLocks
            = new ConcurrentDictionary<string, object>();

        // ─── Hàng đợi xử lý riêng mỗi cổng - tách khỏi ThreadPool ─────
        private readonly ConcurrentDictionary<string, BlockingCollection<byte>> _portQueues
            = new ConcurrentDictionary<string, BlockingCollection<byte>>();

        // ─── Các hàng (row) cần refresh UI - gom batch thay vì từng lần ─
        private readonly ConcurrentDictionary<string, bool> _dirtyRows
            = new ConcurrentDictionary<string, bool>();

        // ─── Timer batch refresh UI 20fps - tránh bão hoà UI message queue ─
        private System.Windows.Forms.Timer _uiRefreshTimer;

        // ─── Guard chống re-entrancy cho TimerCheckSim ─────────────────
        private int _timerCheckRunning = 0;

        
        public CheckToolForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        
        //  KHỞI TẠO
        private void InitializeControls()
        {
            // Khởi động timer batch-refresh UI (50ms = 20fps)
            // Thay vì gọi RefreshRowCell ngay lập tức từ 128 thread,
            // các thread chỉ đánh dấu "dirty", timer này sẽ flush định kỳ.
            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();

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

            // Mỗi cổng có một dedicated thread khởi tạo modem
            foreach (var port in SerialPorts)
            {
                var capturedPort = port; // tránh closure bug
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

                // Khởi tạo lock riêng cho cổng này
                _portLocks.TryAdd(sp.PortName, new object());

                // Khởi tạo hàng đợi xử lý riêng (bounded = 50, tránh tràn RAM)
                var queue = new BlockingCollection<byte>(boundedCapacity: 50);
                _portQueues[sp.PortName] = queue;

                // Dedicated processing thread - KHÔNG dùng ThreadPool
                // → Thread.Sleep trong ListenEvent* không còn cạn kiệt ThreadPool nữa
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
                };
                ComDataGrid.Add(data);
            }

            ComDataGrid = new BindingList<ComDto>(
                ComDataGrid
                    .OrderBy(c => !string.IsNullOrEmpty(c.STT) ? int.Parse(c.STT) : -1)
                    .ToList());
        }

        
        //  DEDICATED PROCESSING THREAD - xử lý tuần tự từng cổng
        /// <summary>
        /// Chạy suốt vòng đời app trên dedicated thread của cổng.
        /// DataReceived chỉ signal vào queue, thread này mới thực sự xử lý.
        /// → Thread.Sleep ở đây KHÔNG ảnh hưởng ThreadPool.
        /// </summary>
        private void ProcessPortQueue(SerialPort sp)
        {
            if (!_portQueues.TryGetValue(sp.PortName, out var queue)) return;

            // GetConsumingEnumerable block thread cho đến khi có item hoặc queue bị đóng
            foreach (var _ in queue.GetConsumingEnumerable())
            {
                try
                {
                    ListenEventSIMStatus(sp);
                    ListenEventICCID(sp);
                    ListenEventPhoneNumber(sp);
                    ListenEventChangeIMEI(sp);
                    ListenEventSmsResponse(sp);
                    // ListenEventCallResponse(sp); // bỏ comment nếu cần
                }
                catch (Exception ex)
                {
                    logger.Error($"[{sp.PortName}] ProcessPortQueue error: {ex.Message}");
                }
            }
        }

        
        //  MODEM
        private void InitializeModem(SerialPort sp)
        {
            try
            {
                if (sp == null) return;
                if (!sp.IsOpen) sp.Open();
                // Khởi động lại modem mà không thay đổi các cài đặt
                SendATCommand(sp, "ATZ");
                // Đưa Baudrate về tốc độ 115200
                SendATCommand(sp, "AT+IPR=115200");
                // Đặt mã ký tự về ASCII
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                // Đặt chế độ quét mạng tự động
                SendATCommand(sp, "AT+QCFG=\"nwscanmode\",0,1");
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

        
        //  SERIAL PORT EVENTS
        /// <summary>
        /// Chạy trên ThreadPool thread của SerialPort.
        /// [CẢI TIẾN] Chỉ đọc dữ liệu + append (có lock) + signal queue.
        /// KHÔNG gọi ListenEvent* trực tiếp ở đây nữa để tránh chiếm ThreadPool.
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

            // Lock tránh race condition khi DataReceived fire đồng thời
            // trên nhiều ThreadPool thread cho cùng một cổng
            lock (_portLocks[sp.PortName])
            {
                MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }

            Console.WriteLine($"{sp.PortName} ---------- {MessageCOMs[sp.PortName]}");

            // Chỉ gửi signal vào queue, KHÔNG xử lý ở đây.
            // TryAdd không block nếu queue đầy — an toàn vì processing thread
            // đang bận xử lý chunk hiện tại và sẽ đọc lại MessageCOMs khi xong.
            if (_portQueues.TryGetValue(sp.PortName, out var queue))
                queue.TryAdd(1);
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] Error: {e.EventType} - {MessageCOMs[sp.PortName]}");

            lock (_portLocks[sp.PortName])
            {
                MessageCOMs[sp.PortName] = string.Empty;
            }

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

        
        //  LISTEN EVENTS - chạy trên dedicated processing thread của cổng
        /// <summary>Khi lấy được status của SIM</summary>
        private void ListenEventSIMStatus(SerialPort sp)
        {
            string content;
            lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

            // Trạng thái SIM đã tháo
            if ((content.Contains("+CPIN: NOT INSERTED") || content.Contains("+CPIN: NOT READY"))
                && content.Contains("+QSIMSTAT: 1,0"))
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
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
                    // Đánh số thứ tự COM nếu SIM chưa được đặt
                    var stt = ComConfigManager.GetOrAssignSTT(sp.PortName);
                    UpdateComData(sp.PortName, dto => dto.STT = stt, "STT");
                    // Lấy ICCID của sim
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
                SendATCommand(sp, "AT+CNMI=2,2,0,1,0");

                // Thread.Sleep ở đây an toàn: đang chạy trên dedicated thread,
                // KHÔNG phải ThreadPool thread
                Thread.Sleep(500);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                // Gửi AT lấy số điện thoại và thông tin tài khoản chính
                SendATCommand(sp, "AT+CUSD=1,\"*101#\",15", 0);
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.ICCID = string.Empty, "ICCID");
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

                var mess = content
                    .AT_Command("AT+CUSD=1,\"*101#\",15")
                    .Replace("AT+CUSD=1,\"*0#\",15", "");

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
                int currentTKC = (int)(tkchinh ?? 0);

                UpdateComData(sp.PortName, dto =>
                {
                    dto.PhoneNumber = phone;
                    dto.HSD = hsd;
                    dto.TKChinh = currentTKC;
                }, "PhoneNumber", "HSD", "TKChinh");
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

        /// <summary>Xử lý khi có tin nhắn SMS đến</summary>
        private void ListenEventSmsResponse(SerialPort sp)
        {
            try
            {
                string content;
                lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

                if (!content.Contains("+CMT:")) return;

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
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = "", "Message101");
            }
        }

        /// <summary>Xử lý khi có cuộc gọi (hiện đang comment)</summary>
        private void ListenEventCallResponse(SerialPort sp)
        {
            try
            {
                // Bỏ comment khi cần bật tính năng nhận cuộc gọi
                // string content;
                // lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }
                // if (content.Contains("RING") && content.Contains("+CLCC:") && content.Contains("1,4,0,0,"))
                // {
                //     var phoneCall = Common.ExtractValidPhoneNumberCall(content);
                //     if (!string.IsNullOrEmpty(phoneCall))
                //     {
                //         SendATCommand(sp, "ATH", 5000);
                //         lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                //         UpdateComData(sp.PortName,
                //             dto => dto.Message101 = $"Số điện thoại gọi đến: {phoneCall}",
                //             "Message101");
                //     }
                // }
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] - ListenEventCallResponse Error: {ex.Message}");
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Lỗi: Nhận cuộc gọi thất bại: {ex.Message}",
                    "Message101");
            }
        }

        
        //  UI HELPERS
        /// <summary>
        /// Timer handler: flush tất cả dirty rows về UI mỗi 50ms.
        /// Gom batch thay vì gọi RefreshRowCell ngay lập tức từ 128 thread.
        /// </summary>
        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_dirtyRows.IsEmpty) return;

            // Lấy snapshot và xoá ngay để không giữ lock lâu
            var dirty = _dirtyRows.Keys.ToList();
            foreach (var key in dirty)
                _dirtyRows.TryRemove(key, out _);

            foreach (var portName in dirty)
            {
                int rowHandle = gvCOM.LocateByValue("COM", portName);
                if (rowHandle >= 0)
                    gvCOM.RefreshRow(rowHandle);
            }
        }

        /// <summary>
        /// [CẢI TIẾN] Background thread chỉ đánh dấu dirty, KHÔNG gọi Invoke ngay.
        /// UI timer sẽ flush định kỳ → tránh bão hoà UI message queue.
        /// Lock _portLocks trước khi thực thi updateAction để tránh concurrent write.
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

                // Đánh dấu row cần refresh; UI timer xử lý sau
                _dirtyRows[portName] = true;
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateComData error: {ex.Message}");
            }
        }


        //  AT COMMAND
        /// <summary>
        /// Gửi AT command và chờ phản hồi.
        /// Thread.Sleep ở đây an toàn khi gọi từ:
        ///   - dedicated processing thread (ProcessPortQueue)
        ///   - init thread (LoadCOMForm)
        ///   - button handler thread (Task.Run trong các event handler)
        /// KHÔNG được gọi trực tiếp từ SerialPort_DataReceived (ThreadPool).
        /// </summary>
        private void SendATCommand(SerialPort sp, string command, int timeout = 1000)
        {
            try
            {
                lock (_portLocks[sp.PortName])
                {
                    MessageCOMs[sp.PortName] = string.Empty;
                }
                sp.WriteLine($"{command}{Environment.NewLine}");
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


        //  TIMER CHECK SIM
        /// <summary>
        /// [CẢI TIẾN] Thêm re-entrancy guard bằng Interlocked.
        /// Nếu lần tick trước chưa xong thì bỏ qua lần tick này,
        /// tránh tích lũy hàng trăm Task.Run chưa hoàn thành.
        /// </summary>
        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            // Guard: nếu đang chạy thì bỏ qua tick này
            if (Interlocked.CompareExchange(ref _timerCheckRunning, 1, 0) != 0) return;

            try
            {
                var fullPortNames = CheckComOnline();
                if (fullPortNames == null) return;

                // ToList() tránh "collection was modified" nếu có thay đổi đồng thời
                foreach (var item in ComDataGrid.ToList())
                {
                    var sp = SerialPorts.Find(x => x.PortName == item.COM);
                    if (sp == null) continue;

                    var capturedItem = item;
                    var capturedSp = sp;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            if (string.IsNullOrEmpty(capturedItem.PhoneNumber))
                            {
                                capturedSp.DiscardInBuffer();
                                capturedSp.DiscardOutBuffer();
                                SendATCommand(capturedSp, "AT+QCCID");
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.ICCID = "COM ERROR";
                                dto.PhoneNumber = "COM ERROR";
                                dto.HSD = "COM ERROR";
                                dto.TKChinh = 0;
                                dto.Message101 =
                                    "COM ERROR. Đảm bảo các cổng COM không có dấu chấm than. " +
                                    "This PC > Manager > Device Manager > Ports (COM & LPT)";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        }
                    });
                }
            }
            finally
            {
                // Luôn release guard dù có exception
                Interlocked.Exchange(ref _timerCheckRunning, 0);
            }
        }


        //  BUTTON / POPUP HANDLERS
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

        private void Popup101_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
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
                            }, "TKChinh", "Message101");
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

        private void PopupResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
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
                                dto.HSD = string.Empty;
                                dto.TKChinh = 0;
                                dto.Message101 = "Reset cổng COM";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                            SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                            // Module được thiết lập để sử dụng chế độ Auto Baud Rate Detection
                            SendATCommand(capturedSp, "AT+IPR=0");
                            // Bật hoặc tắt chức năng Phát hiện thẻ SIM
                            SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                            // Kích hoạt chế độ thông báo sự kiện SIM
                            SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                            // Lưu thay đổi
                            SendATCommand(capturedSp, "AT&W");
                            // Reset COM
                            SendATCommand(capturedSp, "AT+CFUN=1,1", 10000);
                            // Đặt mã ký tự về ASCII
                            SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                            // Đặt chế độ quét mạng tự động
                            SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                            // Đặt module về chế độ Text Mode (ASCII)
                            SendATCommand(capturedSp, "AT+CMGF=1");
                            // Nhận tin nhắn dưới dạng văn bản
                            SendATCommand(capturedSp, "AT+CNMI=2,2");
                            // Lấy ICCID của sim
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
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    var capturedSp = sp;
                    _ = Task.Run(() =>
                    {
                        try
                        {
                            if (!capturedSp.IsOpen) capturedSp.Open();
                            UpdateComData(capturedSp.PortName,
                                dto => dto.Message101 = "Đổi IMEI cổng COM...", "Message101");
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

        private void PopupSao0Thang_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
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
                            }, "TKChinh", "Message101");
                            SendATCommand(capturedSp, "AT+CUSD=2");
                            Thread.Sleep(2000);
                            SendATCommand(capturedSp, "AT+CUSD=1,\"*0#\",15");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void BtnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BindingList<ComDto> dataSource = gvCOM.DataSource as BindingList<ComDto>;
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

            var sortedData = new BindingList<ComDto>(
                newDataSource
                    .OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1)
                    .ToList());
            ComDataGrid.Clear();
            foreach (var item in sortedData) ComDataGrid.Add(item);

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
            gvCOM.RefreshData();
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
                        UpdateComData(capturedSp.PortName,
                            dto => dto.Message101 = "Đổi IMEI cổng COM...", "Message101");
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
                            dto.HSD = string.Empty;
                            dto.TKChinh = 0;
                            dto.Message101 = "Reset cổng COM";
                        }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+IPR=0");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+CFUN=1,1", 10000);
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+CMGF=1");
                        SendATCommand(capturedSp, "AT+CNMI=2,2");
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
                            dto.HSD = string.Empty;
                            dto.TKChinh = 0;
                            dto.Message101 = "Khôi phục cài đặt gốc cổng COM";
                        }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT&F0", 500);
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+IPR=0");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+CMGF=1");
                        SendATCommand(capturedSp, "AT+CNMI=2,2");
                        SendATCommand(capturedSp, "AT+QCCID");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                    }
                });
            }
        }


        //  HELPERS
        private IEnumerable<Dictionary<string, string>> CheckComOnline()
        {
            var fullPortNames = Common.GetFullPortNames();
            var countComsOnline = fullPortNames.Count();
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

        private void CheckToolForm_Load(object sender, EventArgs e)
        {
            Text = $"{Common.Title} - {Application.ProductVersion}";
        }
    }
}