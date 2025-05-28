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
            DevExpress.XtraEditors.Controls.EditorButtonImageOptions editorButtonImageOptions1 = new DevExpress.XtraEditors.Controls.EditorButtonImageOptions();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject1 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject2 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject3 = new DevExpress.Utils.SerializableAppearanceObject();
            DevExpress.Utils.SerializableAppearanceObject serializableAppearanceObject4 = new DevExpress.Utils.SerializableAppearanceObject();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.BtnEditAdb = new DevExpress.XtraEditors.ButtonEdit();
            this.BtnProxy = new DevExpress.XtraEditors.SimpleButton();
            this.BtnClearDBSim = new DevExpress.XtraEditors.SimpleButton();
            this.BtnExportExcel = new DevExpress.XtraEditors.SimpleButton();
            this.BtnStopAll = new DevExpress.XtraEditors.SimpleButton();
            this.BtnRunAll = new DevExpress.XtraEditors.SimpleButton();
            this.GcDevice = new DevExpress.XtraGrid.GridControl();
            this.GvDevice = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.Serial = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Proxy = new DevExpress.XtraGrid.Columns.GridColumn();
            this.proxyMemoEdit = new DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit();
            this.Message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Status = new DevExpress.XtraGrid.Columns.GridColumn();
            this.AdbFolderBrowserDialog = new DevExpress.XtraEditors.XtraFolderBrowserDialog(this.components);
            this.loadingPanel = new DevExpress.XtraWaitForm.ProgressPanel();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.BtnEditAdb.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GcDevice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvDevice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.proxyMemoEdit)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.BtnEditAdb);
            this.panelControl1.Controls.Add(this.BtnProxy);
            this.panelControl1.Controls.Add(this.BtnClearDBSim);
            this.panelControl1.Controls.Add(this.BtnExportExcel);
            this.panelControl1.Controls.Add(this.BtnStopAll);
            this.panelControl1.Controls.Add(this.BtnRunAll);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(1022, 105);
            this.panelControl1.TabIndex = 0;
            // 
            // BtnEditAdb
            // 
            this.BtnEditAdb.EditValue = "Nhập đường dẫn đến folder chứa file adb.exe";
            this.BtnEditAdb.Location = new System.Drawing.Point(358, 11);
            this.BtnEditAdb.Name = "BtnEditAdb";
            this.BtnEditAdb.Properties.AutoHeight = false;
            editorButtonImageOptions1.Image = ((System.Drawing.Image)(resources.GetObject("editorButtonImageOptions1.Image")));
            editorButtonImageOptions1.ImageToTextAlignment = DevExpress.XtraEditors.ImageAlignToText.LeftCenter;
            serializableAppearanceObject1.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            serializableAppearanceObject1.ForeColor = System.Drawing.Color.OrangeRed;
            serializableAppearanceObject1.Options.UseFont = true;
            serializableAppearanceObject1.Options.UseForeColor = true;
            this.BtnEditAdb.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Glyph, "Folder Adb", -1, true, true, false, editorButtonImageOptions1, new DevExpress.Utils.KeyShortcut(System.Windows.Forms.Keys.None), serializableAppearanceObject1, serializableAppearanceObject2, serializableAppearanceObject3, serializableAppearanceObject4, "", "Adb", null, DevExpress.Utils.ToolTipAnchor.Default)});
            this.BtnEditAdb.Properties.ButtonsStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.BtnEditAdb.Properties.NullText = "Nhập đường dẫn đến folder chứa file adb.exe";
            this.BtnEditAdb.Properties.TextEditStyle = DevExpress.XtraEditors.Controls.TextEditStyles.DisableTextEditor;
            this.BtnEditAdb.Size = new System.Drawing.Size(400, 40);
            this.BtnEditAdb.TabIndex = 11;
            this.BtnEditAdb.ToolTip = "Nhập đường dẫn đến folder chứa file adb.exe";
            this.BtnEditAdb.ButtonClick += new DevExpress.XtraEditors.Controls.ButtonPressedEventHandler(this.BtnEditAdb_ButtonClick);
            // 
            // BtnProxy
            // 
            this.BtnProxy.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnProxy.Appearance.Options.UseFont = true;
            this.BtnProxy.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnProxy.ImageOptions.Image")));
            this.BtnProxy.Location = new System.Drawing.Point(638, 55);
            this.BtnProxy.Name = "BtnProxy";
            this.BtnProxy.Size = new System.Drawing.Size(120, 40);
            this.BtnProxy.TabIndex = 10;
            this.BtnProxy.Text = "Nhập Proxy";
            this.BtnProxy.Click += new System.EventHandler(this.BtnProxy_Click);
            // 
            // BtnClearDBSim
            // 
            this.BtnClearDBSim.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnClearDBSim.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnClearDBSim.Appearance.Options.UseFont = true;
            this.BtnClearDBSim.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnClearDBSim.ImageOptions.Image")));
            this.BtnClearDBSim.Location = new System.Drawing.Point(890, 10);
            this.BtnClearDBSim.Name = "BtnClearDBSim";
            this.BtnClearDBSim.Size = new System.Drawing.Size(120, 40);
            this.BtnClearDBSim.TabIndex = 3;
            this.BtnClearDBSim.Text = "Xóa Data";
            this.BtnClearDBSim.Click += new System.EventHandler(this.BtnClearDBSim_Click);
            // 
            // BtnExportExcel
            // 
            this.BtnExportExcel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnExportExcel.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnExportExcel.Appearance.Options.UseFont = true;
            this.BtnExportExcel.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnExportExcel.ImageOptions.Image")));
            this.BtnExportExcel.Location = new System.Drawing.Point(764, 10);
            this.BtnExportExcel.Name = "BtnExportExcel";
            this.BtnExportExcel.Size = new System.Drawing.Size(120, 40);
            this.BtnExportExcel.TabIndex = 2;
            this.BtnExportExcel.Text = "Xuất Data";
            this.BtnExportExcel.Click += new System.EventHandler(this.BtnExportExcel_Click);
            // 
            // BtnStopAll
            // 
            this.BtnStopAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnStopAll.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnStopAll.Appearance.Options.UseFont = true;
            this.BtnStopAll.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnStopAll.ImageOptions.Image")));
            this.BtnStopAll.Location = new System.Drawing.Point(890, 55);
            this.BtnStopAll.Name = "BtnStopAll";
            this.BtnStopAll.Size = new System.Drawing.Size(120, 40);
            this.BtnStopAll.TabIndex = 1;
            this.BtnStopAll.Text = "Kết Thúc";
            this.BtnStopAll.Click += new System.EventHandler(this.BtnStopAll_Click);
            // 
            // BtnRunAll
            // 
            this.BtnRunAll.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.BtnRunAll.Appearance.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.BtnRunAll.Appearance.Options.UseFont = true;
            this.BtnRunAll.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnRunAll.ImageOptions.Image")));
            this.BtnRunAll.Location = new System.Drawing.Point(764, 55);
            this.BtnRunAll.Name = "BtnRunAll";
            this.BtnRunAll.Size = new System.Drawing.Size(120, 40);
            this.BtnRunAll.TabIndex = 0;
            this.BtnRunAll.Text = "Bắt Đầu";
            this.BtnRunAll.Click += new System.EventHandler(this.BtnRunAll_Click);
            // 
            // GcDevice
            // 
            this.GcDevice.Dock = System.Windows.Forms.DockStyle.Fill;
            this.GcDevice.Location = new System.Drawing.Point(0, 105);
            this.GcDevice.MainView = this.GvDevice;
            this.GcDevice.Name = "GcDevice";
            this.GcDevice.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.proxyMemoEdit});
            this.GcDevice.Size = new System.Drawing.Size(1022, 631);
            this.GcDevice.TabIndex = 1;
            this.GcDevice.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.GvDevice});
            // 
            // GvDevice
            // 
            this.GvDevice.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.Serial,
            this.Proxy,
            this.Message,
            this.Status});
            this.GvDevice.DetailHeight = 325;
            this.GvDevice.GridControl = this.GcDevice;
            this.GvDevice.Name = "GvDevice";
            this.GvDevice.OptionsSelection.EnableAppearanceFocusedRow = false;
            this.GvDevice.OptionsView.ShowGroupPanel = false;
            this.GvDevice.RowHeight = 37;
            // 
            // Serial
            // 
            this.Serial.AppearanceCell.Options.UseTextOptions = true;
            this.Serial.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Serial.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.Serial.AppearanceHeader.Options.UseFont = true;
            this.Serial.Caption = "Device";
            this.Serial.FieldName = "Serial";
            this.Serial.MinWidth = 17;
            this.Serial.Name = "Serial";
            this.Serial.OptionsColumn.AllowEdit = false;
            this.Serial.OptionsColumn.FixedWidth = true;
            this.Serial.OptionsFilter.AllowAutoFilter = false;
            this.Serial.Visible = true;
            this.Serial.VisibleIndex = 0;
            this.Serial.Width = 103;
            // 
            // Proxy
            // 
            this.Proxy.AppearanceCell.Options.UseTextOptions = true;
            this.Proxy.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Proxy.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.Proxy.AppearanceHeader.Options.UseFont = true;
            this.Proxy.Caption = "Proxy";
            this.Proxy.ColumnEdit = this.proxyMemoEdit;
            this.Proxy.FieldName = "Proxy";
            this.Proxy.MinWidth = 17;
            this.Proxy.Name = "Proxy";
            this.Proxy.OptionsFilter.AllowAutoFilter = false;
            this.Proxy.Visible = true;
            this.Proxy.VisibleIndex = 1;
            this.Proxy.Width = 343;
            // 
            // proxyMemoEdit
            // 
            this.proxyMemoEdit.Name = "proxyMemoEdit";
            // 
            // Message
            // 
            this.Message.AppearanceCell.Options.UseTextOptions = true;
            this.Message.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.Message.AppearanceHeader.Options.UseFont = true;
            this.Message.Caption = "Thông báo";
            this.Message.FieldName = "Message";
            this.Message.MinWidth = 17;
            this.Message.Name = "Message";
            this.Message.OptionsColumn.AllowEdit = false;
            this.Message.Visible = true;
            this.Message.VisibleIndex = 2;
            this.Message.Width = 393;
            // 
            // Status
            // 
            this.Status.AppearanceCell.Options.UseTextOptions = true;
            this.Status.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.Status.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Status.AppearanceHeader.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.Status.AppearanceHeader.Options.UseFont = true;
            this.Status.AppearanceHeader.Options.UseTextOptions = true;
            this.Status.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.Status.Caption = "Trạng thái";
            this.Status.FieldName = "Status";
            this.Status.MinWidth = 17;
            this.Status.Name = "Status";
            this.Status.OptionsColumn.AllowEdit = false;
            this.Status.OptionsColumn.FixedWidth = true;
            this.Status.OptionsFilter.AllowAutoFilter = false;
            this.Status.Visible = true;
            this.Status.VisibleIndex = 3;
            this.Status.Width = 86;
            // 
            // AdbFolderBrowserDialog
            // 
            this.AdbFolderBrowserDialog.SelectedPath = "xtraFolderBrowserDialog1";
            this.AdbFolderBrowserDialog.ShowNewFolderButton = false;
            // 
            // loadingPanel
            // 
            this.loadingPanel.Appearance.BackColor = System.Drawing.Color.White;
            this.loadingPanel.Appearance.Options.UseBackColor = true;
            this.loadingPanel.AppearanceCaption.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.loadingPanel.AppearanceCaption.Options.UseFont = true;
            this.loadingPanel.Caption = "... Đang tải ...";
            this.loadingPanel.ContentAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.loadingPanel.Location = new System.Drawing.Point(419, 351);
            this.loadingPanel.Name = "loadingPanel";
            this.loadingPanel.ShowDescription = false;
            this.loadingPanel.Size = new System.Drawing.Size(124, 38);
            this.loadingPanel.TabIndex = 2;
            this.loadingPanel.Text = "progressPanel1";
            this.loadingPanel.Visible = false;
            this.loadingPanel.WaitAnimationType = DevExpress.Utils.Animation.WaitingAnimatorType.Bar;
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1022, 736);
            this.Controls.Add(this.loadingPanel);
            this.Controls.Add(this.GcDevice);
            this.Controls.Add(this.panelControl1);
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("Main.IconOptions.Image")));
            this.MaximizeBox = false;
            this.Name = "Main";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Luck LDPlayer Telegram";
            this.Load += new System.EventHandler(this.Main_Load);
            this.Shown += new System.EventHandler(this.Main_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.BtnEditAdb.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GcDevice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.GvDevice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.proxyMemoEdit)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton BtnExportExcel;
        private DevExpress.XtraEditors.SimpleButton BtnStopAll;
        private DevExpress.XtraEditors.SimpleButton BtnRunAll;
        private DevExpress.XtraEditors.SimpleButton BtnClearDBSim;
        private DevExpress.XtraGrid.GridControl GcDevice;
        private DevExpress.XtraGrid.Views.Grid.GridView GvDevice;
        private DevExpress.XtraGrid.Columns.GridColumn Serial;
        private DevExpress.XtraGrid.Columns.GridColumn Proxy;
        private DevExpress.XtraGrid.Columns.GridColumn Message;
        private DevExpress.XtraGrid.Columns.GridColumn Status;
        private DevExpress.XtraEditors.XtraFolderBrowserDialog AdbFolderBrowserDialog;
        private DevExpress.XtraWaitForm.ProgressPanel loadingPanel;
        private DevExpress.XtraEditors.SimpleButton BtnProxy;
        private DevExpress.XtraEditors.Repository.RepositoryItemMemoEdit proxyMemoEdit;
        private DevExpress.XtraEditors.ButtonEdit BtnEditAdb;
    }
}