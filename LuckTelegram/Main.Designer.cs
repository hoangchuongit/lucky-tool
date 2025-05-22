namespace LuckTelegram
{
    partial class Main
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            DevExpress.XtraEditors.Controls.EditorButtonImageOptions editorButtonImageOptions3 = new DevExpress.XtraEditors.Controls.EditorButtonImageOptions();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject9 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject10 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject11 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject12 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.XtraEditors.Controls.EditorButtonImageOptions editorButtonImageOptions4 = new DevExpress.XtraEditors.Controls.EditorButtonImageOptions();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject13 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject14 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject15 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject16 = new DevExpress.Utils.SerializableAppearanceObject();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.TxtFolderADB = new DevExpress.XtraEditors.TextEdit();
            this.BtnChooseFolderADB = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.comboBoxEdit1 = new DevExpress.XtraEditors.ComboBoxEdit();
            this.checkEdit2 = new DevExpress.XtraEditors.CheckEdit();
            this.checkEdit1 = new DevExpress.XtraEditors.CheckEdit();
            this.simpleButton4 = new DevExpress.XtraEditors.SimpleButton();
            this.simpleButton3 = new DevExpress.XtraEditors.SimpleButton();
            this.BtnStopAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRunAll = new DevExpress.XtraEditors.SimpleButton();
            this.GcDevice = new DevExpress.XtraGrid.GridControl();
            this.GvDevice = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.serial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.proxy = new DevExpress.XtraGrid.Columns.GridColumn();
            this.message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.isRunning = new DevExpress.XtraGrid.Columns.GridColumn();
            this.GvBtnAction = new DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit();
            this.AdbFolderBrowserDialog = new DevExpress.XtraEditors.XtraFolderBrowserDialog(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFolderADB.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit2.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit1.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GcDevice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvDevice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvBtnAction)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.TxtFolderADB);
            this.panelControl1.Controls.Add(this.BtnChooseFolderADB);
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Controls.Add(this.comboBoxEdit1);
            this.panelControl1.Controls.Add(this.checkEdit2);
            this.panelControl1.Controls.Add(this.checkEdit1);
            this.panelControl1.Controls.Add(this.simpleButton4);
            this.panelControl1.Controls.Add(this.simpleButton3);
            this.panelControl1.Controls.Add(this.BtnStopAll);
            this.panelControl1.Controls.Add(this.BtnRunAll);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(1022, 88);
            this.panelControl1.TabIndex = 0;
            // 
            // TxtFolderADB
            // 
            this.TxtFolderADB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TxtFolderADB.Location = new System.Drawing.Point(122, 11);
            this.TxtFolderADB.Name = "TxtFolderADB";
            this.TxtFolderADB.Properties.AutoHeight = false;
            this.TxtFolderADB.Size = new System.Drawing.Size(573, 30);
            this.TxtFolderADB.TabIndex = 9;
            // 
            // BtnChooseFolderADB
            // 
            this.BtnChooseFolderADB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnChooseFolderADB.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.BtnChooseFolderADB.Appearance.Options.UseFont = true;
            this.BtnChooseFolderADB.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnChooseFolderADB.ImageOptions.Image")));
            this.BtnChooseFolderADB.Location = new System.Drawing.Point(12, 11);
            this.BtnChooseFolderADB.Name = "BtnChooseFolderADB";
            this.BtnChooseFolderADB.Size = new System.Drawing.Size(104, 30);
            this.BtnChooseFolderADB.TabIndex = 8;
            this.BtnChooseFolderADB.Text = "Folder ADB";
            this.BtnChooseFolderADB.ToolTip = "Chọn folder chứa ADB";
            this.BtnChooseFolderADB.Click += new System.EventHandler(this.BtnChooseFolderADB_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(37, 55);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(79, 14);
            this.labelControl1.TabIndex = 7;
            this.labelControl1.Text = "Đơn vị Gmail";
            // 
            // comboBoxEdit1
            // 
            this.comboBoxEdit1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxEdit1.EditValue = "Tất cả";
            this.comboBoxEdit1.Location = new System.Drawing.Point(122, 47);
            this.comboBoxEdit1.Name = "comboBoxEdit1";
            this.comboBoxEdit1.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.comboBoxEdit1.Properties.Appearance.Options.UseFont = true;
            this.comboBoxEdit1.Properties.AutoHeight = false;
            this.comboBoxEdit1.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.comboBoxEdit1.Properties.Items.AddRange(new object[] {
            "Tất cả",
            "shopgmail",
            "sptmail"});
            this.comboBoxEdit1.Size = new System.Drawing.Size(129, 30);
            this.comboBoxEdit1.TabIndex = 6;
            // 
            // checkEdit2
            // 
            this.checkEdit2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkEdit2.Location = new System.Drawing.Point(353, 52);
            this.checkEdit2.Name = "checkEdit2";
            this.checkEdit2.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkEdit2.Properties.Appearance.Options.UseFont = true;
            this.checkEdit2.Properties.Caption = "Tự động reset tài khoản 2FA";
            this.checkEdit2.Size = new System.Drawing.Size(175, 20);
            this.checkEdit2.TabIndex = 5;
            this.checkEdit2.ToolTip = "Kiểm tra trạng thái Telegram của từng Sim - Không thực hiện Reg tài khoản Telegra" +
    "m";
            // 
            // checkEdit1
            // 
            this.checkEdit1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkEdit1.Location = new System.Drawing.Point(257, 52);
            this.checkEdit1.Name = "checkEdit1";
            this.checkEdit1.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.checkEdit1.Properties.Appearance.Options.UseFont = true;
            this.checkEdit1.Properties.Caption = "Check Sim";
            this.checkEdit1.Size = new System.Drawing.Size(90, 20);
            this.checkEdit1.TabIndex = 4;
            this.checkEdit1.ToolTip = "Kiểm tra trạng thái Telegram của từng Sim - Không thực hiện Reg tài khoản Telegra" +
    "m";
            // 
            // simpleButton4
            // 
            this.simpleButton4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.simpleButton4.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.simpleButton4.Appearance.Options.UseFont = true;
            this.simpleButton4.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton4.ImageOptions.Image")));
            this.simpleButton4.Location = new System.Drawing.Point(862, 11);
            this.simpleButton4.Name = "simpleButton4";
            this.simpleButton4.Size = new System.Drawing.Size(150, 30);
            this.simpleButton4.TabIndex = 3;
            this.simpleButton4.Text = "Xóa Data Phones";
            // 
            // simpleButton3
            // 
            this.simpleButton3.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.simpleButton3.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.simpleButton3.Appearance.Options.UseFont = true;
            this.simpleButton3.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton3.ImageOptions.Image")));
            this.simpleButton3.Location = new System.Drawing.Point(706, 11);
            this.simpleButton3.Name = "simpleButton3";
            this.simpleButton3.Size = new System.Drawing.Size(150, 30);
            this.simpleButton3.TabIndex = 2;
            this.simpleButton3.Text = "Xuất Data Phones";
            // 
            // BtnStopAll
            // 
            this.BtnStopAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnStopAll.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.BtnStopAll.Appearance.Options.UseFont = true;
            this.BtnStopAll.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton2.ImageOptions.Image")));
            this.BtnStopAll.Location = new System.Drawing.Point(862, 47);
            this.BtnStopAll.Name = "BtnStopAll";
            this.BtnStopAll.Size = new System.Drawing.Size(150, 30);
            this.BtnStopAll.TabIndex = 1;
            this.BtnStopAll.Text = "Dừng tất cả";
            // 
            // BtnRunAll
            // 
            this.BtnRunAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRunAll.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.BtnRunAll.Appearance.Options.UseFont = true;
            this.BtnRunAll.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("simpleButton1.ImageOptions.Image")));
            this.BtnRunAll.Location = new System.Drawing.Point(706, 47);
            this.BtnRunAll.Name = "BtnRunAll";
            this.BtnRunAll.Size = new System.Drawing.Size(150, 30);
            this.BtnRunAll.TabIndex = 0;
            this.BtnRunAll.Text = "Chạy tất cả";
            // 
            // GcDevice
            // 
            this.GcDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GcDevice.Location = new System.Drawing.Point(0, 88);
            this.GcDevice.MainView = this.GvDevice;
            this.GcDevice.Name = "GcDevice";
            this.GcDevice.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.GvBtnAction});
            this.GcDevice.Size = new System.Drawing.Size(1022, 648);
            this.GcDevice.TabIndex = 1;
            this.GcDevice.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GvDevice});
            // 
            // GvDevice
            // 
            this.GvDevice.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.serial,
            this.proxy,
            this.message,
            this.isRunning});
            this.GvDevice.DetailHeight = 325;
            this.GvDevice.GridControl = this.GcDevice;
            this.GvDevice.Name = "GvDevice";
            this.GvDevice.OptionsView.ShowGroupPanel = false;
            // 
            // serial
            // 
            this.serial.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.serial.AppearanceHeader.Options.UseFont = true;
            this.serial.Caption = "Device";
            this.serial.FieldName = "serial";
            this.serial.MinWidth = 17;
            this.serial.Name = "serial";
            this.serial.OptionsColumn.AllowEdit = false;
            this.serial.OptionsColumn.FixedWidth = true;
            this.serial.OptionsFilter.AllowAutoFilter = false;
            this.serial.Visible = true;
            this.serial.VisibleIndex = 0;
            this.serial.Width = 150;
            // 
            // proxy
            // 
            this.proxy.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.proxy.AppearanceHeader.Options.UseFont = true;
            this.proxy.Caption = "Proxy";
            this.proxy.FieldName = "proxy";
            this.proxy.MinWidth = 17;
            this.proxy.Name = "proxy";
            this.proxy.OptionsColumn.AllowEdit = false;
            this.proxy.OptionsColumn.FixedWidth = true;
            this.proxy.OptionsFilter.AllowAutoFilter = false;
            this.proxy.Visible = true;
            this.proxy.VisibleIndex = 1;
            this.proxy.Width = 150;
            // 
            // message
            // 
            this.message.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.message.AppearanceHeader.Options.UseFont = true;
            this.message.Caption = "Thông báo";
            this.message.FieldName = "message";
            this.message.MinWidth = 17;
            this.message.Name = "message";
            this.message.OptionsColumn.AllowEdit = false;
            this.message.Visible = true;
            this.message.VisibleIndex = 2;
            this.message.Width = 594;
            // 
            // isRunning
            // 
            this.isRunning.AppearanceCell.Options.UseTextOptions = true;
            this.isRunning.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.isRunning.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.isRunning.AppearanceHeader.Options.UseFont = true;
            this.isRunning.AppearanceHeader.Options.UseTextOptions = true;
            this.isRunning.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.isRunning.Caption = " ";
            this.isRunning.ColumnEdit = this.GvBtnAction;
            this.isRunning.FieldName = "isRunning";
            this.isRunning.MinWidth = 17;
            this.isRunning.Name = "isRunning";
            this.isRunning.OptionsColumn.AllowEdit = false;
            this.isRunning.OptionsColumn.FixedWidth = true;
            this.isRunning.OptionsFilter.AllowAutoFilter = false;
            this.isRunning.Visible = true;
            this.isRunning.VisibleIndex = 3;
            this.isRunning.Width = 103;
            // 
            // GvBtnAction
            // 
            this.GvBtnAction.AutoHeight = false;
            this.GvBtnAction.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis, "Chạy", -1, true, true, false, editorButtonImageOptions3, new DevExpress.Utils.KeyShortcut(System.Windows.Forms.Keys.None), serializableAppearanceObject9, serializableAppearanceObject10, serializableAppearanceObject11, serializableAppearanceObject12, "", "Start", null, DevExpress.Utils.ToolTipAnchor.Default),
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Ellipsis, "Dừng", -1, true, true, false, editorButtonImageOptions4, new DevExpress.Utils.KeyShortcut(System.Windows.Forms.Keys.None), serializableAppearanceObject13, serializableAppearanceObject14, serializableAppearanceObject15, serializableAppearanceObject16, "", "Stop", null, DevExpress.Utils.ToolTipAnchor.Default)});
            this.GvBtnAction.Name = "GvBtnAction";
            // 
            // AdbFolderBrowserDialog
            // 
            this.AdbFolderBrowserDialog.SelectedPath = "xtraFolderBrowserDialog1";
            this.AdbFolderBrowserDialog.ShowNewFolderButton = false;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1022, 736);
            this.Controls.Add(this.GcDevice);
            this.Controls.Add(this.panelControl1);
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("Main.IconOptions.Image")));
            this.MaximizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Luck LDPlayer Telegram";
            this.Load += new System.EventHandler(this.Main_Load);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TxtFolderADB.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.comboBoxEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit2.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.checkEdit1.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GcDevice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvDevice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvBtnAction)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton simpleButton3;
        private DevExpress.XtraEditors.SimpleButton BtnStopAll;
        private DevExpress.XtraEditors.SimpleButton BtnRunAll;
        private DevExpress.XtraEditors.SimpleButton simpleButton4;
        private DevExpress.XtraEditors.CheckEdit checkEdit1;
        private DevExpress.XtraEditors.CheckEdit checkEdit2;
        private DevExpress.XtraEditors.ComboBoxEdit comboBoxEdit1;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraGrid.GridControl GcDevice;
        private DevExpress.XtraGrid.Views.Grid.GridView GvDevice;
        private DevExpress.XtraGrid.Columns.GridColumn serial;
        private DevExpress.XtraGrid.Columns.GridColumn proxy;
        private DevExpress.XtraGrid.Columns.GridColumn message;
        private DevExpress.XtraGrid.Columns.GridColumn isRunning;
        private DevExpress.XtraEditors.Repository.RepositoryItemButtonEdit GvBtnAction;
        private DevExpress.XtraEditors.XtraFolderBrowserDialog AdbFolderBrowserDialog;
        private DevExpress.XtraEditors.TextEdit TxtFolderADB;
        private DevExpress.XtraEditors.SimpleButton BtnChooseFolderADB;
    }
}