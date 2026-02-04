namespace LuckBurnTK
{
    partial class UserInforForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UserInforForm));
            this.btnUpdate = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.txtFullname = new DevExpress.XtraEditors.TextEdit();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.txtChuTaiKhoan = new DevExpress.XtraEditors.TextEdit();
            this.labelControl4 = new DevExpress.XtraEditors.LabelControl();
            this.txtSoTaiKhoan = new DevExpress.XtraEditors.TextEdit();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.txtNganHang = new DevExpress.XtraEditors.TextEdit();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.txtFullname.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChuTaiKhoan.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSoTaiKhoan.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNganHang.Properties)).BeginInit();
            this.SuspendLayout();
            // 
            // btnUpdate
            // 
            this.btnUpdate.Appearance.Font = new System.Drawing.Font("Verdana", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUpdate.Appearance.Options.UseFont = true;
            this.btnUpdate.AppearanceHovered.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(128)))), ((int)(((byte)(0)))));
            this.btnUpdate.AppearanceHovered.Options.UseBackColor = true;
            this.btnUpdate.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdate.ImageOptions.Image")));
            this.btnUpdate.Location = new System.Drawing.Point(14, 239);
            this.btnUpdate.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.btnUpdate.Name = "btnUpdate";
            this.btnUpdate.Size = new System.Drawing.Size(268, 36);
            this.btnUpdate.TabIndex = 12;
            this.btnUpdate.Text = "CẬP NHẬT";
            this.btnUpdate.Click += new System.EventHandler(this.BtnLogin_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(15, 12);
            this.labelControl1.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(62, 13);
            this.labelControl1.TabIndex = 11;
            this.labelControl1.Text = "Họ và tên";
            // 
            // txtFullname
            // 
            this.txtFullname.EditValue = "";
            this.txtFullname.Location = new System.Drawing.Point(15, 29);
            this.txtFullname.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.txtFullname.Name = "txtFullname";
            this.txtFullname.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtFullname.Properties.Appearance.Options.UseFont = true;
            this.txtFullname.Properties.AutoHeight = false;
            this.txtFullname.Properties.NullText = "Nhập họ và tên";
            this.txtFullname.Size = new System.Drawing.Size(268, 30);
            this.txtFullname.TabIndex = 9;
            // 
            // panelControl1
            // 
            this.panelControl1.Appearance.BackColor = System.Drawing.Color.White;
            this.panelControl1.Appearance.Options.UseBackColor = true;
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.txtChuTaiKhoan);
            this.panelControl1.Controls.Add(this.labelControl4);
            this.panelControl1.Controls.Add(this.txtSoTaiKhoan);
            this.panelControl1.Controls.Add(this.labelControl3);
            this.panelControl1.Controls.Add(this.txtNganHang);
            this.panelControl1.Controls.Add(this.labelControl2);
            this.panelControl1.Controls.Add(this.txtFullname);
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Controls.Add(this.btnUpdate);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(298, 291);
            this.panelControl1.TabIndex = 15;
            // 
            // txtChuTaiKhoan
            // 
            this.txtChuTaiKhoan.EditValue = "";
            this.txtChuTaiKhoan.Location = new System.Drawing.Point(14, 194);
            this.txtChuTaiKhoan.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.txtChuTaiKhoan.Name = "txtChuTaiKhoan";
            this.txtChuTaiKhoan.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtChuTaiKhoan.Properties.Appearance.Options.UseFont = true;
            this.txtChuTaiKhoan.Properties.AutoHeight = false;
            this.txtChuTaiKhoan.Properties.NullText = "Nhập chủ tài khoản";
            this.txtChuTaiKhoan.Size = new System.Drawing.Size(268, 30);
            this.txtChuTaiKhoan.TabIndex = 17;
            // 
            // labelControl4
            // 
            this.labelControl4.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl4.Appearance.Options.UseFont = true;
            this.labelControl4.Location = new System.Drawing.Point(14, 175);
            this.labelControl4.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.labelControl4.Name = "labelControl4";
            this.labelControl4.Size = new System.Drawing.Size(89, 13);
            this.labelControl4.TabIndex = 18;
            this.labelControl4.Text = "Chủ tài khoản";
            // 
            // txtSoTaiKhoan
            // 
            this.txtSoTaiKhoan.EditValue = "";
            this.txtSoTaiKhoan.Location = new System.Drawing.Point(15, 139);
            this.txtSoTaiKhoan.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.txtSoTaiKhoan.Name = "txtSoTaiKhoan";
            this.txtSoTaiKhoan.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSoTaiKhoan.Properties.Appearance.Options.UseFont = true;
            this.txtSoTaiKhoan.Properties.AutoHeight = false;
            this.txtSoTaiKhoan.Properties.NullText = "Nhập số tài khoản";
            this.txtSoTaiKhoan.Size = new System.Drawing.Size(268, 30);
            this.txtSoTaiKhoan.TabIndex = 15;
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(14, 120);
            this.labelControl3.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(81, 13);
            this.labelControl3.TabIndex = 16;
            this.labelControl3.Text = "Số tài khoản";
            // 
            // txtNganHang
            // 
            this.txtNganHang.EditValue = "";
            this.txtNganHang.Location = new System.Drawing.Point(15, 84);
            this.txtNganHang.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.txtNganHang.Name = "txtNganHang";
            this.txtNganHang.Properties.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtNganHang.Properties.Appearance.Options.UseFont = true;
            this.txtNganHang.Properties.AutoHeight = false;
            this.txtNganHang.Properties.NullText = "Nhập ngân hàng";
            this.txtNganHang.Size = new System.Drawing.Size(268, 30);
            this.txtNganHang.TabIndex = 13;
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Location = new System.Drawing.Point(14, 65);
            this.labelControl2.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(69, 13);
            this.labelControl2.TabIndex = 14;
            this.labelControl2.Text = "Ngân hàng";
            // 
            // UserInforForm
            // 
            this.Appearance.Options.UseFont = true;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(298, 291);
            this.Controls.Add(this.panelControl1);
            this.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderEffect = DevExpress.XtraEditors.FormBorderEffect.Shadow;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("UserInforForm.IconOptions.Image")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "UserInforForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Thông tin tài khoản";
            ((System.ComponentModel.ISupportInitialize)(this.txtFullname.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtChuTaiKhoan.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSoTaiKhoan.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtNganHang.Properties)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private DevExpress.XtraEditors.SimpleButton btnUpdate;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.TextEdit txtFullname;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.TextEdit txtChuTaiKhoan;
        private DevExpress.XtraEditors.LabelControl labelControl4;
        private DevExpress.XtraEditors.TextEdit txtSoTaiKhoan;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.TextEdit txtNganHang;
        private DevExpress.XtraEditors.LabelControl labelControl2;
    }
}