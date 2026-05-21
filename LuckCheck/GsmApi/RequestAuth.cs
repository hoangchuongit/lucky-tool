using System;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace LuckCheck.GsmApi
{
    /// <summary>
    /// HMAC-SHA256 request authentication cho incoming API calls từ Go Backend.
    ///
    /// ─── Giao thức (Go BE phải implement) ───────────────────────────────────────
    ///
    /// Với mỗi request, Go BE thêm 2 headers:
    ///
    ///   X-Timestamp : {unix_epoch_seconds}
    ///   X-Signature : hex( HMAC-SHA256(sharedSecret, "{timestamp}\n{hex(SHA-256(body))}") )
    ///
    /// Ví dụ (Go):
    ///   bodyBytes  := []byte(requestBody)
    ///   bodyHash   := fmt.Sprintf("%x", sha256.Sum256(bodyBytes))
    ///   ts         := strconv.FormatInt(time.Now().Unix(), 10)
    ///   message    := ts + "\n" + bodyHash
    ///   mac        := hmac.New(sha256.New, []byte(sharedSecret))
    ///   mac.Write([]byte(message))
    ///   signature  := fmt.Sprintf("%x", mac.Sum(nil))
    ///
    /// ─── Bảo vệ ─────────────────────────────────────────────────────────────────
    ///
    ///   Replay attack  : X-Timestamp bị reject nếu |now − ts| > 300 giây
    ///   Forged request : HMAC không khớp → 401
    ///   Timing attack  : So sánh signature bằng constant-time
    ///   MITM           : Body hash trong MAC đảm bảo payload không bị sửa
    /// </summary>
    public static class RequestAuth
    {
        /// <summary>Cửa sổ chấp nhận timestamp: ±5 phút.</summary>
        public const int TimestampToleranceSec = 300;

        // ──────────────────── Verification ────────────────────

        /// <summary>
        /// Xác minh request đến.
        /// </summary>
        /// <param name="req">HttpListenerRequest từ GsmApiServer.</param>
        /// <param name="bodyBytes">Body đã đọc sẵn dưới dạng byte[].</param>
        /// <param name="sharedSecret">Khóa bí mật chung (từ App.config ApiSharedSecret).</param>
        /// <param name="error">Mô tả lỗi nếu xác minh thất bại.</param>
        /// <returns>true nếu hợp lệ; false nếu bị từ chối.</returns>
        public static bool Verify(HttpListenerRequest req, byte[] bodyBytes,
                                   string sharedSecret, out string error)
        {
            error = null;

            // ── 1. X-Timestamp ──
            string tsHeader = req.Headers["X-Timestamp"];
            if (string.IsNullOrEmpty(tsHeader) || !long.TryParse(tsHeader, out long tsEpoch))
            {
                error = "X-Timestamp header thiếu hoặc không phải số nguyên";
                return false;
            }

            long nowEpoch = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            long diff     = Math.Abs(nowEpoch - tsEpoch);
            if (diff > TimestampToleranceSec)
            {
                error = $"Request hết hạn: |now({nowEpoch}) − ts({tsEpoch})| = {diff}s > {TimestampToleranceSec}s";
                return false;
            }

            // ── 2. X-Signature ──
            string sigHeader = req.Headers["X-Signature"];
            if (string.IsNullOrEmpty(sigHeader))
            {
                error = "X-Signature header thiếu";
                return false;
            }

            string bodyHash = Sha256Hex(bodyBytes);
            string message  = $"{tsEpoch}\n{bodyHash}";
            string expected = HmacSha256Hex(Encoding.UTF8.GetBytes(sharedSecret), message);

            // Constant-time compare — tránh timing attack
            if (!ConstantTimeStringEquals(sigHeader.ToLowerInvariant(), expected))
            {
                error = "X-Signature không khớp — sai sharedSecret hoặc body bị sửa";
                return false;
            }

            return true;
        }

        // ──────────────────── Builder (dùng để Go BE tham khảo) ────────────────────

        /// <summary>
        /// Tạo signature string cho một request.
        /// Dùng để test hoặc để Go BE implement đúng giao thức.
        ///   message = "{timestamp}\n{hex(SHA256(body))}"
        ///   signature = hex(HMAC-SHA256(sharedSecret, message))
        /// </summary>
        public static string BuildSignature(string sharedSecret, long timestamp, byte[] body)
        {
            string bodyHash = Sha256Hex(body);
            string message  = $"{timestamp}\n{bodyHash}";
            return HmacSha256Hex(Encoding.UTF8.GetBytes(sharedSecret), message);
        }

        // ──────────────────── Crypto helpers ────────────────────

        private static string Sha256Hex(byte[] data)
        {
            using (var sha = SHA256.Create())
                return BytesToHex(sha.ComputeHash(data ?? Array.Empty<byte>()));
        }

        private static string HmacSha256Hex(byte[] key, string message)
        {
            using (var hmac = new HMACSHA256(key))
                return BytesToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
        }

        private static string BytesToHex(byte[] bytes)
        {
            var sb = new System.Text.StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// So sánh hai string trong thời gian hằng số.
        /// Nếu độ dài khác nhau vẫn chạy đủ vòng lặp để tránh leak timing.
        /// </summary>
        private static bool ConstantTimeStringEquals(string a, string b)
        {
            // Pad chuỗi ngắn hơn để tránh leak độ dài
            int maxLen = Math.Max(a.Length, b.Length);
            int diff   = a.Length ^ b.Length; // non-zero nếu độ dài khác
            for (int i = 0; i < maxLen; i++)
            {
                char ca = i < a.Length ? a[i] : '\0';
                char cb = i < b.Length ? b[i] : '\0';
                diff |= ca ^ cb;
            }
            return diff == 0;
        }
    }
}
