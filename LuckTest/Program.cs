using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LuckTest
{
    internal class Program
    {
        static SerialPort serialPort;

        static void Main(string[] args)
        {
            string portName = "COM8"; // ⚠️ Đổi COM port đúng với thiết bị
            serialPort = new SerialPort(portName, 115200, Parity.None, 8, StopBits.One);
            serialPort.Encoding = Encoding.ASCII;
            serialPort.ReadTimeout = 3000;
            serialPort.WriteTimeout = 3000;

            try
            {
                serialPort.Open();

                byte[] data = File.ReadAllBytes("D:\\Freelancer\\LuckTools\\LuckTest\\alo.amr");

                serialPort.Write("AT+CFUN=1,1");
                Thread.Sleep(10000);

                // Bước 1: QFOPEN
                serialPort.DiscardInBuffer();
                serialPort.Write($"AT+QFOPEN=\"RAM:alo.amr\",0,{data.Length + 1000}\r");
                Thread.Sleep(3000);
                string openResponse = serialPort.ReadExisting();
                Console.WriteLine("QFOPEN response: " + openResponse);

                int fd = ParseFd(openResponse);
                if (fd == -1)
                {
                    Console.WriteLine("❌ Không lấy được file descriptor.");
                    return;
                }

                // Bước 2: QFWRITE
                serialPort.DiscardInBuffer();
                serialPort.Write($"AT+QFWRITE={fd},{data.Length},20\r");
                Thread.Sleep(500);
                string writePrompt = serialPort.ReadExisting();
                Console.WriteLine("QFWRITE response: " + writePrompt);

                if (writePrompt.Contains("CONNECT"))
                {
                    serialPort.Write(data, 0, data.Length); // Gửi dữ liệu
                    Thread.Sleep(500);
                    string writeDone = serialPort.ReadExisting();
                    Console.WriteLine("Write result: " + writeDone);
                }
                else
                {
                    Console.WriteLine("❌ Không nhận được CONNECT sau QFWRITE.");
                    return;
                }

                // Bước 3: QFCLOSE
                serialPort.DiscardInBuffer();
                serialPort.Write($"AT+QFCLOSE={fd}\r");
                Thread.Sleep(500);
                string closeResp = serialPort.ReadExisting();
                Console.WriteLine("QFCLOSE response: " + closeResp);
                serialPort.Write($"AT+QFLST=\"RAM:*\"");
                Thread.Sleep(10000);
                Console.WriteLine("Done");
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

        static int ParseFd(string response)
        {
            // Tách dòng như: +QFOPEN: 1
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
    }
}
