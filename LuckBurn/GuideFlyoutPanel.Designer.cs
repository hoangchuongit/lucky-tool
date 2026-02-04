namespace LuckBurnTK
{
    partial class GuideFlyoutPanel
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.BtnBoQua = new DevExpress.XtraEditors.SimpleButton();
            this.BtnSau = new DevExpress.XtraEditors.SimpleButton();
            this.btnTruoc = new DevExpress.XtraEditors.SimpleButton();
            this.label = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Controls.Add(this.BtnBoQua);
            this.panelControl1.Controls.Add(this.BtnSau);
            this.panelControl1.Controls.Add(this.btnTruoc);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelControl1.Location = new System.Drawing.Point(0, 167);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(400, 33);
            this.panelControl1.TabIndex = 2;
            // 
            // BtnBoQua
            // 
            this.BtnBoQua.Location = new System.Drawing.Point(5, 5);
            this.BtnBoQua.Name = "BtnBoQua";
            this.BtnBoQua.Size = new System.Drawing.Size(46, 23);
            this.BtnBoQua.TabIndex = 2;
            this.BtnBoQua.Text = "Bỏ qua";
            this.BtnBoQua.Click += new System.EventHandler(this.BtnBoQua_Click);
            // 
            // BtnSau
            // 
            this.BtnSau.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnSau.Location = new System.Drawing.Point(349, 5);
            this.BtnSau.Name = "BtnSau";
            this.BtnSau.Size = new System.Drawing.Size(46, 23);
            this.BtnSau.TabIndex = 1;
            this.BtnSau.Text = "Sau";
            this.BtnSau.Click += new System.EventHandler(this.BtnSau_Click);
            // 
            // btnTruoc
            // 
            this.btnTruoc.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnTruoc.Location = new System.Drawing.Point(297, 5);
            this.btnTruoc.Name = "btnTruoc";
            this.btnTruoc.Size = new System.Drawing.Size(46, 23);
            this.btnTruoc.TabIndex = 0;
            this.btnTruoc.Text = "Trước";
            this.btnTruoc.Click += new System.EventHandler(this.BtnTruoc_Click);
            // 
            // label
            // 
            this.label.AllowHtmlString = true;
            this.label.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label.Appearance.Options.UseFont = true;
            this.label.Appearance.Options.UseTextOptions = true;
            this.label.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.label.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.label.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.label.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
            this.label.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label.Location = new System.Drawing.Point(0, 0);
            this.label.Name = "label";
            this.label.Padding = new System.Windows.Forms.Padding(10);
            this.label.Size = new System.Drawing.Size(400, 167);
            this.label.TabIndex = 6;
            this.label.Text = "Empty";
            // 
            // GuideFlyoutPanel
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.Controls.Add(this.label);
            this.Controls.Add(this.panelControl1);
            this.Name = "GuideFlyoutPanel";
            this.Size = new System.Drawing.Size(400, 200);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton BtnBoQua;
        private DevExpress.XtraEditors.SimpleButton BtnSau;
        private DevExpress.XtraEditors.SimpleButton btnTruoc;
        private DevExpress.XtraEditors.LabelControl label;
    }
}
