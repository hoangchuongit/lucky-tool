namespace LuckOTP
{
    partial class GSMForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GSMForm));
            this.timerCheckSimError = new System.Windows.Forms.Timer(this.components);
            this.gridControlModems = new DevExpress.XtraGrid.GridControl();
            this.gridViewModems = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.Com = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Stt = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemComboBox1 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.ICCID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Phone = new DevExpress.XtraGrid.Columns.GridColumn();
            this.TrangThai = new DevExpress.XtraGrid.Columns.GridColumn();
            this.TeleStatus = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.gridView1 = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.btnUpdateNo = new DevExpress.XtraEditors.SimpleButton();
            this.btnResetNo = new DevExpress.XtraEditors.SimpleButton();
            this.btnResetCom = new DevExpress.XtraEditors.SimpleButton();
            this.btnRestoreFactoryCom = new DevExpress.XtraEditors.SimpleButton();
            this.btnImportSimData = new DevExpress.XtraEditors.SimpleButton();
            this.panelToolbar = new DevExpress.XtraEditors.PanelControl();
            this.btnClearSimData = new DevExpress.XtraEditors.SimpleButton();
            this.lblPhoneCount = new DevExpress.XtraEditors.LabelControl();
            ((System.ComponentModel.ISupportInitialize)(this.gridControlModems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewModems)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelToolbar)).BeginInit();
            this.panelToolbar.SuspendLayout();
            this.SuspendLayout();
            // 
            // timerCheckSimError
            // 
            this.timerCheckSimError.Interval = 10000;
            this.timerCheckSimError.Tick += new System.EventHandler(this.timerCheckSimError_Tick);
            // 
            // gridControlModems
            // 
            this.gridControlModems.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gridControlModems.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gridControlModems.Location = new System.Drawing.Point(0, 50);
            this.gridControlModems.MainView = this.gridViewModems;
            this.gridControlModems.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gridControlModems.Name = "gridControlModems";
            this.gridControlModems.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemComboBox1});
            this.gridControlModems.Size = new System.Drawing.Size(1384, 706);
            this.gridControlModems.TabIndex = 4;
            this.gridControlModems.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gridViewModems});
            // 
            // gridViewModems
            // 
            this.gridViewModems.AppearancePrint.FooterPanel.Font = new System.Drawing.Font("Tahoma", 14F);
            this.gridViewModems.AppearancePrint.FooterPanel.Options.UseFont = true;
            this.gridViewModems.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.Com,
            this.Stt,
            this.ICCID,
            this.Phone,
            this.TrangThai,
            this.TeleStatus,
            this.Message});
            this.gridViewModems.GridControl = this.gridControlModems;
            this.gridViewModems.Name = "gridViewModems";
            this.gridViewModems.OptionsSelection.MultiSelect = true;
            this.gridViewModems.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CellSelect;
            this.gridViewModems.OptionsView.RowAutoHeight = true;
            this.gridViewModems.OptionsView.ShowGroupPanel = false;
            this.gridViewModems.OptionsView.ShowHorizontalLines = DevExpress.Utils.DefaultBoolean.True;
            this.gridViewModems.OptionsView.ShowIndicator = false;
            this.gridViewModems.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.gridViewModems_RowCellStyle);
            // 
            // Com
            // 
            this.Com.AppearanceCell.Options.UseTextOptions = true;
            this.Com.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.Com.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Com.AppearanceHeader.Options.UseTextOptions = true;
            this.Com.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.Com.Caption = "COM";
            this.Com.FieldName = "Com";
            this.Com.MinWidth = 23;
            this.Com.Name = "Com";
            this.Com.OptionsColumn.AllowEdit = false;
            this.Com.OptionsColumn.FixedWidth = true;
            this.Com.Visible = true;
            this.Com.VisibleIndex = 0;
            this.Com.Width = 55;
            // 
            // Stt
            // 
            this.Stt.AppearanceCell.Options.UseTextOptions = true;
            this.Stt.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.Stt.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Stt.AppearanceHeader.Options.UseTextOptions = true;
            this.Stt.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.Stt.Caption = "No.";
            this.Stt.ColumnEdit = this.repositoryItemComboBox1;
            this.Stt.FieldName = "Stt";
            this.Stt.MinWidth = 23;
            this.Stt.Name = "Stt";
            this.Stt.OptionsColumn.FixedWidth = true;
            this.Stt.Visible = true;
            this.Stt.VisibleIndex = 1;
            this.Stt.Width = 55;
            // 
            // repositoryItemComboBox1
            // 
            this.repositoryItemComboBox1.AutoHeight = false;
            this.repositoryItemComboBox1.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.repositoryItemComboBox1.DropDownRows = 10;
            this.repositoryItemComboBox1.Items.AddRange(new object[] {
            "1",
            "2",
            "3",
            "4",
            "5",
            "6",
            "7",
            "8",
            "9",
            "10",
            "11",
            "12",
            "13",
            "14",
            "15",
            "16",
            "17",
            "18",
            "19",
            "20",
            "21",
            "22",
            "23",
            "24",
            "25",
            "26",
            "27",
            "28",
            "29",
            "30",
            "31",
            "32",
            "33",
            "34",
            "35",
            "36",
            "37",
            "38",
            "39",
            "40",
            "41",
            "42",
            "43",
            "44",
            "45",
            "46",
            "47",
            "48",
            "49",
            "50",
            "51",
            "52",
            "53",
            "54",
            "55",
            "56",
            "57",
            "58",
            "59",
            "60",
            "61",
            "62",
            "63",
            "64",
            "65",
            "66",
            "67",
            "68",
            "69",
            "70",
            "71",
            "72",
            "73",
            "74",
            "75",
            "76",
            "77",
            "78",
            "79",
            "80",
            "81",
            "82",
            "83",
            "84",
            "85",
            "86",
            "87",
            "88",
            "89",
            "90",
            "91",
            "92",
            "93",
            "94",
            "95",
            "96",
            "97",
            "98",
            "99",
            "100",
            "101",
            "102",
            "103",
            "104",
            "105",
            "106",
            "107",
            "108",
            "109",
            "110",
            "111",
            "112",
            "113",
            "114",
            "115",
            "116",
            "117",
            "118",
            "119",
            "120",
            "121",
            "122",
            "123",
            "124",
            "125",
            "126",
            "127",
            "128",
            "129",
            "130",
            "131",
            "132",
            "133",
            "134",
            "135",
            "136",
            "137",
            "138",
            "139",
            "140",
            "141",
            "142",
            "143",
            "144",
            "145",
            "146",
            "147",
            "148",
            "149",
            "150",
            "151",
            "152",
            "153",
            "154",
            "155",
            "156",
            "157",
            "158",
            "159",
            "160",
            "161",
            "162",
            "163",
            "164",
            "165",
            "166",
            "167",
            "168",
            "169",
            "170",
            "171",
            "172",
            "173",
            "174",
            "175",
            "176",
            "177",
            "178",
            "179",
            "180",
            "181",
            "182",
            "183",
            "184",
            "185",
            "186",
            "187",
            "188",
            "189",
            "190",
            "191",
            "192",
            "193",
            "194",
            "195",
            "196",
            "197",
            "198",
            "199",
            "200"});
            this.repositoryItemComboBox1.Name = "repositoryItemComboBox1";
            // 
            // ICCID
            // 
            this.ICCID.AppearanceCell.Options.UseTextOptions = true;
            this.ICCID.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ICCID.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.ICCID.AppearanceHeader.Options.UseTextOptions = true;
            this.ICCID.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ICCID.Caption = "ICCID";
            this.ICCID.FieldName = "ICCID";
            this.ICCID.MinWidth = 23;
            this.ICCID.Name = "ICCID";
            this.ICCID.OptionsColumn.AllowEdit = false;
            this.ICCID.OptionsColumn.FixedWidth = true;
            this.ICCID.Visible = true;
            this.ICCID.VisibleIndex = 2;
            this.ICCID.Width = 160;
            // 
            // Phone
            // 
            this.Phone.AppearanceCell.Options.UseTextOptions = true;
            this.Phone.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.Phone.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Phone.AppearanceHeader.Options.UseTextOptions = true;
            this.Phone.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.Phone.Caption = "Số điện thoại";
            this.Phone.FieldName = "Phone";
            this.Phone.MinWidth = 23;
            this.Phone.Name = "Phone";
            this.Phone.OptionsColumn.AllowEdit = false;
            this.Phone.OptionsColumn.FixedWidth = true;
            this.Phone.Visible = true;
            this.Phone.VisibleIndex = 3;
            this.Phone.Width = 120;
            // 
            // TrangThai
            // 
            this.TrangThai.AppearanceCell.Options.UseTextOptions = true;
            this.TrangThai.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.TrangThai.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.TrangThai.AppearanceHeader.Options.UseTextOptions = true;
            this.TrangThai.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.TrangThai.Caption = "Trạng thái";
            this.TrangThai.FieldName = "TrangThai";
            this.TrangThai.MinWidth = 23;
            this.TrangThai.Name = "TrangThai";
            this.TrangThai.OptionsColumn.AllowEdit = false;
            this.TrangThai.OptionsColumn.FixedWidth = true;
            this.TrangThai.Visible = true;
            this.TrangThai.VisibleIndex = 4;
            this.TrangThai.Width = 90;
            // 
            // TeleStatus
            // 
            this.TeleStatus.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Bold);
            this.TeleStatus.AppearanceCell.Options.UseFont = true;
            this.TeleStatus.AppearanceCell.Options.UseTextOptions = true;
            this.TeleStatus.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.TeleStatus.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.TeleStatus.AppearanceHeader.Options.UseTextOptions = true;
            this.TeleStatus.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.TeleStatus.Caption = "Telegram";
            this.TeleStatus.FieldName = "TeleStatus";
            this.TeleStatus.MinWidth = 23;
            this.TeleStatus.Name = "TeleStatus";
            this.TeleStatus.OptionsColumn.AllowEdit = false;
            this.TeleStatus.OptionsColumn.FixedWidth = true;
            this.TeleStatus.Visible = true;
            this.TeleStatus.VisibleIndex = 5;
            this.TeleStatus.Width = 90;
            // 
            // Message
            // 
            this.Message.AppearanceCell.Options.UseTextOptions = true;
            this.Message.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.Message.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message.Caption = "Tin nhắn";
            this.Message.FieldName = "Message";
            this.Message.MinWidth = 23;
            this.Message.Name = "Message";
            this.Message.OptionsColumn.AllowEdit = false;
            this.Message.Visible = true;
            this.Message.VisibleIndex = 6;
            this.Message.Width = 812;
            // 
            // gridView1
            // 
            this.gridView1.Name = "gridView1";
            // 
            // btnUpdateNo
            // 
            this.btnUpdateNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnUpdateNo.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnUpdateNo.Appearance.Options.UseFont = true;
            this.btnUpdateNo.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdateNo.ImageOptions.Image")));
            this.btnUpdateNo.Location = new System.Drawing.Point(632, 5);
            this.btnUpdateNo.Name = "btnUpdateNo";
            this.btnUpdateNo.Size = new System.Drawing.Size(159, 40);
            this.btnUpdateNo.TabIndex = 5;
            this.btnUpdateNo.Text = "Cập nhật STT";
            this.btnUpdateNo.Click += new System.EventHandler(this.btnUpdateNo_Click);
            // 
            // btnResetNo
            // 
            this.btnResetNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetNo.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnResetNo.Appearance.Options.UseFont = true;
            this.btnResetNo.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetNo.ImageOptions.Image")));
            this.btnResetNo.Location = new System.Drawing.Point(797, 5);
            this.btnResetNo.Name = "btnResetNo";
            this.btnResetNo.Size = new System.Drawing.Size(141, 40);
            this.btnResetNo.TabIndex = 6;
            this.btnResetNo.Text = "Cài lại STT";
            this.btnResetNo.Click += new System.EventHandler(this.btnResetNo_Click);
            // 
            // btnResetCom
            // 
            this.btnResetCom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnResetCom.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnResetCom.Appearance.Options.UseFont = true;
            this.btnResetCom.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetCom.ImageOptions.Image")));
            this.btnResetCom.Location = new System.Drawing.Point(944, 5);
            this.btnResetCom.Name = "btnResetCom";
            this.btnResetCom.Size = new System.Drawing.Size(197, 40);
            this.btnResetCom.TabIndex = 7;
            this.btnResetCom.Text = "Cài lại tất cả COM";
            this.btnResetCom.Click += new System.EventHandler(this.btnResetCom_Click);
            // 
            // btnRestoreFactoryCom
            // 
            this.btnRestoreFactoryCom.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnRestoreFactoryCom.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnRestoreFactoryCom.Appearance.Options.UseFont = true;
            this.btnRestoreFactoryCom.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnRestoreFactoryCom.ImageOptions.Image")));
            this.btnRestoreFactoryCom.Location = new System.Drawing.Point(1147, 5);
            this.btnRestoreFactoryCom.Name = "btnRestoreFactoryCom";
            this.btnRestoreFactoryCom.Size = new System.Drawing.Size(225, 40);
            this.btnRestoreFactoryCom.TabIndex = 8;
            this.btnRestoreFactoryCom.Text = "Khôi phục cài đặt gốc";
            this.btnRestoreFactoryCom.Click += new System.EventHandler(this.btnRestoreFactoryCom_Click);
            // 
            // btnImportSimData
            // 
            this.btnImportSimData.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnImportSimData.Appearance.Options.UseFont = true;
            this.btnImportSimData.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnImportSimData.ImageOptions.Image")));
            this.btnImportSimData.Location = new System.Drawing.Point(12, 5);
            this.btnImportSimData.Name = "btnImportSimData";
            this.btnImportSimData.Size = new System.Drawing.Size(185, 40);
            this.btnImportSimData.TabIndex = 9;
            this.btnImportSimData.Text = "Nhập SIM Data";
            this.btnImportSimData.Click += new System.EventHandler(this.btnImportSimData_Click);
            // 
            // panelToolbar
            // 
            this.panelToolbar.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelToolbar.Controls.Add(this.btnClearSimData);
            this.panelToolbar.Controls.Add(this.lblPhoneCount);
            this.panelToolbar.Controls.Add(this.btnUpdateNo);
            this.panelToolbar.Controls.Add(this.btnRestoreFactoryCom);
            this.panelToolbar.Controls.Add(this.btnResetNo);
            this.panelToolbar.Controls.Add(this.btnResetCom);
            this.panelToolbar.Controls.Add(this.btnImportSimData);
            this.panelToolbar.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelToolbar.Location = new System.Drawing.Point(0, 0);
            this.panelToolbar.Name = "panelToolbar";
            this.panelToolbar.Size = new System.Drawing.Size(1384, 50);
            this.panelToolbar.TabIndex = 9;
            // 
            // btnClearSimData
            // 
            this.btnClearSimData.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnClearSimData.Appearance.Options.UseFont = true;
            this.btnClearSimData.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnClearSimData.ImageOptions.Image")));
            this.btnClearSimData.Location = new System.Drawing.Point(203, 5);
            this.btnClearSimData.Name = "btnClearSimData";
            this.btnClearSimData.Size = new System.Drawing.Size(185, 40);
            this.btnClearSimData.TabIndex = 11;
            this.btnClearSimData.Text = "Xóa SIM Data";
            this.btnClearSimData.Click += new System.EventHandler(this.btnClearSimData_Click);
            // 
            // lblPhoneCount
            // 
            this.lblPhoneCount.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.lblPhoneCount.Appearance.ForeColor = System.Drawing.Color.Blue;
            this.lblPhoneCount.Appearance.Options.UseFont = true;
            this.lblPhoneCount.Appearance.Options.UseForeColor = true;
            this.lblPhoneCount.Location = new System.Drawing.Point(405, 16);
            this.lblPhoneCount.Name = "lblPhoneCount";
            this.lblPhoneCount.Size = new System.Drawing.Size(110, 19);
            this.lblPhoneCount.TabIndex = 10;
            this.lblPhoneCount.Text = "Số SIM: 0 sim";
            // 
            // GSMForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 14F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1384, 756);
            this.Controls.Add(this.gridControlModems);
            this.Controls.Add(this.panelToolbar);
            this.IconOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("GSMForm.IconOptions.LargeImage")));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "GSMForm";
            this.Text = "Luck Check Sim";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            ((System.ComponentModel.ISupportInitialize)(this.gridControlModems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridViewModems)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gridView1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelToolbar)).EndInit();
            this.panelToolbar.ResumeLayout(false);
            this.panelToolbar.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Timer timerCheckSimError;
        private DevExpress.XtraGrid.GridControl gridControlModems;
        private DevExpress.XtraGrid.Views.Grid.GridView gridViewModems;
        private DevExpress.XtraGrid.Columns.GridColumn Com;
        private DevExpress.XtraGrid.Columns.GridColumn Stt;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox1;
        private DevExpress.XtraGrid.Columns.GridColumn ICCID;
        private DevExpress.XtraGrid.Columns.GridColumn Phone;
        private DevExpress.XtraGrid.Columns.GridColumn TrangThai;
        private DevExpress.XtraGrid.Columns.GridColumn TeleStatus;
        private DevExpress.XtraGrid.Columns.GridColumn Message;
        private DevExpress.XtraGrid.Views.Grid.GridView gridView1;
        private DevExpress.XtraEditors.SimpleButton btnUpdateNo;
        private DevExpress.XtraEditors.SimpleButton btnResetNo;
        private DevExpress.XtraEditors.SimpleButton btnResetCom;
        private DevExpress.XtraEditors.SimpleButton btnRestoreFactoryCom;
        private DevExpress.XtraEditors.SimpleButton btnImportSimData;
        private DevExpress.XtraEditors.PanelControl panelToolbar;
        private DevExpress.XtraEditors.LabelControl lblPhoneCount;
        private DevExpress.XtraEditors.SimpleButton btnClearSimData;
    }
}