namespace LuckOTP
{
    partial class GSM
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
            DevExpress.Utils.SuperToolTip superToolTip9 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem9 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip10 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem10 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip11 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem11 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip12 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem12 = new DevExpress.Utils.ToolTipItem();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GSM));
            this.barManager = new DevExpress.XtraBars.BarManager();
            this.mainToolBar = new DevExpress.XtraBars.Bar();
            this.btnComSettingDropDown = new DevExpress.XtraBars.BarSubItem();
            this.btnUpdateComPort = new DevExpress.XtraBars.BarButtonItem();
            this.btnResetComPort = new DevExpress.XtraBars.BarButtonItem();
            this.btnChangeIMEI = new DevExpress.XtraBars.BarButtonItem();
            this.btnResetCom = new DevExpress.XtraBars.BarButtonItem();
            this.btnRestoreSettings = new DevExpress.XtraBars.BarButtonItem();
            this.barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            this.repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.barSubItem1 = new DevExpress.XtraBars.BarSubItem();
            this.gcCOM = new DevExpress.XtraGrid.GridControl();
            this.gvCOM = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.COM = new DevExpress.XtraGrid.Columns.GridColumn();
            this.STT = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemComboBox1 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.ICCID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PhoneNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Message101 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemTextEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TimerCheckSim = new System.Windows.Forms.Timer();
            this.guide1 = new DevExpress.Utils.VisualEffects.Guide();
            this.Message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.TKChinh = new DevExpress.XtraGrid.Columns.GridColumn();
            this.TimerSyncDB = new System.Windows.Forms.Timer();
            ((System.ComponentModel.ISupportInitialize)(this.barManager)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcCOM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCOM)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).BeginInit();
            this.SuspendLayout();
            // 
            // barManager
            // 
            this.barManager.Bars.AddRange(new DevExpress.XtraBars.Bar[] {
            this.mainToolBar});
            this.barManager.DockControls.Add(this.barDockControlTop);
            this.barManager.DockControls.Add(this.barDockControlBottom);
            this.barManager.DockControls.Add(this.barDockControlLeft);
            this.barManager.DockControls.Add(this.barDockControlRight);
            this.barManager.DockWindowTabFont = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barManager.Form = this;
            this.barManager.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.btnComSettingDropDown,
            this.btnUpdateComPort,
            this.btnResetComPort,
            this.btnResetCom,
            this.btnRestoreSettings,
            this.btnChangeIMEI});
            this.barManager.MaxItemId = 33;
            this.barManager.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemTextEdit1});
            // 
            // mainToolBar
            // 
            this.mainToolBar.BarName = "Main Toolbar";
            this.mainToolBar.DockCol = 0;
            this.mainToolBar.DockRow = 0;
            this.mainToolBar.DockStyle = DevExpress.XtraBars.BarDockStyle.Top;
            this.mainToolBar.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnComSettingDropDown, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph)});
            this.mainToolBar.OptionsBar.AllowQuickCustomization = false;
            this.mainToolBar.OptionsBar.DisableClose = true;
            this.mainToolBar.OptionsBar.DisableCustomization = true;
            this.mainToolBar.OptionsBar.DrawBorder = false;
            this.mainToolBar.OptionsBar.DrawDragBorder = false;
            this.mainToolBar.Text = "Main Toolbar";
            // 
            // btnComSettingDropDown
            // 
            this.btnComSettingDropDown.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
            this.btnComSettingDropDown.Caption = "Cài đặt chung";
            this.btnComSettingDropDown.Id = 2;
            this.btnComSettingDropDown.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnComSettingDropDown.ImageOptions.Image")));
            this.btnComSettingDropDown.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnComSettingDropDown.ImageOptions.LargeImage")));
            this.btnComSettingDropDown.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComSettingDropDown.ItemAppearance.Normal.Options.UseFont = true;
            this.btnComSettingDropDown.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnUpdateComPort, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnResetComPort, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnChangeIMEI, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnResetCom, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnRestoreSettings, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph)});
            this.btnComSettingDropDown.MenuAppearance.AppearanceMenu.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComSettingDropDown.MenuAppearance.AppearanceMenu.Normal.Options.UseFont = true;
            this.btnComSettingDropDown.MenuAppearance.HeaderItemAppearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComSettingDropDown.MenuAppearance.HeaderItemAppearance.Options.UseFont = true;
            this.btnComSettingDropDown.MenuAppearance.MenuBar.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComSettingDropDown.MenuAppearance.MenuBar.Options.UseFont = true;
            this.btnComSettingDropDown.MenuAppearance.MenuCaption.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnComSettingDropDown.MenuAppearance.MenuCaption.Options.UseFont = true;
            this.btnComSettingDropDown.Name = "btnComSettingDropDown";
            // 
            // btnUpdateComPort
            // 
            this.btnUpdateComPort.Caption = "Cập nhật STT (Số thứ tự) các cổng COM";
            this.btnUpdateComPort.Id = 3;
            this.btnUpdateComPort.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdateComPort.ImageOptions.Image")));
            this.btnUpdateComPort.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnUpdateComPort.ImageOptions.LargeImage")));
            this.btnUpdateComPort.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnUpdateComPort.ItemAppearance.Normal.Options.UseFont = true;
            this.btnUpdateComPort.Name = "btnUpdateComPort";
            toolTipItem9.Text = "Cập nhật lại số thứ tự cổng COM";
            superToolTip9.Items.Add(toolTipItem9);
            this.btnUpdateComPort.SuperTip = superToolTip9;
            this.btnUpdateComPort.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnUpdateComPort_ItemClick);
            // 
            // btnResetComPort
            // 
            this.btnResetComPort.Caption = "Đặt lại STT (Số thứ tự) các cổng COM";
            this.btnResetComPort.Id = 4;
            this.btnResetComPort.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetComPort.ImageOptions.Image")));
            this.btnResetComPort.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnResetComPort.ImageOptions.LargeImage")));
            this.btnResetComPort.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResetComPort.ItemAppearance.Normal.Options.UseFont = true;
            this.btnResetComPort.Name = "btnResetComPort";
            toolTipItem10.Text = "Đặt lại số thứ tự cổng COM";
            superToolTip10.Items.Add(toolTipItem10);
            this.btnResetComPort.SuperTip = superToolTip10;
            this.btnResetComPort.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnResetComPort_ItemClick);
            // 
            // btnChangeIMEI
            // 
            this.btnChangeIMEI.Caption = "Thay đổi IMEI các cổng COM";
            this.btnChangeIMEI.Id = 11;
            this.btnChangeIMEI.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnChangeIMEI.ImageOptions.Image")));
            this.btnChangeIMEI.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnChangeIMEI.ImageOptions.LargeImage")));
            this.btnChangeIMEI.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnChangeIMEI.ItemAppearance.Normal.Options.UseFont = true;
            this.btnChangeIMEI.Name = "btnChangeIMEI";
            this.btnChangeIMEI.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnChangeIMEI_ItemClick);
            // 
            // btnResetCom
            // 
            this.btnResetCom.Caption = "Reset các cổng COM";
            this.btnResetCom.Id = 5;
            this.btnResetCom.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetCom.ImageOptions.Image")));
            this.btnResetCom.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnResetCom.ImageOptions.LargeImage")));
            this.btnResetCom.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnResetCom.ItemAppearance.Normal.Options.UseFont = true;
            this.btnResetCom.Name = "btnResetCom";
            toolTipItem11.Text = "Reset lại cổng COM";
            superToolTip11.Items.Add(toolTipItem11);
            this.btnResetCom.SuperTip = superToolTip11;
            this.btnResetCom.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnResetCom_ItemClick);
            // 
            // btnRestoreSettings
            // 
            this.btnRestoreSettings.Caption = "Khôi phục cài đặt gốc các cổng COM";
            this.btnRestoreSettings.Id = 6;
            this.btnRestoreSettings.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnRestoreSettings.ImageOptions.Image")));
            this.btnRestoreSettings.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnRestoreSettings.ImageOptions.LargeImage")));
            this.btnRestoreSettings.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnRestoreSettings.ItemAppearance.Normal.Options.UseFont = true;
            this.btnRestoreSettings.Name = "btnRestoreSettings";
            toolTipItem12.Text = "Khôi phục lại cài đặt mặc định";
            superToolTip12.Items.Add(toolTipItem12);
            this.btnRestoreSettings.SuperTip = superToolTip12;
            this.btnRestoreSettings.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnRestoreSettings_ItemClick);
            // 
            // barDockControlTop
            // 
            this.barDockControlTop.CausesValidation = false;
            this.barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.barDockControlTop.Location = new System.Drawing.Point(0, 0);
            this.barDockControlTop.Manager = this.barManager;
            this.barDockControlTop.Size = new System.Drawing.Size(1366, 27);
            // 
            // barDockControlBottom
            // 
            this.barDockControlBottom.CausesValidation = false;
            this.barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.barDockControlBottom.Location = new System.Drawing.Point(0, 818);
            this.barDockControlBottom.Manager = this.barManager;
            this.barDockControlBottom.Size = new System.Drawing.Size(1366, 0);
            // 
            // barDockControlLeft
            // 
            this.barDockControlLeft.CausesValidation = false;
            this.barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.barDockControlLeft.Location = new System.Drawing.Point(0, 27);
            this.barDockControlLeft.Manager = this.barManager;
            this.barDockControlLeft.Size = new System.Drawing.Size(0, 791);
            // 
            // barDockControlRight
            // 
            this.barDockControlRight.CausesValidation = false;
            this.barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.barDockControlRight.Location = new System.Drawing.Point(1366, 27);
            this.barDockControlRight.Manager = this.barManager;
            this.barDockControlRight.Size = new System.Drawing.Size(0, 791);
            // 
            // repositoryItemTextEdit1
            // 
            this.repositoryItemTextEdit1.AutoHeight = false;
            this.repositoryItemTextEdit1.Mask.EditMask = "n0";
            this.repositoryItemTextEdit1.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.repositoryItemTextEdit1.Mask.UseMaskAsDisplayFormat = true;
            this.repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // barSubItem1
            // 
            this.barSubItem1.Caption = "Tài khoản";
            this.barSubItem1.Id = 16;
            this.barSubItem1.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barSubItem1.ImageOptions.Image")));
            this.barSubItem1.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barSubItem1.ImageOptions.LargeImage")));
            this.barSubItem1.ItemAppearance.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barSubItem1.ItemAppearance.Normal.Options.UseFont = true;
            this.barSubItem1.MenuAppearance.AppearanceMenu.Normal.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barSubItem1.MenuAppearance.AppearanceMenu.Normal.Options.UseFont = true;
            this.barSubItem1.MenuAppearance.HeaderItemAppearance.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barSubItem1.MenuAppearance.HeaderItemAppearance.Options.UseFont = true;
            this.barSubItem1.MenuAppearance.MenuBar.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barSubItem1.MenuAppearance.MenuBar.Options.UseFont = true;
            this.barSubItem1.MenuAppearance.MenuCaption.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.barSubItem1.MenuAppearance.MenuCaption.Options.UseFont = true;
            this.barSubItem1.Name = "barSubItem1";
            this.barSubItem1.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            // 
            // gcCOM
            // 
            this.gcCOM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcCOM.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcCOM.Location = new System.Drawing.Point(0, 27);
            this.gcCOM.MainView = this.gvCOM;
            this.gcCOM.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcCOM.Name = "gcCOM";
            this.gcCOM.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemComboBox1,
            this.repositoryItemTextEdit2});
            this.gcCOM.Size = new System.Drawing.Size(1366, 791);
            this.gcCOM.TabIndex = 0;
            this.gcCOM.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvCOM});
            // 
            // gvCOM
            // 
            this.gvCOM.AppearancePrint.FooterPanel.Font = new System.Drawing.Font("Tahoma", 14F);
            this.gvCOM.AppearancePrint.FooterPanel.Options.UseFont = true;
            this.gvCOM.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.COM,
            this.STT,
            this.ICCID,
            this.PhoneNumber,
            this.Message101});
            this.gvCOM.GridControl = this.gcCOM;
            this.gvCOM.Name = "gvCOM";
            this.gvCOM.OptionsClipboard.CopyColumnHeaders = DevExpress.Utils.DefaultBoolean.False;
            this.gvCOM.OptionsSelection.MultiSelect = true;
            this.gvCOM.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CellSelect;
            this.gvCOM.OptionsView.ShowGroupPanel = false;
            // 
            // COM
            // 
            this.COM.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.COM.AppearanceCell.Options.UseFont = true;
            this.COM.AppearanceCell.Options.UseTextOptions = true;
            this.COM.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.COM.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.COM.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.COM.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.COM.AppearanceHeader.Options.UseFont = true;
            this.COM.AppearanceHeader.Options.UseTextOptions = true;
            this.COM.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.COM.Caption = "COM";
            this.COM.FieldName = "COM";
            this.COM.MaxWidth = 60;
            this.COM.MinWidth = 60;
            this.COM.Name = "COM";
            this.COM.OptionsColumn.AllowEdit = false;
            this.COM.OptionsColumn.AllowSize = false;
            this.COM.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.COM.OptionsColumn.FixedWidth = true;
            this.COM.OptionsFilter.AllowAutoFilter = false;
            this.COM.OptionsFilter.AllowFilter = false;
            this.COM.Visible = true;
            this.COM.VisibleIndex = 0;
            this.COM.Width = 60;
            // 
            // STT
            // 
            this.STT.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.STT.AppearanceCell.Options.UseFont = true;
            this.STT.AppearanceCell.Options.UseTextOptions = true;
            this.STT.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.STT.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.STT.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.STT.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.STT.AppearanceHeader.Options.UseFont = true;
            this.STT.AppearanceHeader.Options.UseTextOptions = true;
            this.STT.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.STT.Caption = "No";
            this.STT.ColumnEdit = this.repositoryItemComboBox1;
            this.STT.FieldName = "STT";
            this.STT.MaxWidth = 50;
            this.STT.MinWidth = 50;
            this.STT.Name = "STT";
            this.STT.OptionsColumn.AllowGroup = DevExpress.Utils.DefaultBoolean.False;
            this.STT.OptionsColumn.AllowMerge = DevExpress.Utils.DefaultBoolean.False;
            this.STT.OptionsColumn.AllowMove = false;
            this.STT.OptionsColumn.AllowShowHide = false;
            this.STT.OptionsColumn.AllowSize = false;
            this.STT.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.STT.OptionsColumn.FixedWidth = true;
            this.STT.OptionsFilter.AllowAutoFilter = false;
            this.STT.OptionsFilter.AllowFilter = false;
            this.STT.Visible = true;
            this.STT.VisibleIndex = 1;
            this.STT.Width = 50;
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
            "200",
            "201",
            "202",
            "203",
            "204",
            "205",
            "206",
            "207",
            "208",
            "209",
            "210",
            "211",
            "212",
            "213",
            "214",
            "215",
            "216",
            "217",
            "218",
            "219",
            "220",
            "221",
            "222",
            "223",
            "224",
            "225",
            "226",
            "227",
            "228",
            "229",
            "230",
            "231",
            "232",
            "233",
            "234",
            "235",
            "236",
            "237",
            "238",
            "239",
            "240",
            "241",
            "242",
            "243",
            "244",
            "245",
            "246",
            "247",
            "248",
            "249",
            "250",
            "251",
            "252",
            "253",
            "254",
            "255",
            "256",
            "257",
            "258",
            "259",
            "260",
            "261",
            "262",
            "263",
            "264",
            "265",
            "266",
            "267",
            "268",
            "269",
            "270",
            "271",
            "272",
            "273",
            "274",
            "275",
            "276",
            "277",
            "278",
            "279",
            "280",
            "281",
            "282",
            "283",
            "284",
            "285",
            "286",
            "287",
            "288",
            "289",
            "290",
            "291",
            "292",
            "293",
            "294",
            "295",
            "296",
            "297",
            "298",
            "299",
            "300",
            "301",
            "302",
            "303",
            "304",
            "305",
            "306",
            "307",
            "308",
            "309",
            "310",
            "311",
            "312",
            "313",
            "314",
            "315",
            "316",
            "317",
            "318",
            "319",
            "320",
            "321",
            "322",
            "323",
            "324",
            "325",
            "326",
            "327",
            "328",
            "329",
            "330",
            "331",
            "332",
            "333",
            "334",
            "335",
            "336",
            "337",
            "338",
            "339",
            "340",
            "341",
            "342",
            "343",
            "344",
            "345",
            "346",
            "347",
            "348",
            "349",
            "350",
            "351",
            "352",
            "353",
            "354",
            "355",
            "356",
            "357",
            "358",
            "359",
            "360",
            "361",
            "362",
            "363",
            "364",
            "365",
            "366",
            "367",
            "368",
            "369",
            "370",
            "371",
            "372",
            "373",
            "374",
            "375",
            "376",
            "377",
            "378",
            "379",
            "380",
            "381",
            "382",
            "383",
            "384",
            "385",
            "386",
            "387",
            "388",
            "389",
            "390",
            "391",
            "392",
            "393",
            "394",
            "395",
            "396",
            "397",
            "398",
            "399",
            "400",
            "401",
            "402",
            "403",
            "404",
            "405",
            "406",
            "407",
            "408",
            "409",
            "410",
            "411",
            "412",
            "413",
            "414",
            "415",
            "416",
            "417",
            "418",
            "419",
            "420",
            "421",
            "422",
            "423",
            "424",
            "425",
            "426",
            "427",
            "428",
            "429",
            "430",
            "431",
            "432",
            "433",
            "434",
            "435",
            "436",
            "437",
            "438",
            "439",
            "440",
            "441",
            "442",
            "443",
            "444",
            "445",
            "446",
            "447",
            "448",
            "449",
            "450",
            "451",
            "452",
            "453",
            "454",
            "455",
            "456",
            "457",
            "458",
            "459",
            "460",
            "461",
            "462",
            "463",
            "464",
            "465",
            "466",
            "467",
            "468",
            "469",
            "470",
            "471",
            "472",
            "473",
            "474",
            "475",
            "476",
            "477",
            "478",
            "479",
            "480",
            "481",
            "482",
            "483",
            "484",
            "485",
            "486",
            "487",
            "488",
            "489",
            "490",
            "491",
            "492",
            "493",
            "494",
            "495",
            "496",
            "497",
            "498",
            "499",
            "500"});
            this.repositoryItemComboBox1.Name = "repositoryItemComboBox1";
            // 
            // ICCID
            // 
            this.ICCID.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.ICCID.AppearanceCell.Options.UseFont = true;
            this.ICCID.AppearanceCell.Options.UseTextOptions = true;
            this.ICCID.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ICCID.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.ICCID.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.ICCID.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.ICCID.AppearanceHeader.Options.UseFont = true;
            this.ICCID.AppearanceHeader.Options.UseTextOptions = true;
            this.ICCID.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ICCID.Caption = "ICCID";
            this.ICCID.FieldName = "ICCID";
            this.ICCID.MinWidth = 23;
            this.ICCID.Name = "ICCID";
            this.ICCID.OptionsColumn.AllowEdit = false;
            this.ICCID.OptionsColumn.AllowSize = false;
            this.ICCID.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.ICCID.OptionsColumn.FixedWidth = true;
            this.ICCID.OptionsFilter.AllowAutoFilter = false;
            this.ICCID.OptionsFilter.AllowFilter = false;
            this.ICCID.Visible = true;
            this.ICCID.VisibleIndex = 2;
            this.ICCID.Width = 150;
            // 
            // PhoneNumber
            // 
            this.PhoneNumber.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.PhoneNumber.AppearanceCell.Options.UseFont = true;
            this.PhoneNumber.AppearanceCell.Options.UseTextOptions = true;
            this.PhoneNumber.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.PhoneNumber.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.PhoneNumber.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.PhoneNumber.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.PhoneNumber.AppearanceHeader.Options.UseFont = true;
            this.PhoneNumber.AppearanceHeader.Options.UseTextOptions = true;
            this.PhoneNumber.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.PhoneNumber.Caption = "Số điện thoại";
            this.PhoneNumber.FieldName = "PhoneNumber";
            this.PhoneNumber.MinWidth = 23;
            this.PhoneNumber.Name = "PhoneNumber";
            this.PhoneNumber.OptionsColumn.AllowEdit = false;
            this.PhoneNumber.OptionsColumn.AllowSize = false;
            this.PhoneNumber.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.PhoneNumber.OptionsColumn.FixedWidth = true;
            this.PhoneNumber.OptionsFilter.AllowAutoFilter = false;
            this.PhoneNumber.OptionsFilter.AutoFilterCondition = DevExpress.XtraGrid.Columns.AutoFilterCondition.Like;
            this.PhoneNumber.OptionsFilter.FilterPopupMode = DevExpress.XtraGrid.Columns.FilterPopupMode.CheckedList;
            this.PhoneNumber.Visible = true;
            this.PhoneNumber.VisibleIndex = 3;
            this.PhoneNumber.Width = 110;
            // 
            // Message101
            // 
            this.Message101.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.Message101.AppearanceCell.Options.UseFont = true;
            this.Message101.AppearanceCell.Options.UseTextOptions = true;
            this.Message101.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message101.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.Message101.AppearanceHeader.Options.UseFont = true;
            this.Message101.Caption = "Tin nhắn";
            this.Message101.FieldName = "Message101";
            this.Message101.Name = "Message101";
            this.Message101.OptionsColumn.AllowEdit = false;
            this.Message101.Visible = true;
            this.Message101.VisibleIndex = 4;
            this.Message101.Width = 707;
            // 
            // repositoryItemTextEdit2
            // 
            this.repositoryItemTextEdit2.AutoHeight = false;
            this.repositoryItemTextEdit2.Name = "repositoryItemTextEdit2";
            this.repositoryItemTextEdit2.ReadOnly = true;
            // 
            // TimerCheckSim
            // 
            this.TimerCheckSim.Interval = 10000;
            this.TimerCheckSim.Tick += new System.EventHandler(this.TimerCheckSim_Tick);
            // 
            // Message
            // 
            this.Message.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.Message.AppearanceCell.ForeColor = System.Drawing.Color.Green;
            this.Message.AppearanceCell.Options.UseFont = true;
            this.Message.AppearanceCell.Options.UseForeColor = true;
            this.Message.AppearanceCell.Options.UseTextOptions = true;
            this.Message.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.Message.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.Message.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.Message.AppearanceHeader.Options.UseFont = true;
            this.Message.Caption = "Trạng Thái";
            this.Message.FieldName = "Message";
            this.Message.MinWidth = 23;
            this.Message.Name = "Message";
            this.Message.OptionsColumn.AllowEdit = false;
            this.Message.OptionsColumn.AllowSize = false;
            this.Message.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.Message.OptionsFilter.AllowAutoFilter = false;
            this.Message.OptionsFilter.AllowFilter = false;
            this.Message.Width = 96;
            // 
            // TKChinh
            // 
            this.TKChinh.AppearanceCell.Font = new System.Drawing.Font("Verdana", 8F);
            this.TKChinh.AppearanceCell.ForeColor = System.Drawing.Color.DarkRed;
            this.TKChinh.AppearanceCell.Options.UseFont = true;
            this.TKChinh.AppearanceCell.Options.UseForeColor = true;
            this.TKChinh.AppearanceCell.Options.UseTextOptions = true;
            this.TKChinh.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.TKChinh.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.TKChinh.AppearanceHeader.Font = new System.Drawing.Font("Verdana", 8F, System.Drawing.FontStyle.Bold);
            this.TKChinh.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.TKChinh.AppearanceHeader.Options.UseFont = true;
            this.TKChinh.AppearanceHeader.Options.UseTextOptions = true;
            this.TKChinh.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.TKChinh.Caption = "TK Chính";
            this.TKChinh.FieldName = "TKChinh";
            this.TKChinh.Name = "TKChinh";
            this.TKChinh.OptionsColumn.AllowEdit = false;
            this.TKChinh.OptionsColumn.AllowSize = false;
            this.TKChinh.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.TKChinh.OptionsColumn.FixedWidth = true;
            this.TKChinh.OptionsFilter.AllowAutoFilter = false;
            this.TKChinh.OptionsFilter.AllowFilter = false;
            this.TKChinh.Width = 80;
            // 
            // TimerSyncDB
            // 
            this.TimerSyncDB.Interval = 120000;
            this.TimerSyncDB.Tick += new System.EventHandler(this.TimerSyncDB_Tick);
            // 
            // GSM
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1366, 818);
            this.Controls.Add(this.gcCOM);
            this.Controls.Add(this.barDockControlLeft);
            this.Controls.Add(this.barDockControlRight);
            this.Controls.Add(this.barDockControlBottom);
            this.Controls.Add(this.barDockControlTop);
            this.IconOptions.Image = ((System.Drawing.Image)(resources.GetObject("GSM.IconOptions.Image")));
            this.LookAndFeel.SkinName = "Office 2019 Colorful";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Name = "GSM";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Luck OTP";
            this.Load += new System.EventHandler(this.BurnTKForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.barManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcCOM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvCOM)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private DevExpress.XtraBars.BarManager barManager;
        private DevExpress.XtraBars.Bar mainToolBar;
        private DevExpress.XtraBars.BarDockControl barDockControlTop;
        private DevExpress.XtraBars.BarDockControl barDockControlBottom;
        private DevExpress.XtraBars.BarDockControl barDockControlLeft;
        private DevExpress.XtraBars.BarDockControl barDockControlRight;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private DevExpress.XtraBars.BarSubItem btnComSettingDropDown;
        private DevExpress.XtraBars.BarButtonItem btnUpdateComPort;
        private DevExpress.XtraBars.BarButtonItem btnResetComPort;
        private DevExpress.XtraBars.BarButtonItem btnResetCom;
        private DevExpress.XtraBars.BarButtonItem btnRestoreSettings;
        private DevExpress.XtraGrid.GridControl gcCOM;
        private DevExpress.XtraGrid.Views.Grid.GridView gvCOM;
        private DevExpress.XtraGrid.Columns.GridColumn COM;
        private DevExpress.XtraGrid.Columns.GridColumn STT;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox1;
        private DevExpress.XtraGrid.Columns.GridColumn ICCID;
        private DevExpress.XtraGrid.Columns.GridColumn PhoneNumber;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit2;
        private System.Windows.Forms.Timer TimerCheckSim;
        private DevExpress.XtraGrid.Columns.GridColumn Message101;
        private DevExpress.XtraBars.BarButtonItem btnChangeIMEI;
        private DevExpress.XtraBars.BarSubItem barSubItem1;
        private DevExpress.Utils.VisualEffects.Guide guide1;
        private DevExpress.XtraGrid.Columns.GridColumn Message;
        private DevExpress.XtraGrid.Columns.GridColumn TKChinh;
        private System.Windows.Forms.Timer TimerSyncDB;
    }
}