using System;
using System.Drawing;

namespace LuckBurnTK
{
    public partial class GuideFlyoutPanel : DevExpress.XtraEditors.XtraUserControl
    {
        private int curentLessonIdCore;
        private BurnTKForm module;
        private int count;

        public GuideFlyoutPanel(BurnTKForm moduleGuide, int lessonCount)
        {
            InitializeComponent();
            module = moduleGuide;
            curentLessonIdCore = 0;
            count = lessonCount;
        }

        public string LabelText
        {
            get { return label.Text; }
            set { label.Text = value; }
        }

        public int CurrentLessonIndex
        {
            get { return curentLessonIdCore; }
        }

        private void BtnBoQua_Click(object sender, EventArgs e)
        {
            module.EndTutorial();
        }

        private void BtnTruoc_Click(object sender, EventArgs e)
        {
            curentLessonIdCore--;
            if (curentLessonIdCore < 0)
                curentLessonIdCore = count - 1;
            module.SetLesson(curentLessonIdCore);
        }

        private void BtnSau_Click(object sender, EventArgs e)
        {
            curentLessonIdCore++;
            if (curentLessonIdCore > count - 1)
                curentLessonIdCore = 0;
            module.SetLesson(curentLessonIdCore);
        }
    }
}