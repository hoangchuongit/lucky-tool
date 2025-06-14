using DevExpress.XtraEditors;
namespace LuckBurnTK
{
    public partial class TermForm : XtraForm
    {
        public TermForm()
        {
            InitializeComponent();
            richTextBox1.LoadFile("terms.rtf");
            richTextBox1.ReadOnly = true;
        }
    }
}