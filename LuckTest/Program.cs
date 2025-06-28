using System;
using System.IO;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace LuckTest
{
    internal class Program
    {
        private static SerialPort serialPort;

        private static void Main(string[] args)
        {
            string portName = "COM16"; // ⚠️ Đổi COM port đúng với thiết bị
            string phoneNumber = "0349751746"; // ⚠️ Đổi số điện thoại gọi đến
            string filename = "HỖ-TRỢ-1-KỲ-19S.amr"; // ⚠️ Tên file âm thanh
            string filePath = $"D:\\Freelancer\\LuckTools\\LuckTest\\{filename}";
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One)
            {
                Encoding = Encoding.ASCII,
                ReadTimeout = 3000,
                WriteTimeout = 3000
            };

            try
            {
                serialPort.Open();
                byte[] data = File.ReadAllBytes(filePath);

                serialPort.WriteLine("AT+QFDWL=\"RAM:record.amr\"");
                Thread.Sleep(1000);

                Console.WriteLine("🔄 Reset modem...");
                serialPort.Write("AT+CFUN=1,1\r\n");
                Thread.Sleep(10000);

                // Tải file .amr vào RAM
                if (!UploadAmrFileToRAM(filename, data))
                {
                    Console.WriteLine("❌ Không thể upload file.");
                    return;
                }

                // Gọi điện
                Console.WriteLine("📞 Đang gọi tới " + phoneNumber);
                serialPort.DiscardInBuffer();
                serialPort.Write($"ATD{phoneNumber};\r\n");
                string callResp = WaitForResponse("CONNECT", 15000);
                Console.WriteLine("📶 Call response: " + callResp);

                if (!callResp.Contains("+CLCC:") || !callResp.Contains(",0,0,"))
                {
                    Console.WriteLine("❌ Không kết nối được cuộc gọi.");
                    return;
                }

                // Bắt đầu ghi âm (tùy chọn)
                Console.WriteLine("🔴 Bắt đầu ghi âm...");
                serialPort.Write("AT+QAUDRD=1,\"RAM:record.amr\",3\r\n");
                Thread.Sleep(1000);
                string recResp = serialPort.ReadExisting();
                Console.WriteLine("QAUDRD response: " + recResp);

                //// Đợi 11 giây
                //Console.WriteLine("⏳ Đợi 11 giây...");
                //Thread.Sleep(11000);

                // Phát âm thanh từ file RAM
                Console.WriteLine("🔊 Đang phát âm thanh...");
                serialPort.Write($"AT+QPSND=1,\"RAM:{filename}\",0,7,7,1\r\n");
                string playResp = WaitForResponse("OK", 5000);
                Console.WriteLine("QAUDPLAY response: " + playResp);

                // Giữ cuộc gọi thêm 20 giây để phát đủ
                Thread.Sleep(20000);

                // Dừng ghi âm
                Console.WriteLine("⏹️ Dừng ghi âm...");
                serialPort.Write("AT+QAUDRD=0\r\n");
                Thread.Sleep(1000);
                string stopRec = serialPort.ReadExisting();
                Console.WriteLine("Stop record response: " + stopRec);

                // Kết thúc cuộc gọi
                serialPort.Write("ATH\r\n");
                Console.WriteLine("📴 Đã kết thúc cuộc gọi.");

                Console.WriteLine("✅ Hoàn tất.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi: " + ex.Message);
            }
            finally
            {
                if (serialPort.IsOpen)
                    serialPort.Close();
            }
        }

        private static bool UploadAmrFileToRAM(string filename, byte[] data)
        {
            serialPort.DiscardInBuffer();
            serialPort.Write($"AT+QFOPEN=\"RAM:{filename}\",0,{data.Length + 1000}\r\n");
            string openResponse = WaitForResponse("+QFOPEN:", 5000);
            int fd = ParseFd(openResponse);
            if (fd == -1) return false;

            serialPort.DiscardInBuffer();
            serialPort.Write($"AT+QFWRITE={fd},{data.Length},20\r\n");
            string writePrompt = WaitForResponse("CONNECT", 5000);

            if (!writePrompt.Contains("CONNECT"))
            {
                Console.WriteLine("❌ Không nhận được CONNECT.");
                return false;
            }

            serialPort.Write(data, 0, data.Length);
            string writeResult = WaitForResponse("OK", 10000);

            if (!writeResult.Contains("OK"))
            {
                Console.WriteLine("❌ Ghi dữ liệu thất bại: " + writeResult);
                return false;
            }

            serialPort.Write($"AT+QFCLOSE={fd}\r\n");
            string closeResp = WaitForResponse("OK", 2000);
            return closeResp.Contains("OK");
        }

        private static int ParseFd(string response)
        {
            foreach (string line in response.Split('\n'))
            {
                if (line.Trim().StartsWith("+QFOPEN:"))
                {
                    string[] parts = line.Split(':');
                    if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int fd))
                        return fd;
                }
            }
            return -1;
        }

        private static string WaitForResponse(string keyword, int timeoutMs)
        {
            StringBuilder sb = new StringBuilder();
            DateTime start = DateTime.Now;
            while ((DateTime.Now - start).TotalMilliseconds < timeoutMs)
            {
                try
                {
                    string incoming = serialPort.ReadExisting();
                    if (!string.IsNullOrEmpty(incoming))
                    {
                        sb.Append(incoming);
                        if (sb.ToString().Contains(keyword)) break;
                    }
                }
                catch { }
                Thread.Sleep(100);
            }
            return sb.ToString();
        }
    }
}