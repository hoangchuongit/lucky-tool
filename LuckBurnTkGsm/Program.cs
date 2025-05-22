using System;
using System.Globalization;
using System.Threading;
using System.Windows.Forms;

namespace LuckBurnTK
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            //// Tạo Key (32 byte)
            //byte[] key = new byte[32];
            //byte[] iv = new byte[16];
            //using (var rng = RandomNumberGenerator.Create())
            //{
            //    rng.GetBytes(key);
            //    rng.GetBytes(iv);
            //}

            //string base64Key = Convert.ToBase64String(key);
            //string base64IV = Convert.ToBase64String(iv);
            //Console.WriteLine("KEY = " + base64Key);
            //Console.WriteLine("IV  = " + base64IV);

            //string plain = "Host=51.79.161.237;Port=5432;Database=luck_burn;User Id=gsm_burn;Password=;Pooling=true;MinPoolSize=1;MaxPoolSize=20;";
            //string encrypted = SecureConfig.Encrypt(plain);
            //Console.WriteLine("Encrypted: " + encrypted);

            CultureInfo viVN = new CultureInfo("vi-VN");
            Thread.CurrentThread.CurrentCulture = viVN;
            Thread.CurrentThread.CurrentUICulture = viVN;

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LoginForm());
        }
    }
}