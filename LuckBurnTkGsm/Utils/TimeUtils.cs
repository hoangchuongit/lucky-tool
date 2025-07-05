using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LuckBurnTK.Utils
{
    public static class TimeUtils
    {
        // 1️⃣ Lấy giờ UTC chính xác từ NTP server
        public static DateTime GetNetworkUtcTime(string ntpServer = "time.windows.com")
        {
            byte[] ntpData = new byte[48];
            ntpData[0] = 0x1B; // LI=0, VN=3, Mode=3 (RFC-2030)

            var addresses = Dns.GetHostEntry(ntpServer).AddressList;
            var endpoint = new IPEndPoint(addresses[0], 123);

            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                socket.Connect(endpoint);
                socket.ReceiveTimeout = 2000; // Timeout 2s
                socket.Send(ntpData);
                socket.Receive(ntpData);
            }

            const byte offset = 40;
            ulong intPart = BitConverter.ToUInt32(ntpData, offset);
            ulong fracPart = BitConverter.ToUInt32(ntpData, offset + 4);
            ulong milliseconds = (intPart * 1000) + ((fracPart * 1000) / 0x100000000UL);

            DateTime epoch = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddMilliseconds((long)milliseconds);
        }

        // 2️⃣ Chuyển giờ UTC sang giờ UTC+7 (giờ VN)
        public static DateTime GetNetworkTimeUtcPlus7(string ntpServer = "time.windows.com")
        {
            DateTime utc = GetNetworkUtcTime(ntpServer);

            // Trên Windows, "SE Asia Standard Time" là múi UTC+7
            var tz = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }

        /// <summary>
        /// Nhận ngày hết hạn (dd/MM/yyyy), trả về số ngày đã trôi qua so với thời gian UTC+7 hiện tại.
        /// Trả về null nếu định dạng ngày không hợp lệ hoặc lỗi mạng/NTP.
        /// </summary>
        public static int? DaysSinceHsd(string hsd)
        {
            if (!DateTime.TryParseExact(hsd, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime expiry))
            {
                return null;
            }

            DateTime nowLocal;
            try
            {
                nowLocal = GetNetworkTimeUtcPlus7().Date;
            }
            catch
            {
                return null; // Khi lỗi kết nối NTP
            }
            return (nowLocal - expiry.Date).Days;
        }
    }
}
