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

        // ─── Lock riêng mỗi cổng ─────────────────────────────────────────────
        private readonly ConcurrentDictionary<string, object> _portLocks
            = new ConcurrentDictionary<string, object>();

        // ─── Hàng đợi xử lý riêng mỗi cổng ──────────────────────────────────
        private readonly ConcurrentDictionary<string, BlockingCollection<byte>> _portQueues
            = new ConcurrentDictionary<string, BlockingCollection<byte>>();

        // ─── Dirty rows để batch refresh UI ──────────────────────────────────
        private readonly ConcurrentDictionary<string, bool> _dirtyRows
            = new ConcurrentDictionary<string, bool>();

        // ─── Timer batch refresh UI ───────────────────────────────────────────
        private System.Windows.Forms.Timer _uiRefreshTimer;

        // ─── Guard chống re-entrancy cho TimerCheckSim ────────────────────────
        private int _timerCheckRunning = 0;

        // ═════════════════════════════════════════════════════════════════════
        //  PHÁT SINH DATA 4G — Quectel EC20 HTTP Range Chunked Download
        // ═════════════════════════════════════════════════════════════════════
        //
        //  PHÂN TÍCH HARDWARE (Quectel EC20, chip MDM9607):
        //  ─────────────────────────────────────────────────
        //  • UFS (User File System): tổng ~5.37 MB, free ~2.22 MB
        //    (từ AT+QFLDS="UFS" → +QFLDS: 2326528,5636096)
        //  • File cần tải: 25 MB → KHÔNG thể lưu toàn bộ vào UFS
        //
        //  GIẢI PHÁP: HTTP Range Chunked Download
        //  ─────────────────────────────────────────────────
        //  • AT+QHTTPGETFILE tải từ 4G thẳng vào UFS (không qua UART):
        //    - Tốc độ tải = tốc độ 4G thực tế (LTE Cat-4 = 150Mbps peak)
        //    - Thực tế EC20: ~20–50 Mbps → 2MB chunk ≈ 0.3–0.8s/chunk
        //    - UART chỉ nhận URC "+QHTTPGETFILE: 0" khi xong → không bị bottleneck
        //  • Dùng HTTP Range header để chia 25MB thành nhiều chunk ~1.8MB
        //  • Mỗi chunk: tải → xoá UFS → chunk tiếp theo
        //  • AT+QHTTPCFG="requestheader",1 cho phép gửi header tùy chỉnh
        //  • Tổng ~14 chunk × ~0.8s = ~12 giây/cổng COM
        //  • 128 cổng chạy song song → Task.Run riêng, không block nhau
        //
        //  CHUỖI AT COMMAND PER CHUNK:
        //  ─────────────────────────────────────────────────
        //  [Setup 1 lần]
        //  AT+QHTTPCFG="contextid",1          → OK
        //  AT+QHTTPCFG="requestheader",1      → OK  (bật custom header)
        //  AT+QHTTPCFG="responseheader",0     → OK
        //  AT+QHTTPCFG="sslctxid",1           → OK
        //  AT+QSSLCFG="ignorelocaltime",1,1   → OK  (bỏ qua cert time)
        //  AT+QSSLCFG="sslversion",1,4        → OK  (TLS 1.2)
        //  AT+QSSLCFG="ciphersuite",1,0xFFFF  → OK  (all ciphersuites)
        //  AT+QIACT? → if not "+QIACT: 1,1" → AT+QIACT=1
        //  AT+QFLDS="UFS" → parse freeBytes → compute chunkSize
        //
        //  [Per chunk i, rangeStart=i*chunkSize, rangeEnd=min(...,fileSize-1)]
        //  AT+QFDEL="UFS:c.zip"               → OK hoặc ERROR (bỏ qua)
        //  AT+QHTTPURL=<urlLen>,80            → CONNECT
        //  → gửi URL string
        //                                     → OK
        //  AT+QHTTPGETFILE="UFS:c.zip",300   → CONNECT  (prompt nhập header)
        //  → gửi:
        //    "GET /path HTTP/1.1\r\n"
        //    "Host: hostname\r\n"
        //    "Range: bytes={start}-{end}\r\n"
        //    "Connection: close\r\n"
        //    "\r\n"
        //                                     → +QHTTPGETFILE: 0,206,chunkLen
        //  AT+QFDEL="UFS:c.zip"               → OK
        // ═════════════════════════════════════════════════════════════════════

        private const string DataGenUrl = "https://gmeta.io.vn/update-app/LuckTools_GMeta.zip";
        private const string DataGenHost = "gmeta.io.vn";
        private const string DataGenPath = "/update-app/LuckTools_GMeta.zip";
        private const long DataGenTargetBytes = 25L * 1024 * 1024; // 25 MB
        private const string UfsChunkFile = "UFS:c.zip";       // Tên ngắn, tiết kiệm UFS metadata
        private const int ChunkTimeoutMs = 300_000;           // 300s timeout mỗi chunk
        private const int MaxRetryPerChunk = 2;                 // Retry tối đa mỗi chunk

        // ─────────────────────────────────────────────────────────────────────
        //  [1] Query UFS free space từ EC20
        //  Trả về số bytes còn trống, hoặc 0 nếu lỗi.
        // ─────────────────────────────────────────────────────────────────────
        private long QueryUfsFreeBytes(SerialPort sp)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write("AT+QFLDS=\"UFS\"\r");
                Thread.Sleep(1500);
                string resp;
                lock (_portLocks[sp.PortName]) { resp = MessageCOMs[sp.PortName]; }

                // Response: +QFLDS: freeBytes,totalBytes
                // Ví dụ: +QFLDS: 2326528,5636096
                var m = Regex.Match(resp, @"\+QFLDS:\s*(\d+),\s*(\d+)");
                if (!m.Success)
                {
                    logger.Warn($"[{sp.PortName}] AT+QFLDS parse failed: {resp.Trim()}");
                    return 0;
                }
                long freeBytes = long.Parse(m.Groups[1].Value);
                long totalBytes = long.Parse(m.Groups[2].Value);
                logger.Info($"[{sp.PortName}] UFS: free={freeBytes:N0} B, total={totalBytes:N0} B");
                return freeBytes;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] QueryUfsFreeBytes: {ex.Message}");
                return 0;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  [2] Cấu hình HTTP context và kích hoạt kết nối 4G
        //  Trả về true nếu thành công.
        // ─────────────────────────────────────────────────────────────────────
        private bool SetupHttpContext(SerialPort sp)
        {
            try
            {
                // HTTP configuration
                SendATCommand(sp, "AT+QHTTPCFG=\"contextid\",1", 500);
                SendATCommand(sp, "AT+QHTTPCFG=\"requestheader\",1", 500); // ← bật custom header
                SendATCommand(sp, "AT+QHTTPCFG=\"responseheader\",0", 500);
                SendATCommand(sp, "AT+QHTTPCFG=\"sslctxid\",1", 500);

                // SSL: EC20 thường không sync thời gian → ignorelocaltime bắt buộc
                SendATCommand(sp, "AT+QSSLCFG=\"ignorelocaltime\",1,1", 500);
                // TLS 1.2 (giá trị 4 = TLS1.1+1.2, giá trị 6 = TLS1.2 only)
                SendATCommand(sp, "AT+QSSLCFG=\"sslversion\",1,4", 500);
                // Tất cả ciphersuites → tăng khả năng kết nối thành công
                SendATCommand(sp, "AT+QSSLCFG=\"ciphersuite\",1,0xFFFF", 500);

                // Kiểm tra PDP context đã active chưa
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write("AT+QIACT?\r");
                Thread.Sleep(2000);
                string actResp;
                lock (_portLocks[sp.PortName]) { actResp = MessageCOMs[sp.PortName]; }

                if (!actResp.Contains("+QIACT: 1,1"))
                {
                    // Chưa active → kích hoạt (có thể mất 5–10s lần đầu)
                    logger.Info($"[{sp.PortName}] Activating PDP context...");
                    SendATCommand(sp, "AT+QIACT=1", 10000);

                    // Verify lại
                    lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                    sp.Write("AT+QIACT?\r");
                    Thread.Sleep(2000);
                    lock (_portLocks[sp.PortName]) { actResp = MessageCOMs[sp.PortName]; }
                    if (!actResp.Contains("+QIACT: 1,1"))
                    {
                        logger.Error($"[{sp.PortName}] PDP context không active: {actResp.Trim()}");
                        return false;
                    }
                }

                logger.Info($"[{sp.PortName}] HTTP context ready.");
                return true;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] SetupHttpContext: {ex.Message}");
                return false;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  [3] Set URL vào session HTTP của EC20
        //  Cần gọi lại trước mỗi AT+QHTTPGETFILE vì Connection: close
        //  làm đứt TCP sau mỗi chunk.
        //  Trả về true nếu thành công.
        // ─────────────────────────────────────────────────────────────────────
        private bool SetHttpUrl(SerialPort sp)
        {
            try
            {
                int urlLen = Encoding.ASCII.GetByteCount(DataGenUrl);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QHTTPURL={urlLen},80\r");

                // EC20 phản hồi "CONNECT" để nhận URL string
                if (!WaitForResponseInCOM(sp.PortName, "CONNECT", 10_000))
                {
                    logger.Warn($"[{sp.PortName}] AT+QHTTPURL: không nhận được CONNECT prompt");
                    return false;
                }

                // Gửi URL (không cần \r hay \n ở cuối)
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write(DataGenUrl);

                // Chờ OK
                if (!WaitForResponseInCOM(sp.PortName, "OK", 5_000))
                {
                    logger.Warn($"[{sp.PortName}] AT+QHTTPURL: không nhận được OK");
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

        // ─────────────────────────────────────────────────────────────────────
        //  [4] Xoá file trên UFS (bỏ qua lỗi nếu file không tồn tại)
        // ─────────────────────────────────────────────────────────────────────
        private void DeleteUfsFile(SerialPort sp, string ufsPath)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QFDEL=\"{ufsPath}\"\r");
                Thread.Sleep(500);
                // Không cần check kết quả — ERROR nếu file không tồn tại là OK
            }
            catch (Exception ex)
            {
                logger.Warn($"[{sp.PortName}] DeleteUfsFile({ufsPath}): {ex.Message}");
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  [5] Download một chunk bằng HTTP Range request
        //
        //  Trả về:
        //    > 0  → số bytes thực tế nhận được (thành công)
        //    -2   → HTTP 416 Range Not Satisfiable (đã tải hết file)
        //    -1   → lỗi khác (timeout, lỗi mạng, ...)
        // ─────────────────────────────────────────────────────────────────────
        private long DownloadChunk(SerialPort sp, long rangeStart, long rangeEnd,
            int chunkIndex, int totalChunks)
        {
            try
            {
                // Xoá file cũ trên UFS (giải phóng chỗ) trước khi tải chunk mới
                DeleteUfsFile(sp, UfsChunkFile);

                // Set URL (cần mỗi chunk vì Connection: close làm đứt TCP)
                if (!SetHttpUrl(sp))
                    return -1;

                // Phát lệnh tải file
                // EC20 response: CONNECT → user gửi HTTP request headers → +QHTTPGETFILE: err,...
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write($"AT+QHTTPGETFILE=\"{UfsChunkFile}\",300\r");

                // Chờ CONNECT prompt (EC20 sẵn sàng nhận HTTP request headers)
                if (!WaitForResponseInCOM(sp.PortName, "CONNECT", 15_000))
                {
                    logger.Warn($"[{sp.PortName}] Chunk {chunkIndex}/{totalChunks}: " +
                                "không nhận được CONNECT từ AT+QHTTPGETFILE");
                    return -1;
                }

                // Gửi HTTP GET request với Range header
                // Format: method SP path SP version CRLF *(header CRLF) CRLF
                string httpRequest =
                    $"GET {DataGenPath} HTTP/1.1\r\n" +
                    $"Host: {DataGenHost}\r\n" +
                    $"Range: bytes={rangeStart}-{rangeEnd}\r\n" +
                    "User-Agent: EC20HTTP/1.0\r\n" +
                    "Connection: close\r\n" +
                    "\r\n";

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.Write(httpRequest);

                // Chờ kết quả URC (tối đa 300s — chunk 2MB ở tốc độ tối thiểu 1Mbps ≈ 16s)
                if (!WaitForResponseInCOM(sp.PortName, "+QHTTPGETFILE:", ChunkTimeoutMs))
                {
                    logger.Warn($"[{sp.PortName}] Chunk {chunkIndex}/{totalChunks}: timeout 300s");
                    return -1;
                }

                string result;
                lock (_portLocks[sp.PortName]) { result = MessageCOMs[sp.PortName]; }

                // Parse URC: +QHTTPGETFILE: <err>[,<httpCode>[,<contentLen>]]
                // Ví dụ thành công:  +QHTTPGETFILE: 0,206,1800000
                // Range exceeded:    +QHTTPGETFILE: 720,416    (err=720 → HTTP 4xx)
                // hoặc:              +QHTTPGETFILE: 0,416      (một số firmware khác)
                var m = Regex.Match(result,
                    @"\+QHTTPGETFILE:\s*(\d+)(?:,(\d+)(?:,(\d+))?)?");
                if (!m.Success)
                {
                    logger.Warn($"[{sp.PortName}] Chunk {chunkIndex}: parse URC thất bại: " +
                                result.Trim());
                    return -1;
                }

                int err = int.Parse(m.Groups[1].Value);
                int httpCode = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
                long contentLen = m.Groups[3].Success ? long.Parse(m.Groups[3].Value) : 0;

                // Xoá file ngay sau khi tải để giải phóng UFS cho chunk tiếp theo
                DeleteUfsFile(sp, UfsChunkFile);

                // ── Xử lý kết quả ─────────────────────────────────────────
                // HTTP 206 Partial Content → thành công
                if (err == 0 && (httpCode == 206 || httpCode == 200))
                {
                    // contentLen = 0 có thể xảy ra với một số firmware → ước lượng từ range
                    long received = contentLen > 0 ? contentLen : (rangeEnd - rangeStart + 1);
                    logger.Info($"[{sp.PortName}] Chunk {chunkIndex}/{totalChunks}: " +
                                $"OK {received:N0} bytes (HTTP {httpCode})");
                    return received;
                }

                // HTTP 416 Range Not Satisfiable → đã tải hết file
                if (httpCode == 416 || (err != 0 && result.Contains("416")))
                {
                    logger.Info($"[{sp.PortName}] Chunk {chunkIndex}: HTTP 416 → file đã hết");
                    return -2; // Signal "all done"
                }

                // Lỗi khác
                logger.Error($"[{sp.PortName}] Chunk {chunkIndex}: err={err}, " +
                             $"httpCode={httpCode}, resp={result.Trim()}");
                return -1;
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] DownloadChunk {chunkIndex}: {ex.Message}");
                return -1;
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  [6] Main: Phát sinh Data 4G qua HTTP Range chunked download
        //
        //  Luồng thực thi:
        //    1. Query UFS free → tính chunk size an toàn (90% free)
        //    2. Cấu hình HTTP/SSL/PDP
        //    3. Vòng lặp tải chunk: download → xoá UFS → chunk tiếp
        //    4. Retry tự động nếu chunk lỗi (tối đa MaxRetryPerChunk lần)
        //    5. Cập nhật progress real-time lên cột "Tin nhắn"
        //
        //  Ghi chú quan trọng:
        //    - KHÔNG dùng AT+QHTTPGET (stream qua UART) vì UART 115200 = 11KB/s
        //      → tải 25MB mất ~37 phút/cổng → không khả thi
        //    - AT+QHTTPGETFILE lưu vào UFS ở tốc độ 4G thực tế (không bị UART bottleneck)
        //    - Mỗi cổng chạy Task.Run riêng → 128 cổng tải song song
        // ─────────────────────────────────────────────────────────────────────
        private void DownloadFileVia4G(SerialPort sp)
        {
            try
            {
                if (!sp.IsOpen) sp.Open();

                UpdateComData(sp.PortName,
                    dto => dto.Message101 = "Đang chuẩn bị phát sinh...",
                    "Message101");

                // ── Bước 1: Query UFS free space ─────────────────────────────
                long freeBytes = QueryUfsFreeBytes(sp);
                if (freeBytes <= 0)
                    throw new Exception("Không đọc được dung lượng UFS. Kiểm tra kết nối.");

                // Chunk size = 90% free space (10% buffer cho filesystem overhead của UFS)
                // Thực tế +QFLDS free đã trừ metadata, nhưng vẫn cần buffer nhỏ
                long chunkSize = (long)(freeBytes * 0.90);
                if (chunkSize < 512 * 1024) // Tối thiểu 512KB
                    throw new Exception($"UFS quá ít chỗ ({freeBytes:N0} B). Cần ít nhất 512KB.");

                // Tổng số chunk cần tải
                int totalChunks = (int)Math.Ceiling((double)DataGenTargetBytes / chunkSize);
                long targetMB = DataGenTargetBytes / 1024 / 1024;

                logger.Info($"[{sp.PortName}] 4G download: target={targetMB}MB, " +
                            $"chunkSize={chunkSize:N0}B, chunks={totalChunks}");

                UpdateComData(sp.PortName,
                    dto => dto.Message101 =
                        $"Phát sinh {targetMB}MB: {totalChunks} chunks × " +
                        $"{chunkSize / 1024 / 1024.0:F1}MB",
                    "Message101");

                // ── Bước 2: Cấu hình HTTP context ────────────────────────────
                if (!SetupHttpContext(sp))
                    throw new Exception("Lỗi cấu hình HTTP/SSL context");

                // ── Bước 3: Vòng lặp tải chunk ───────────────────────────────
                long totalDownloaded = 0;

                for (int i = 0; i < totalChunks; i++)
                {
                    long rangeStart = (long)i * chunkSize;
                    long rangeEnd = Math.Min(rangeStart + chunkSize - 1,
                                               DataGenTargetBytes - 1);

                    // Update progress trước khi bắt đầu chunk
                    int capturedI = i;
                    int capturedTotal = totalChunks;
                    long capturedDown = totalDownloaded;
                    UpdateComData(sp.PortName, dto =>
                        dto.Message101 =
                            $"Đang phát sinh ({capturedI + 1}/{capturedTotal}) " +
                            $"{capturedDown / 1024.0 / 1024.0:F1}/{targetMB} MB",
                        "Message101");

                    // Thử tải chunk, retry nếu thất bại
                    long received = -1;
                    for (int attempt = 1; attempt <= MaxRetryPerChunk; attempt++)
                    {
                        received = DownloadChunk(sp, rangeStart, rangeEnd,
                                                 i + 1, totalChunks);
                        if (received >= 0 || received == -2)
                            break; // thành công hoặc range exceeded

                        if (attempt < MaxRetryPerChunk)
                        {
                            logger.Warn($"[{sp.PortName}] Chunk {i + 1}: retry {attempt}/{MaxRetryPerChunk - 1}");
                            UpdateComData(sp.PortName,
                                dto => dto.Message101 =
                                    $"Đang phát sinh ({capturedI + 1}/{capturedTotal}) " +
                                    $"— retry {attempt}...",
                                "Message101");
                            Thread.Sleep(3000);
                        }
                    }

                    // Xử lý kết quả chunk
                    if (received == -2)
                    {
                        // HTTP 416: server báo hết range → dừng sớm
                        logger.Info($"[{sp.PortName}] Server báo hết dữ liệu ở chunk {i + 1}");
                        break;
                    }

                    if (received < 0)
                    {
                        // Thất bại sau MaxRetryPerChunk lần
                        throw new Exception(
                            $"Chunk {i + 1}/{totalChunks} thất bại sau {MaxRetryPerChunk} lần thử. " +
                            $"Đã phát sinh: {totalDownloaded / 1024.0 / 1024.0:F1} MB");
                    }

                    totalDownloaded += received;

                    // Nếu received nhỏ hơn kích thước yêu cầu → đây là chunk cuối
                    if (received < (rangeEnd - rangeStart + 1))
                    {
                        logger.Info($"[{sp.PortName}] Chunk cuối nhận được {received:N0} B " +
                                    $"< {rangeEnd - rangeStart + 1:N0} B → kết thúc");
                        break;
                    }
                }

                // ── Bước 4: Kết quả cuối ─────────────────────────────────────
                double downloadedMB = totalDownloaded / 1024.0 / 1024.0;
                logger.Info($"[{sp.PortName}] Phát sinh Data 4G hoàn thành: {downloadedMB:F1} MB");

                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Đã phát sinh Data 4G ({downloadedMB:F1} MB)",
                    "Message101");
            }
            catch (Exception ex)
            {
                logger.Error($"[{sp.PortName}] DownloadFileVia4G Error: {ex.Message}");
                UpdateComData(sp.PortName,
                    dto => dto.Message101 = $"Lỗi phát sinh 4G: {ex.Message}",
                    "Message101");
            }
            finally
            {
                // Dọn dẹp:
                // 1. Tắt requestheader mode để không ảnh hưởng AT command khác
                // 2. Xoá file tạm nếu còn tồn tại
                // 3. Clear buffer
                try
                {
                    DeleteUfsFile(sp, UfsChunkFile);
                    SendATCommand(sp, "AT+QHTTPCFG=\"requestheader\",0", 500);
                }
                catch { /* ignore cleanup errors */ }

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
            }
        }

        // ─────────────────────────────────────────────────────────────────────
        //  Helper: Chờ token xuất hiện trong MessageCOMs
        //  Poll mỗi 300ms (cân bằng: không quá tốn CPU, không bỏ lỡ token)
        // ─────────────────────────────────────────────────────────────────────
        private bool WaitForResponseInCOM(string portName, string token, int timeoutMs)
        {
            const int pollMs = 300;
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

        // ─────────────────────────────────────────────────────────────────────
        //  Helper: Lấy danh sách SerialPort từ row đang được check
        //  Nếu không có row nào check → trả về tất cả
        // ─────────────────────────────────────────────────────────────────────
        private List<SerialPort> GetSelectedOrAllPorts()
        {
            var selectedHandles = gvCOM.GetSelectedRows();
            if (selectedHandles == null || selectedHandles.Length == 0)
                return SerialPorts.ToList();

            var result = new List<SerialPort>();
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp != null) result.Add(sp);
                }
            }
            return result.Count > 0 ? result : SerialPorts.ToList();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  KHỞI TẠO
        // ═════════════════════════════════════════════════════════════════════

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

            // CheckBoxRowSelect: checkbox ở đầu mỗi row + header checkbox "chọn tất cả"
            gvCOM.OptionsSelection.MultiSelect = true;
            gvCOM.OptionsSelection.MultiSelectMode =
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
                _portLocks.TryAdd(sp.PortName, new object());

                var queue = new BlockingCollection<byte>(boundedCapacity: 50);
                _portQueues[sp.PortName] = queue;

                var capturedSp = sp;
                new Thread(() => ProcessPortQueue(capturedSp))
                {
                    IsBackground = true,
                    Name = $"Proc_{sp.PortName}"
                }.Start();

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

        // ═════════════════════════════════════════════════════════════════════
        //  DEDICATED PROCESSING THREAD
        // ═════════════════════════════════════════════════════════════════════

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
                    logger.Error($"[{sp.PortName}] ProcessPortQueue error: {ex.Message}");
                }
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  MODEM
        // ═════════════════════════════════════════════════════════════════════

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
                    dto => dto.Message101 = $"Error InitializeModem: {ex.Message}",
                    "Message101");
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  SERIAL PORT EVENTS
        // ═════════════════════════════════════════════════════════════════════

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
                logger.Error($"[{sp.PortName}] Error đọc SerialPort: {ex.Message}");
                return;
            }

            if (bytesRead <= 0) return;

            lock (_portLocks[sp.PortName])
            {
                MessageCOMs[sp.PortName] += Encoding.ASCII.GetString(buffer, 0, bytesRead);
            }

#if DEBUG
            if(sp.PortName=="COM142")
                Console.WriteLine($"{sp.PortName} --- {MessageCOMs[sp.PortName]}");
#endif

            if (_portQueues.TryGetValue(sp.PortName, out var queue))
                queue.TryAdd(1);
        }

        private void SerialPort_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            logger.Error($"[{sp.PortName}] Error: {e.EventType}");

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

        // ═════════════════════════════════════════════════════════════════════
        //  LISTEN EVENTS
        // ═════════════════════════════════════════════════════════════════════

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
                    dto => dto.Message101 = "Thay đổi IMEI thất bại. Thử lại sau 10s.",
                    "Message101");
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
                var checkUTF16 = Common.IsValidUtf16(messData);
                if (checkUTF16) messData = Common.DecodeUnicode(messData);

                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }

                string hsd = Common.ExtractHanSD(messData);
                if (string.IsNullOrEmpty(hsd))
                    UpdateComData(sp.PortName, dto => dto.Message101 = messData, "Message101");
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

        // ═════════════════════════════════════════════════════════════════════
        //  UI HELPERS
        // ═════════════════════════════════════════════════════════════════════

        private void UiRefreshTimer_Tick(object sender, EventArgs e)
        {
            if (_dirtyRows.IsEmpty) return;
            var dirty = _dirtyRows.Keys.ToList();
            foreach (var key in dirty) _dirtyRows.TryRemove(key, out _);
            foreach (var portName in dirty)
            {
                int rowHandle = gvCOM.LocateByValue("COM", portName);
                if (rowHandle >= 0) gvCOM.RefreshRow(rowHandle);
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
                logger.Error($"UpdateComData error: {ex.Message}");
            }
        }

        private void InvokeIfRequired(Action action)
        {
            if (gcCOM.IsDisposed || !gcCOM.IsHandleCreated) return;
            if (gcCOM.InvokeRequired) gcCOM.BeginInvoke(action);
            else action();
        }

        // ═════════════════════════════════════════════════════════════════════
        //  AT COMMAND
        // ═════════════════════════════════════════════════════════════════════

        private void SendATCommand(SerialPort sp, string command, int timeout = 1000)
        {
            try
            {
                lock (_portLocks[sp.PortName]) { MessageCOMs[sp.PortName] = string.Empty; }
                sp.WriteLine($"{command}{Environment.NewLine}");
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

        // ═════════════════════════════════════════════════════════════════════
        //  TIMER CHECK SIM
        // ═════════════════════════════════════════════════════════════════════

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
                            logger.Error($"Lỗi khi gửi lệnh tới {capturedSp.PortName}: {ex.Message}");
                            UpdateComData(capturedSp.PortName, dto =>
                            {
                                dto.ICCID = "COM ERROR";
                                dto.PhoneNumber = "COM ERROR";
                                dto.HSD = "COM ERROR";
                                dto.TKChinh = 0;
                                dto.Message101 =
                                    "COM ERROR. Đảm bảo cổng COM không có dấu chấm than.";
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

        // ═════════════════════════════════════════════════════════════════════
        //  BUTTON / POPUP HANDLERS
        // ═════════════════════════════════════════════════════════════════════

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
            foreach (int handle in gvCOM.GetSelectedRows())
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
                            { dto.TKChinh = 0; dto.Message101 = ""; }, "TKChinh", "Message101");
                            SendATCommand(capturedSp, "AT+CUSD=1,\"*101#\",15");
                        }
                        catch (Exception ex)
                        {
                            logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}");
                        }
                    });
                }
            }
        }

        private void PopupResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (int handle in gvCOM.GetSelectedRows())
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
                                dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                                dto.HSD = string.Empty; dto.TKChinh = 0;
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
                        catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                    });
                }
            }
        }

        private void PopupChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (int handle in gvCOM.GetSelectedRows())
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
                        catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                    });
                }
            }
        }

        private void PopupSao0Thang_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            foreach (int handle in gvCOM.GetSelectedRows())
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
                            { dto.TKChinh = 0; dto.Message101 = ""; }, "TKChinh", "Message101");
                            SendATCommand(capturedSp, "AT+CUSD=2");
                            Thread.Sleep(2000);
                            SendATCommand(capturedSp, "AT+CUSD=1,\"*0#\",15");
                        }
                        catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                    });
                }
            }
        }

        private void PopupPhatSinhData4G_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            int[] selectedHandles = gvCOM.GetSelectedRows();
            if (selectedHandles == null || selectedHandles.Length == 0)
            {
                XtraMessageBox.Show("Vui lòng tick checkbox ít nhất một cổng COM.",
                    "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            foreach (int handle in selectedHandles)
            {
                if (gvCOM.GetRow(handle) is ComDto row)
                {
                    var sp = SerialPorts.FirstOrDefault(x => x.PortName == row.COM);
                    if (sp == null) continue;
                    var capturedSp = sp;
                    _ = Task.Run(() => DownloadFileVia4G(capturedSp));
                }
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
            string msg = ports.Count == SerialPorts.Count
                ? $"Phát sinh Data 4G ({DataGenTargetBytes / 1024 / 1024} MB) cho tất cả {ports.Count} cổng COM?"
                : $"Phát sinh Data 4G ({DataGenTargetBytes / 1024 / 1024} MB) cho {ports.Count} cổng COM đã chọn?";

            if (XtraMessageBox.Show(msg, "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            foreach (var sp in ports)
            {
                var capturedSp = sp;
                _ = Task.Run(() => DownloadFileVia4G(capturedSp));
            }
        }

        private void BtnUpdateComPort_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            BindingList<ComDto> dataSource = gvCOM.DataSource as BindingList<ComDto>;
            if (dataSource == null) { logger.Error("DataSource là null!"); return; }

            var newDataSource = new BindingList<ComDto>();
            foreach (var item in dataSource)
            {
                var dup = dataSource.FirstOrDefault(x => x != item && x.STT == item.STT);
                if (dup != null && dup.STT != "")
                {
                    MessageBox.Show($"STT {item.STT} bị trùng!", "Cảnh báo",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                newDataSource.Add(item);
            }
            var sorted = new BindingList<ComDto>(
                newDataSource.OrderBy(x => !string.IsNullOrEmpty(x.STT) ? int.Parse(x.STT) : -1).ToList());
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
            gvCOM.RefreshData();
        }

        private void BtnChangeIMEI_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Thay đổi IMEI tất cả cổng COM?", "Xác nhận",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            foreach (var sp in SerialPorts)
            {
                var capturedSp = sp;
                new Thread(() =>
                {
                    try
                    {
                        if (!capturedSp.IsOpen) capturedSp.Open();
                        UpdateComData(capturedSp.PortName,
                            dto => dto.Message101 = "Đổi IMEI...", "Message101");
                        SendATCommand(capturedSp,
                            "AT+EGMR=1,7,\"" + Common.GenerateIMEI() + "\"\r\n");
                    }
                    catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                })
                { IsBackground = true }.Start();
            }
        }

        private void BtnResetCom_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Reset lại toàn bộ cổng COM?", "Xác nhận",
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
                            dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                            dto.HSD = string.Empty; dto.TKChinh = 0; dto.Message101 = "Reset cổng COM";
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
                    catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                });
            }
        }

        private void BtnRestoreSettings_ItemClick(object sender, DevExpress.XtraBars.ItemClickEventArgs e)
        {
            if (XtraMessageBox.Show("Khôi phục cài đặt gốc?", "Xác nhận",
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
                            dto.ICCID = string.Empty; dto.PhoneNumber = string.Empty;
                            dto.HSD = string.Empty; dto.TKChinh = 0;
                            dto.Message101 = "Khôi phục cài đặt gốc";
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
                    catch (Exception ex) { logger.Error($"Lỗi {capturedSp.PortName}: {ex.Message}"); }
                });
            }
        }

        // ═════════════════════════════════════════════════════════════════════
        //  HELPERS
        // ═════════════════════════════════════════════════════════════════════

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