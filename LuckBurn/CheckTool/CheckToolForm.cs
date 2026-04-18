using DevExpress.XtraEditors;
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

        private readonly List<SerialPort> SerialPorts = new List<SerialPort>();
        private BindingList<ComDto> ComDataGrid { get; set; } = new BindingList<ComDto>();
        private readonly ConcurrentDictionary<string, string> MessageCOMs = new ConcurrentDictionary<string, string>();
        private readonly ConcurrentDictionary<string, object> _portLocks = new ConcurrentDictionary<string, object>();
        private readonly ConcurrentDictionary<string, BlockingCollection<byte>> _portQueues = new ConcurrentDictionary<string, BlockingCollection<byte>>();
        private readonly ConcurrentDictionary<string, bool> _dirtyRows = new ConcurrentDictionary<string, bool>();
        private System.Windows.Forms.Timer _uiRefreshTimer;
        private int _timerCheckRunning = 0;
        private const string DataGenUrl = "https://luckburn.mobi/update-app/25";
        private const long DataGenSafetyLimitBytes = 1024L * 1024 * 1024; // giới hạn an toàn 1GB
        private const string UfsChunkFile = "UFS:c.zip";
        private const int ChunkTimeoutMs = 300_000;
        private const int MaxRetryPerChunk = 2;

        private long QueryUfsFreeBytes(SerialPort sp)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write("AT+QFLDS=\"UFS\"\r");
                Thread.Sleep(1500);
                string resp;
                lock (_portLocks[sp.PortName]) { resp = MessageCOMs[sp.PortName]; }

                var m = Regex.Match(resp, @"\+QFLDS:\s*(\d+),\s*(\d+)");
                if (!m.Success)
                {
                    logger.Warn($"[{sp.PortName}] AT+QFLDS parse failed: {resp.Trim()}");
                    return 0;
                }
                long freeBytes = long.Parse(m.Groups[1].Value);
                logger.Info($"[{sp.PortName}] UFS free={freeBytes:N0} B, total={long.Parse(m.Groups[2].Value):N0} B");
                return freeBytes;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] QueryUfsFreeBytes: {ex.Message}");
                return 0;
            }
        }

        private bool SetupHttpContext(SerialPort sp)
        {
            try
            {
                SendATCommand(sp, "AT+QHTTPCFG=\"contextid\",1", 500);
                SendATCommand(sp, "AT+QHTTPCFG=\"requestheader\",0", 500);
                SendATCommand(sp, "AT+QHTTPCFG=\"responseheader\",0", 500);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write("AT+QIACT?\r");
                Thread.Sleep(1500);
                string actResp;
                lock (_portLocks[sp.PortName]) { actResp = MessageCOMs[sp.PortName]; }

                if (actResp.Contains("+QIACT:") && !actResp.Contains("+QIACT: 1,1"))
                {
                    logger.Info($"[{sp.PortName}] PDP context chưa active, kích hoạt...");
                    SendATCommand(sp, "AT+QIACT=1", 10000);
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SetupHttpContext: {ex.Message}");
                return false;
            }
        }

        private bool SetHttpUrl(SerialPort sp, string url)
        {
            try
            {
                int urlLen = Encoding.ASCII.GetByteCount(url);
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QHTTPURL={urlLen},80\r");

                if (!WaitForResponseInCOM(sp.PortName, "CONNECT", 10_000))
                {
                    logger.Warn($"[{sp.PortName}] QHTTPURL: không nhận được CONNECT");
                    return false;
                }

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write(url);

                if (!WaitForResponseInCOM(sp.PortName, "OK", 5_000))
                {
                    logger.Warn($"[{sp.PortName}] QHTTPURL: không nhận được OK");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SetHttpUrl: {ex.Message}");
                return false;
            }
        }

        private void DeleteUfsFile(SerialPort sp, string ufsPath)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QFDEL=\"{ufsPath}\"\r");
                Thread.Sleep(500);
            }
            catch (Exception ex)
            {
                logger.Warn($"[{sp.PortName}] DeleteUfsFile({ufsPath}): {ex.Message}");
            }
        }

        private long DownloadChunk(SerialPort sp, string fileUrl, long rangeStart, long rangeEnd, int chunkIndex)
        {
            try
            {
                DeleteUfsFile(sp, UfsChunkFile);

                if (!SetHttpUrl(sp, fileUrl))
                    return -1;

                long requestedLen = rangeEnd - rangeStart + 1;

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QHTTPGETEX=300,{rangeStart},{requestedLen}\r");

                if (!WaitForResponseInCOM(sp.PortName, "+QHTTPGET:", ChunkTimeoutMs))
                {
                    logger.Warn($"[{sp.PortName}] Đợt {chunkIndex}: timeout chờ +QHTTPGET");
                    return -1;
                }

                string getResp;
                lock (_portLocks[sp.PortName]) { getResp = MessageCOMs[sp.PortName]; }

                var m = Regex.Match(getResp, @"\+QHTTPGET:\s*(\d+)(?:,(\d+)(?:,(\d+))?)?");
                if (!m.Success)
                {
                    logger.Warn($"[{sp.PortName}] Đợt {chunkIndex}: parse +QHTTPGET thất bại: {getResp.Trim()}");
                    return -1;
                }

                int err = int.Parse(m.Groups[1].Value);
                int httpCode = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
                long contentLen = m.Groups[3].Success ? long.Parse(m.Groups[3].Value) : 0;

                if (httpCode == 416 || (err != 0 && getResp.Contains("416")))
                {
                    logger.Info($"[{sp.PortName}] Đợt {chunkIndex}: HTTP 416 → đã sử dụng hết");
                    return -2;
                }

                if (err != 0 || (httpCode != 206 && httpCode != 200))
                {
                    logger.Error($"[{sp.PortName}] Đợt {chunkIndex}: err={err}, httpCode={httpCode}");
                    return -1;
                }

                long received = contentLen > 0 ? contentLen : requestedLen;

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QHTTPREADFILE=\"{UfsChunkFile}\",80\r");

                if (!WaitForResponseInCOM(sp.PortName, "+QHTTPREADFILE: 0", 90_000))
                {
                    string readResp;
                    lock (_portLocks[sp.PortName]) { readResp = MessageCOMs[sp.PortName]; }
                    logger.Error($"[{sp.PortName}] Đợt {chunkIndex}: QHTTPREADFILE lỗi: {readResp.Trim()}");
                    return -1;
                }

                DeleteUfsFile(sp, UfsChunkFile);
                logger.Info($"[{sp.PortName}] Đợt {chunkIndex}: OK {received:N0} bytes (HTTP {httpCode})");
                return received;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] DownloadChunk đợt {chunkIndex}: {ex.Message}");
                return -1;
            }
        }

        private void DownloadFileVia4G(SerialPort sp, string fileUrl = null)
        {
            try
            {
                if (!sp.IsOpen) sp.Open();
                fileUrl = string.IsNullOrWhiteSpace(fileUrl) ? DataGenUrl : fileUrl.Trim();

                UpdateComData(sp.PortName,
                    dto => dto.Message101 = "Đang chuẩn bị sử dụng Data 4G...",
                    "Message101");

                long freeBytes = QueryUfsFreeBytes(sp);
                if (freeBytes <= 0)
                    throw new Exception("Không đọc được dung lượng bộ nhớ module. Kiểm tra kết nối.");

                long chunkSize = (long)(freeBytes * 0.85);
                chunkSize = Math.Min(chunkSize, 2L * 1024 * 1024);
                if (chunkSize < 512 * 1024)
                    throw new Exception($"Bộ nhớ module quá ít ({freeBytes:N0} B). Cần ít nhất 512 KB.");

                double chunkMB = chunkSize / 1024.0 / 1024.0;
                logger.Info($"[{sp.PortName}] Bắt đầu sử dụng 4G: {fileUrl}, đợt ~{chunkMB:F1} MB");

                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Bắt đầu sử dụng Data 4G, mỗi đợt ~{chunkMB:F1} MB...",
                    "Message101");

                if (!SetupHttpContext(sp))
                    throw new Exception("Lỗi cấu hình kết nối 4G");

                long totalConsumed = 0;
                long rangeStart = 0;
                int chunkIndex = 1;

                while (true)
                {
                    long rangeEnd = rangeStart + chunkSize - 1;
                    long requestedLen = rangeEnd - rangeStart + 1;

                    long capturedDown = totalConsumed;
                    int capturedIndex = chunkIndex;
                    UpdateComData(sp.PortName, dto => dto.Message101 = $"Đã sử dụng {capturedDown / 1024.0 / 1024.0:F1} MB", "Message101");

                    long received = -1;
                    for (int attempt = 1; attempt <= MaxRetryPerChunk; attempt++)
                    {
                        received = DownloadChunk(sp, fileUrl, rangeStart, rangeEnd, chunkIndex);
                        if (received >= 0 || received == -2) break;

                        if (attempt < MaxRetryPerChunk)
                        {
                            logger.Warn($"[{sp.PortName}] Đợt {chunkIndex}: thử lại lần {attempt}");
                            UpdateComData(sp.PortName, dto => dto.Message101 = $"Thử lại lần {attempt}...", "Message101");
                            Thread.Sleep(3000);
                        }
                    }

                    if (received == -2)
                    {
                        logger.Info($"[{sp.PortName}] Đợt {chunkIndex}: đã sử dụng hết nguồn");
                        break;
                    }

                    if (received < 0)
                    {
                        throw new Exception($"Đã sử dụng: {totalConsumed / 1024.0 / 1024.0:F1} MB");
                    }

                    totalConsumed += received;

                    if (totalConsumed >= DataGenSafetyLimitBytes)
                        throw new Exception($"Đã đạt giới hạn an toàn {DataGenSafetyLimitBytes / 1024 / 1024} MB.");

                    if (received < requestedLen)
                    {
                        logger.Info($"[{sp.PortName}] Đợt cuối nhận {received:N0} B → kết thúc");
                        break;
                    }

                    rangeStart += received;
                    chunkIndex++;
                }

                double totalMB = totalConsumed / 1024.0 / 1024.0;
                logger.Info($"[{sp.PortName}] Hoàn thành sử dụng Data 4G: {totalMB:F1} MB");
                UpdateComData(sp.PortName, dto => dto.Message101 = $"Kết thúc. Sử dụng hết {totalMB:F1} MB Data 4G", "Message101");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] DownloadFileVia4G Error: {ex.Message}");
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Lỗi sử dụng Data 4G: {ex.Message}",
                    "Message101");
            }
            finally
            {
                try { DeleteUfsFile(sp, UfsChunkFile); } catch { }
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
            }
        }

        private bool WaitForResponseInCOM(string portName, string token, int timeoutMs)
        {
            const int pollMs = 200;
            int elapsed = 0;
            while (elapsed < timeoutMs)
            {
                Thread.Sleep(pollMs);
                elapsed += pollMs;
                string content;
                lock (_portLocks[portName]) { content = MessageCOMs[portName]; }
                if (content.Contains(token)) return true;
            }
            return false;
        }

        private List<SerialPort> GetSelectedOrAllPorts()
        {
            var handles = GridViewCOM.GetSelectedRows();
            if (handles == null || handles.Length == 0)
                return SerialPorts.ToList();

            var result = new List<SerialPort>();
            foreach (int h in handles)
            {
                if (GridViewCOM.GetRow(h) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp != null) result.Add(sp);
                }
            }
            return result.Count > 0 ? result : SerialPorts.ToList();
        }

        public CheckToolForm()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            _uiRefreshTimer = new System.Windows.Forms.Timer { Interval = 50 };
            _uiRefreshTimer.Tick += UiRefreshTimer_Tick;
            _uiRefreshTimer.Start();

            GridViewCOM.OptionsSelection.MultiSelect = true;
            GridViewCOM.OptionsSelection.MultiSelectMode =
                DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CheckBoxRowSelect;

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
                var captured = port;
                new Thread(() =>
                {
                    try { InitializeModem(captured); }
                    catch (Exception ex) { logger.Error($"{captured.PortName} - InitializeModem: {ex.Message}"); }
                })
                { IsBackground = true, Name = $"Init_{port.PortName}" }.Start();
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

                var sp = new SerialPort(port)
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
                _portLocks.TryAdd(sp.PortName, new object());

                var queue = new BlockingCollection<byte>(boundedCapacity: 50);
                _portQueues[sp.PortName] = queue;

                var capturedSp = sp;
                new Thread(() => ProcessPortQueue(capturedSp))
                { IsBackground = true, Name = $"Proc_{sp.PortName}" }.Start();

                ComDataGrid.Add(new ComDto
                {
                    COM = sp.PortName,
                    STT = ComConfigManager.GetOrAssignSTT(sp.PortName, true),
                    ICCID = string.Empty,
                    PhoneNumber = string.Empty,
                    TKChinh = 0,
                    Message101 = string.Empty,
                });
            }

            ComDataGrid = new BindingList<ComDto>(
                ComDataGrid
                    .OrderBy(c => !string.IsNullOrEmpty(c.STT) ? int.Parse(c.STT) : -1)
                    .ToList());
        }

        private void ProcessPortQueue(SerialPort sp)
        {
            if (!_portQueues.TryGetValue(sp.PortName, out var queue)) return;
            foreach (var _ in queue.GetConsumingEnumerable())
            {
                try
                {
                    ListenEventSIMStatus(sp);
                    ListenEventICCID(sp);
                    ListenEventPhoneNumber(sp);
                    ListenEventChangeIMEI(sp);
                    ListenEventSmsResponse(sp);
                }
                catch (Exception ex)
                {
                    logger.Error($"[{sp.PortName}] ProcessPortQueue: {ex.Message}");
                }
            }
        }

        private void InitializeModem(SerialPort sp)
        {
            try
            {
                if (sp == null) return;
                if (!sp.IsOpen) sp.Open();
                SendATCommand(sp, "ATZ");
                SendATCommand(sp, "AT+IPR=115200");
                SendATCommand(sp, "AT+CSCS=\"GSM\"");
                SendATCommand(sp, "AT+QCFG=\"nwscanmode\",0,1");
                SendATCommand(sp, "AT+QSIMDET=1,0");
                SendATCommand(sp, "AT+QSIMSTAT=1");
                SendATCommand(sp, "AT+COLP=1");
                SendATCommand(sp, "AT+CLCC=1");
                SendATCommand(sp, "ATX3");
                SendATCommand(sp, "AT&W");
                SendATCommand(sp, "AT+QCCID");
            }
            catch (Exception ex)
            {
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Lỗi khởi tạo modem: {ex.Message}", "Message101");
            }
        }

        private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
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
                logger.Error($"[{sp.PortName}] Đọc SerialPort lỗi: {ex.Message}");
                return;
            }

            if (bytesRead <= 0) return;

            lock (_portLocks[sp.PortName])
            {
                MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }

#if DEBUG
            Console.WriteLine($"{sp.PortName} --- {MessageCOMs[sp.PortName]}");
#endif

            if (_portQueues.TryGetValue(sp.PortName, out var queue))
                queue.TryAdd(1);
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            var sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] SerialPort Error: {e.EventType}");

            lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
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

        private void ListenEventSIMStatus(SerialPort sp)
        {
            string content;
            lock (_portLocks[sp.PortName]) { content = MessageCOMs[sp.PortName]; }

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
                        dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                        dto.HSD = string.Empty; dto.TKChinh = 0; dto.Message101 = string.Empty;
                    }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                }
                catch (Exception ex)
                {
                    logger.Error($"Tháo SIM: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.Message101 = "Tháo sim thất bại!", "Message101");
                }
            }

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
                    logger.Error($"Cắm SIM: {ex.Message}");
                    UpdateComData(sp.PortName, dto => dto.Message101 = "Cắm sim thất bại!", "Message101");
                }
            }
        }

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
                        .Replace("ATZ", "").Replace("AT+CSCS=\"GSM\"", "")
                        .Replace("AT+QCCID", "").Replace("+QCCID: ", "")
                        .Replace("+QUSIM: 1", "").Substring(0, 20),
                    "ICCID");

                SendATCommand(sp, "AT+CMGF=1");
                SendATCommand(sp, "AT+CNMI=2,2,0,1,0");
                Thread.Sleep(500);
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                SendATCommand(sp, "AT+CUSD=1,\"*101#\",15", 0);
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
                    dto => dto.Message101 = "Thay đổi IMEI thành công. Chờ 5s.", "Message101");
                Thread.Sleep(5000);
                SendATCommand(sp, "AT+QCCID");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = "Thay đổi IMEI thất bại. Thử lại sau 10s.", "Message101");
                Thread.Sleep(10000);
                SendATCommand(sp, "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
            }
        }

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
                if (Common.IsValidUtf16(messData)) messData = Common.DecodeUnicode(messData);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                string hsd = Common.ExtractHanSD(messData);
                if (string.IsNullOrEmpty(hsd))
                    UpdateComData(sp.PortName, dto => dto.Message101 = messData, "Message101");
                else
                    UpdateComData(sp.PortName, dto =>
                    {
                        dto.Message101 = messData; dto.HSD = hsd;
                    }, "Message101", "HSD");
            }
            catch (Exception)
            {
                UpdateComData(sp.PortName, dto => dto.Message101 = "", "Message101");
            }
        }

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

        private void UpdateComData(string portName, Action<ComDto> updateAction,
            params string[] propertyNames)
        {
            try
            {
                var item = ComDataGrid.FirstOrDefault(dto => dto.COM == portName);
                if (item == null) return;
                lock (_portLocks[portName]) { updateAction(item); }
                _dirtyRows[portName] = true;
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateComData: {ex.Message}");
            }
        }

        private void SendATCommand(SerialPort sp, string command, int timeout = 1000)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"{command}\r");
                if (timeout > 0) Thread.Sleep(timeout);
                string response;
                lock (_portLocks[sp.PortName]) { response = MessageCOMs[sp.PortName]; }
                response.AT_Command(command);
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SendATCommand: {ex.Message}");
            }
        }

        private void TimerCheckSim_Tick(object sender, EventArgs e)
        {
            if (Interlocked.CompareExchange(ref _timerCheckRunning, 1, 0) != 0) return;
            try
            {
                var fullPortNames = CheckComOnline();
                if (fullPortNames == null) return;

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
                            logger.Error($"TimerCheckSim [{capturedSp.PortName}]: {ex.Message}");
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.ICCID = "COM ERROR"; dto.PhoneNumber = "COM ERROR";
                                dto.HSD = "COM ERROR"; dto.TKChinh = 0;
                                dto.Message101 = "COM ERROR — Device Manager > Ports (COM & LPT)";
                            }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        }
                    });
                }
            }
            finally
            {
                Interlocked.Exchange(ref _timerCheckRunning, 0);
            }
        }

        private void Popup101_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (var sp in GetSelectedOrAllPorts())
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName,
                            dto => { dto.TKChinh = 0; dto.Message101 = ""; },
                            "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                    }
                    catch (Exception ex) { logger.Error($"[{capturedSp.PortName}] *101#: {ex.Message}"); }
                });
            }
        }

        private void PopupSao0Thang_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (var sp in GetSelectedOrAllPorts())
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName,
                            dto => { dto.TKChinh = 0; dto.Message101 = ""; },
                            "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT+CUSD=2");
                        Thread.Sleep(2000);
                        SendATCommand(capturedSp, "AT+CUSD=1,\"*0#\",15");
                    }
                    catch (Exception ex) { logger.Error($"[{capturedSp.PortName}] *0#: {ex.Message}"); }
                });
            }
        }

        private void BtnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var ports = GetSelectedOrAllPorts();
            string prompt = ports.Count == SerialPorts.Count
                ? $"Khởi động lại toàn bộ {ports.Count} cổng COM?"
                : $"Khởi động lại {ports.Count} cổng COM đã chọn?";
            if (XtraMessageBox.Show(prompt, "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in ports)
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName, dto =>
                        {
                            dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                            dto.HSD = string.Empty; dto.TKChinh = 0; dto.Message101 = "Khởi động lại...";
                        }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+IPR=0");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT+CFUN=1,1", 10000);
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+CMGF=1");
                        SendATCommand(capturedSp, "AT+CNMI=2,2");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+QCCID");
                    }
                    catch (Exception ex) { logger.Error($"[{capturedSp.PortName}] Khởi động lại: {ex.Message}"); }
                });
            }
        }

        private void BtnChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var ports = GetSelectedOrAllPorts();
            string prompt = ports.Count == SerialPorts.Count
                ? $"Thay đổi IMEI toàn bộ {ports.Count} cổng COM?"
                : $"Thay đổi IMEI {ports.Count} cổng COM đã chọn?";
            if (XtraMessageBox.Show(prompt, "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in ports)
            {
                var capturedSp = sp;
                new Thread(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName,
                            dto => dto.Message101 = "Đang thay đổi IMEI...", "Message101");
                        SendATCommand(capturedSp,
                            "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                    }
                    catch (Exception ex) { logger.Error($"[{capturedSp.PortName}] Thay đổi IMEI: {ex.Message}"); }
                })
                { IsBackground = true }.Start();
            }
        }

        private void BtnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var ports = GetSelectedOrAllPorts();
            string prompt = ports.Count == SerialPorts.Count
                ? $"Khôi phục cài đặt gốc toàn bộ {ports.Count} cổng COM?"
                : $"Khôi phục cài đặt gốc {ports.Count} cổng COM đã chọn?";
            if (XtraMessageBox.Show(prompt, "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in ports)
            {
                var capturedSp = sp;
                _ = Task.Run(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName, dto =>
                        {
                            dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                            dto.HSD = string.Empty; dto.TKChinh = 0;
                            dto.Message101 = "Khôi phục cài đặt gốc...";
                        }, "ICCID", "PhoneNumber", "HSD", "TKChinh", "Message101");
                        SendATCommand(capturedSp, "AT&F0", 500);
                        SendATCommand(capturedSp, "AT+QURCCFG=\"urcport\",\"uart1\"");
                        SendATCommand(capturedSp, "AT+IPR=0");
                        SendATCommand(capturedSp, "AT+QSIMDET=1,0");
                        SendATCommand(capturedSp, "AT+QSIMSTAT=1");
                        SendATCommand(capturedSp, "AT+CSCS=\"GSM\"");
                        SendATCommand(capturedSp, "AT+QCFG=\"nwscanmode\",0,1");
                        SendATCommand(capturedSp, "AT+CMGF=1");
                        SendATCommand(capturedSp, "AT+CNMI=2,2");
                        SendATCommand(capturedSp, "AT&W");
                        SendATCommand(capturedSp, "AT+QCCID");
                    }
                    catch (Exception ex) { logger.Error($"[{capturedSp.PortName}] Khôi phục: {ex.Message}"); }
                });
            }
        }

        private void BtnPhatSinhData4G_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var ports = GetSelectedOrAllPorts();
            if (ports.Count == 0)
            {
                XtraMessageBox.Show("Không tìm thấy cổng COM nào.", "Thông báo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            string prompt = ports.Count == SerialPorts.Count
                ? $"Tiêu thụ Data 4G trên tất cả {ports.Count} cổng COM?"
                : $"Tiêu thụ Data 4G trên {ports.Count} cổng COM đã chọn?";
            if (XtraMessageBox.Show(prompt, "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in ports)
            {
                var capturedSp = sp;
                _ = Task.Run(() => DownloadFileVia4G(capturedSp));
            }
        }

        private void BtnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            var dataSource = GridViewCOM.DataSource as BindingList<ComDto>;
            if (dataSource == null) { logger.Error("DataSource là null!"); return; }

            foreach (var item in dataSource)
            {
                var dup = dataSource.FirstOrDefault(x => x != item && x.STT == item.STT);
                if (dup != null && dup.STT != "")
                {
                    MessageBox.Show($"STT {item.STT} bị trùng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var sorted = new BindingList<ComDto>(
                dataSource
                    .OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1)
                    .ToList());
            ComDataGrid.Clear();
            foreach (var item in sorted) ComDataGrid.Add(item);
            XtraMessageBox.Show("Đã cập nhật STT cổng COM", "Thông báo",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void BtnResetComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Đặt lại STT cổng COM?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "com_settings.json");
            if (File.Exists(configPath)) File.Delete(configPath);
            foreach (var item in ComDataGrid) item.STT = "";
            GridViewCOM.RefreshData();
        }

        private IEnumerable<Dictionary<string, string>> CheckComOnline()
        {
            var fullPortNames = Common.GetFullPortNames();
            var countComsOnline = fullPortNames.Count();
            if (countComsOnline == 0)
            {
                TimerCheckSim.Enabled = false;
                XtraMessageBox.Show(
                    "Toàn bộ cổng COM bị lỗi.\nThis PC > Manager > Device Manager > Ports",
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