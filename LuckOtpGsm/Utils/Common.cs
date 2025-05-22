using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LuckOTP.Utils
{
    public class Common
    {
        public static readonly string connectionString = "Host=51.79.161.237;Port=5432;Database=luckotp;User Id=gsm;Password=Ninhpm@123;Pooling=true;MinPoolSize=1;MaxPoolSize=20";

        public static IEnumerable<Dictionary<string, string>> GetFullPortNames()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%'"))
            {
                return searcher.Get().Cast<ManagementBaseObject>()
                    .Select(p => new Dictionary<string, string>
                    {
                    { "Caption", p["Caption"]?.ToString() ?? string.Empty },
                    { "DeviceID", p["DeviceID"]?.ToString() ?? string.Empty }
                    }).ToList();
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetPhoneNumber(string input)
        {
            // Regular expression to match phone numbers
            // Matches 10 digits starting with 0 or 11 digits starting with 84
            string pattern = @"(?:0\d{9}|\d{9}|84\d{9})(?!\d)";

            // Create a Regex object
            Regex regex = new Regex(pattern);

            // Find matches
            Match match = regex.Match(input);

            // Check if a match is found
            if (match.Success)
            {
                string phone = match.Value;
                phone = phone.Length == 11 ? "0" + phone.Substring(2) : phone.Length == 9 ? "0" + phone : phone;
                return phone;
            }

            return null;
        }

        public static byte[] CreateWavHeader(int dataLength, int sampleRate, int numChannels, int bitsPerSample)
        {
            int blockAlign = numChannels * (bitsPerSample / 8);
            int byteRate = sampleRate * blockAlign;
            byte[] header = new byte[44];

            // RIFF header
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("RIFF"), 0, header, 0, 4);
            BitConverter.GetBytes(36 + dataLength).CopyTo(header, 4); // File size minus 8 bytes
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("WAVE"), 0, header, 8, 4);

            // fmt chunk
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("fmt "), 0, header, 12, 4);
            BitConverter.GetBytes(16).CopyTo(header, 16); // PCM header size
            BitConverter.GetBytes((short)1).CopyTo(header, 20); // Audio format (PCM)
            BitConverter.GetBytes((short)numChannels).CopyTo(header, 22);
            BitConverter.GetBytes(sampleRate).CopyTo(header, 24);
            BitConverter.GetBytes(byteRate).CopyTo(header, 28);
            BitConverter.GetBytes((short)blockAlign).CopyTo(header, 32);
            BitConverter.GetBytes((short)bitsPerSample).CopyTo(header, 34);

            // data chunk
            Buffer.BlockCopy(Encoding.ASCII.GetBytes("data"), 0, header, 36, 4);
            BitConverter.GetBytes(dataLength).CopyTo(header, 40);

            return header;
        }

        // Converts bytes to kilobytes and returns the result as a string.
        public static string BytesToKB(long bytes)
        {
            // Convert bytes to kilobytes (1 KB = 1024 bytes)
            long kb = bytes / 1024;

            // Return the result as a formatted string
            return $"{kb} KB";
        }

        public static bool IsValidUtf16(string input)
        {
            // Bước 1: Kiểm tra độ dài và ký tự hex
            if (input.Length % 4 != 0 || !Regex.IsMatch(input, @"\A\b[0-9A-Fa-f]+\b\Z"))
                return false;
            // Bước 2: Duyệt từng cặp 4 ký tự (mã UTF-16)
            for (int i = 0; i < input.Length; i += 4)
            {
                string hex = input.Substring(i, 4);
                int unicodeValue = Convert.ToInt32(hex, 16);

                // Loại bỏ các mã trong khoảng D800 - DFFF (surrogate pairs)
                if (unicodeValue >= 0xD800 && unicodeValue <= 0xDFFF)
                    return false;

                // Kiểm tra xem có nằm ngoài phạm vi UTF-16
                if (unicodeValue > 0xFFFF)
                    return false;
            }
            return true;
        }

        public static string DecodeUnicode(string unicodeString)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < unicodeString.Length; i += 4)
            {
                string hex = unicodeString.Substring(i, 4);
                int unicodeValue = Convert.ToInt32(hex, 16);
                sb.Append((char)unicodeValue);
            }

            return sb.ToString();
        }

        // Hàm tạo số IMEI ngẫu nhiên hợp lệs
        public static string GenerateIMEI()
        {
            // TAC dành cho Quectel M26 (có thể thay đổi)
            string tac = "86159703";

            // Tạo 6 số serial ngẫu nhiên
            string serial = GenerateRandomNumber(6);

            // Kết hợp TAC + Serial
            string imeiBase = tac + serial;

            // Tính checksum bằng Luhn Algorithm
            int checksum = CalculateLuhnChecksum(imeiBase);

            return imeiBase + checksum;
        }

        private static string GenerateRandomNumber(int length)
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] randomBytes = new byte[length];
                rng.GetBytes(randomBytes);

                char[] result = new char[length];
                for (int i = 0; i < length; i++)
                {
                    result[i] = (char)('0' + (randomBytes[i] % 10)); // Chuyển thành số từ 0-9
                }

                return new string(result);
            }
        }

        private static int CalculateLuhnChecksum(string imeiBase)
        {
            int sum = 0;
            bool doubleDigit = true;

            for (int i = imeiBase.Length - 1; i >= 0; i--)
            {
                int n = imeiBase[i] - '0';

                if (doubleDigit)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }

                sum += n;
                doubleDigit = !doubleDigit;
            }

            return (10 - (sum % 10)) % 10;
        }
    }
}