using DevExpress.XtraEditors;

namespace LuckTelegram
{
    public partial class ProxyForm : XtraForm
    {
        public string ProxyText => txtProxies.Text;

        public ProxyForm()
        {
            InitializeComponent();
        }
    }
}