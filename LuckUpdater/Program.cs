using System;
using System.Windows.Forms;

namespace LuckUpdater
{
    internal class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UpdaterForm(args[0], args[1]));
            //var a = "C:\\Users\\admin\\AppData\\Local\\Temp\\update.zip";
            //var b = "D:\\Freelancer\\LuckTools\\LuckBurnTkGsm\\bin\\Debug\\LuckBurnTK.exe";
            //Application.Run(new UpdaterForm(a, b));
        }
    }
}
