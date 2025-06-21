using DevExpress.XtraEditors;
namespace LuckBurnTK
{
    public partial class TermForm : XtraForm
    {
        public TermForm()
        {
            InitializeComponent();
            pdfViewer1.LoadDocument("terms.pdf");
            pdfViewer1.ReadOnly = true;
        }
    }
}