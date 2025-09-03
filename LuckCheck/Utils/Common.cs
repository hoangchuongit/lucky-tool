using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LuckCheck.Utils
{
    public class Common
    {
        public static IEnumerable<Dictionary<string, string>> GetFullPortNames()
        {
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption LIKE '%(COM%' AND ConfigManagerErrorCode = 0"))
            {
                return searcher.Get().Cast<ManagementBaseObject>().Select(p => new Dictionary<string, string>{
                    { "Caption", p["Caption"]?.ToString() ?? string.Empty },
                    { "DeviceID", p["DeviceID"]?.ToString() ?? string.Empty }
                }).ToList().Where(x => x["Caption"].Contains("XR21V1414"));
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            try
            {
                return Enumerable.Range(0, hex.Length).Where(x => x % 2 == 0).Select(x => Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
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

        public static bool IsValidUtf16(string input)
        {
            // Bước 1: Kiểm tra độ dài và ký tự hex
            if (input.Length % 4 != 0 || !Regex.IsMatch(input, @"\A\b[0-9A-Fa-f]+\b\Z")) return false;
            // Bước 2: Duyệt từng cặp 4 ký tự (mã UTF-16)
            for (int i = 0; i < input.Length; i += 4)
            {
                string hex = input.Substring(i, 4);
                int unicodeValue = Convert.ToInt32(hex, 16);
                // Loại bỏ các mã trong khoảng D800 - DFFF (surrogate pairs)
                if (unicodeValue >= 0xD800 && unicodeValue <= 0xDFFF) return false;
                // Kiểm tra xem có nằm ngoài phạm vi UTF-16
                if (unicodeValue > 0xFFFF) return false;
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

        /// <summary>
        /// Trích xuất số tiền từ chuỗi tài khoản chính (tìm trước VND, VNĐ, d, đ)
        /// </summary>
        /// <param name="input">Chuỗi đầu vào chứa số dư</param>
        /// <returns>Số tiền nếu tìm được, null nếu không</returns>
        public static int? ExtractBalance(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            // Regex: bắt số có thể chứa , hoặc . trước các đơn vị d, đ, vnd, vnđ
            var match = Regex.Match(input, @"([\d.,]+)\s*(v?n?[dđ])", RegexOptions.None);
            if (!match.Success) return null;
            string raw = match.Groups[1].Value;
            if (Regex.IsMatch(raw, @"^\d{9,11}$"))
                return null;
            string cleaned = raw.Replace(",", "").Replace(".", "");
            return int.TryParse(cleaned, out int balance) ? balance : (int?)null;
        }

        public static string ExtractValidPhoneNumberCall(string data)
        {
            // Regex tìm số trong dấu ngoặc kép
            var match = Regex.Match(data, @"\+CLCC:[^""]*""([^""]+)""");
            if (match.Success)
            {
                string phone = match.Groups[1].Value;
                // Kiểm tra số có độ dài hợp lý và cho phép dấu +
                if (phone.Length >= 8 && Regex.IsMatch(phone, @"^\+?\d+$"))
                    return phone;
            }
            return null; // Không hợp lệ
        }
        
        public static string ExtractNgayKH(string input)
        {
            var pattern = @"(?:\bhsd\b|het\s*han|han\s*su\s*dung(?:\s*den\s*ngay)?|dung\s*den)" +
                  @"\s*:?\s*" +
                  @"(?:\d{1,2}:\d{2}(?::\d{2})?\s+)?" + // giờ trước ngày (tuỳ chọn)
                  @"(\d{2}[-/]\d{2}[-/]\d{4})";        // ngày

            var m = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
            if (!m.Success) return null;
            return m.Groups[1].Value.Replace('-', '/');
        }
    }
}