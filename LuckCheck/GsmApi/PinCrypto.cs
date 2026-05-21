using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace LuckCheck.GsmApi
{
    /// <summary>
    /// AES-256-CBC + HMAC-SHA256 (Encrypt-then-MAC) dùng để mã hoá PIN thẻ cào
    /// khi truyền qua HTTP giữa Go Backend và C# GSM service.
    ///
    /// Blob format: Base64( IV[16] | Ciphertext[N*16] | HMAC-SHA256[32] )
    ///
    /// Key derivation từ 1 master key (32 bytes) → 2 independent subkeys:
    ///   encKey = HMAC-SHA256(master, "gsm-pin-enc-key-v1")   → AES-256 key
    ///   macKey = HMAC-SHA256(master, "gsm-pin-mac-key-v1")   → HMAC key
    ///
    /// Tại sao AES-256-CBC + HMAC thay vì AES-GCM?
    ///   AesGcm chỉ có từ .NET Core 3.0+; project dùng .NET 4.8.
    ///   Encrypt-then-MAC với HMAC-SHA256 tương đương bảo mật khi implement đúng.
    /// </summary>
    public static class PinCrypto
    {
        private const int IvLen      = 16;  // AES block size
        private const int MacLen     = 32;  // SHA-256 output
        private const int MinBlobLen = IvLen + 16 + MacLen; // IV + 1 AES block + MAC

        private const string EncContext = "gsm-pin-enc-key-v1";
        private const string MacContext = "gsm-pin-mac-key-v1";

        // ──────────────────── Public API ────────────────────

        /// <summary>
        /// Mã hoá PIN thẻ cào.
        /// Trả về Base64 blob an toàn để đặt trong JSON field "pin_encrypted".
        /// </summary>
        /// <param name="pin">Mã thẻ dạng plain-text (9-15 chữ số).</param>
        /// <param name="masterKeyBase64">Master key 32 bytes, Base64-encoded (từ App.config / env).</param>
        public static string Encrypt(string pin, string masterKeyBase64)
        {
            if (string.IsNullOrEmpty(pin))            throw new ArgumentNullException(nameof(pin));
            if (string.IsNullOrEmpty(masterKeyBase64)) throw new ArgumentNullException(nameof(masterKeyBase64));

            byte[] master     = FromBase64Strict(masterKeyBase64);
            byte[] encKey     = DeriveSubKey(master, EncContext);
            byte[] macKey     = DeriveSubKey(master, MacContext);

            byte[] iv         = GenerateIv();
            byte[] plainBytes = Encoding.UTF8.GetBytes(pin);
            byte[] ciphertext = AesCbcEncrypt(encKey, iv, plainBytes);
            byte[] mac        = ComputeHmac(macKey, iv, ciphertext);

            // Concat: IV | Ciphertext | MAC
            byte[] blob = new byte[IvLen + ciphertext.Length + MacLen];
            Buffer.BlockCopy(iv,         0, blob, 0,                         IvLen);
            Buffer.BlockCopy(ciphertext, 0, blob, IvLen,                     ciphertext.Length);
            Buffer.BlockCopy(mac,        0, blob, IvLen + ciphertext.Length, MacLen);

            return Convert.ToBase64String(blob);
        }

        /// <summary>
        /// Giải mã blob và trả về PIN plain-text.
        /// Ném <see cref="CryptographicException"/> nếu MAC không hợp lệ hoặc blob bị giả mạo.
        /// </summary>
        /// <param name="blobBase64">Giá trị từ field "pin_encrypted" trong request.</param>
        /// <param name="masterKeyBase64">Cùng master key dùng lúc mã hoá.</param>
        public static string Decrypt(string blobBase64, string masterKeyBase64)
        {
            if (string.IsNullOrEmpty(blobBase64))      throw new ArgumentNullException(nameof(blobBase64));
            if (string.IsNullOrEmpty(masterKeyBase64)) throw new ArgumentNullException(nameof(masterKeyBase64));

            byte[] blob = FromBase64Strict(blobBase64);
            if (blob.Length < MinBlobLen)
                throw new CryptographicException("Blob quá ngắn — dữ liệu bị hỏng hoặc giả mạo.");

            byte[] master       = FromBase64Strict(masterKeyBase64);
            byte[] encKey       = DeriveSubKey(master, EncContext);
            byte[] macKey       = DeriveSubKey(master, MacContext);

            int ciphertextLen   = blob.Length - IvLen - MacLen;
            byte[] iv           = new byte[IvLen];
            byte[] ciphertext   = new byte[ciphertextLen];
            byte[] receivedMac  = new byte[MacLen];

            Buffer.BlockCopy(blob, 0,                      iv,          0, IvLen);
            Buffer.BlockCopy(blob, IvLen,                  ciphertext,  0, ciphertextLen);
            Buffer.BlockCopy(blob, IvLen + ciphertextLen,  receivedMac, 0, MacLen);

            byte[] expectedMac = ComputeHmac(macKey, iv, ciphertext);

            // Constant-time comparison — tránh timing attack khi so sánh MAC
            if (!ConstantTimeEquals(receivedMac, expectedMac))
                throw new CryptographicException(
                    "HMAC không hợp lệ — dữ liệu bị giả mạo hoặc sai PinMasterKey.");

            byte[] plainBytes = AesCbcDecrypt(encKey, iv, ciphertext);
            return Encoding.UTF8.GetString(plainBytes);
        }

        // ──────────────────── Key derivation ────────────────────

        /// <summary>
        /// HKDF-lite: HMAC-SHA256(masterKey, contextInfo) → 32-byte subkey.
        /// Hai context string khác nhau tạo ra 2 key độc lập, tránh related-key attack.
        /// </summary>
        private static byte[] DeriveSubKey(byte[] masterKey, string context)
        {
            using (var hmac = new HMACSHA256(masterKey))
                return hmac.ComputeHash(Encoding.UTF8.GetBytes(context));
        }

        // ──────────────────── AES-256-CBC ────────────────────

        private static byte[] AesCbcEncrypt(byte[] key, byte[] iv, byte[] plaintext)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize   = 256;
                aes.BlockSize = 128;
                aes.Mode      = CipherMode.CBC;
                aes.Padding   = PaddingMode.PKCS7;
                aes.Key       = key;
                aes.IV        = iv;
                using (var enc = aes.CreateEncryptor())
                    return enc.TransformFinalBlock(plaintext, 0, plaintext.Length);
            }
        }

        private static byte[] AesCbcDecrypt(byte[] key, byte[] iv, byte[] ciphertext)
        {
            using (var aes = new AesCryptoServiceProvider())
            {
                aes.KeySize   = 256;
                aes.BlockSize = 128;
                aes.Mode      = CipherMode.CBC;
                aes.Padding   = PaddingMode.PKCS7;
                aes.Key       = key;
                aes.IV        = iv;
                using (var dec = aes.CreateDecryptor())
                    return dec.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            }
        }

        // ──────────────────── HMAC ────────────────────

        /// <summary>Encrypt-then-MAC: HMAC over IV || Ciphertext.</summary>
        private static byte[] ComputeHmac(byte[] macKey, byte[] iv, byte[] ciphertext)
        {
            byte[] input = new byte[iv.Length + ciphertext.Length];
            Buffer.BlockCopy(iv,         0, input, 0,         iv.Length);
            Buffer.BlockCopy(ciphertext, 0, input, iv.Length, ciphertext.Length);
            using (var hmac = new HMACSHA256(macKey))
                return hmac.ComputeHash(input);
        }

        // ──────────────────── Helpers ────────────────────

        private static byte[] GenerateIv()
        {
            byte[] iv = new byte[IvLen];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(iv);
            return iv;
        }

        private static byte[] FromBase64Strict(string b64)
        {
            try   { return Convert.FromBase64String(b64); }
            catch { throw new FormatException("Giá trị không phải Base64 hợp lệ."); }
        }

        /// <summary>
        /// So sánh hai byte array trong thời gian hằng số (constant-time).
        /// Tránh timing attack: kẻ tấn công không thể đo thời gian để đoán vị trí sai.
        /// </summary>
        private static bool ConstantTimeEquals(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++)
                diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // ──────────────────── Utility ────────────────────

        /// <summary>
        /// Generate một master key ngẫu nhiên 32 bytes, encode Base64.
        /// Dùng 1 lần để tạo key trong môi trường deploy — lưu vào App.config và .env Go BE.
        /// </summary>
        public static string GenerateMasterKey()
        {
            byte[] key = new byte[32];
            using (var rng = new RNGCryptoServiceProvider())
                rng.GetBytes(key);
            return Convert.ToBase64String(key);
        }
    }
}
