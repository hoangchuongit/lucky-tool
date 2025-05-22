using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;

namespace LuckOTP.Utils
{
    public class Common
    {
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

        public static string NormalizePhone(string phone)
        {
            if (string.IsNullOrWhiteSpace(phone))
                return phone;
            phone = Regex.Replace(phone, @"[^\d]", "");
            if (phone.StartsWith("84"))
                return phone;
            else if (phone.StartsWith("0"))
                return "84" + phone.Substring(1);
            else
                return "84" + phone;
        }
    }
}