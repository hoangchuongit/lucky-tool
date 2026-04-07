namespace LuckBurn
{
    partial class ChangePassForm
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ChangePassForm));
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.btnLogin = new DevExpress.XtraEditors.SimpleButton();
            this.txtPassNew = new DevExpress.XtraEditors.TextEdit();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.txtPassOld = new DevExpress.XtraEditors.TextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassNew.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassOld.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(13, 68);
            this.labelControl2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(87, 13);
            this.labelControl2.TabIndex = 14;
            this.labelControl2.Text = "Mật khẩu mới";
            // 
            // btnLogin
            // 
            this.btnLogin.Appearance.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnLogin.Appearance.Options.UseFont = true;
            this.btnLogin.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnLogin.AppearanceHovered.Options.UseBackColor = true;
            this.btnLogin.ImageOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("btnLogin.ImageOptions.SvgImage")));
            this.btnLogin.Location = new System.Drawing.Point(13, 127);
            this.btnLogin.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(230, 30);
            this.btnLogin.TabIndex = 12;
            this.btnLogin.Text = "ĐỔI MẬT KHẨU";
            this.btnLogin.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // txtPassNew
            // 
            this.txtPassNew.EditValue = "";
            this.txtPassNew.Location = new System.Drawing.Point(13, 88);
            this.txtPassNew.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtPassNew.Name = "txtPassNew";
            this.txtPassNew.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPassNew.Properties.Appearance.Options.UseFont = true;
            this.txtPassNew.Properties.AutoHeight = false;
            this.txtPassNew.Properties.NullText = "Nhập mật khẩu";
            this.txtPassNew.Properties.PasswordChar = '*';
            this.txtPassNew.Properties.UseSystemPasswordChar = true;
            this.txtPassNew.Size = new System.Drawing.Size(230, 30);
            this.txtPassNew.TabIndex = 10;
            // 
            // panelControl1
            // 
            this.panelControl1.Appearance.BackColor = System.Drawing.Color.White;
            this.panelControl1.Appearance.Options.UseBackColor = true;
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Controls.Add(this.txtPassOld);
            this.panelControl1.Controls.Add(this.labelControl2);
            this.panelControl1.Controls.Add(this.txtPassNew);
            this.panelControl1.Controls.Add(this.btnLogin);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(260, 174);
            this.panelControl1.TabIndex = 15;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(13, 12);
            this.labelControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(78, 13);
            this.labelControl1.TabIndex = 16;
            this.labelControl1.Text = "Mật khẩu cũ";
            // 
            // txtPassOld
            // 
            this.txtPassOld.EditValue = "";
            this.txtPassOld.Location = new System.Drawing.Point(13, 32);
            this.txtPassOld.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtPassOld.Name = "txtPassOld";
            this.txtPassOld.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtPassOld.Properties.Appearance.Options.UseFont = true;
            this.txtPassOld.Properties.AutoHeight = false;
            this.txtPassOld.Properties.NullText = "Nhập mật khẩu";
            this.txtPassOld.Properties.PasswordChar = '*';
            this.txtPassOld.Properties.UseSystemPasswordChar = true;
            this.txtPassOld.Size = new System.Drawing.Size(230, 30);
            this.txtPassOld.TabIndex = 15;
            // 
            // ChangePassForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(260, 174);
            this.Controls.Add(this.panelControl1);
            this.FormBorderEffect = DevExpress.XtraEditors.FormBorderEffect.Shadow;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("ChangePassForm.IconOptions.Image")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ChangePassForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Đổi Mật Khẩu";
            ((System.ComponentModel.ISupportInitialize)(this.txtPassNew.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtPassOld.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.SimpleButton btnLogin;
        private DevExpress.XtraEditors.TextEdit txtPassNew;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit txtPassOld;
    }
}