namespace LuckBurnTK
{
    partial class BurnTKForm
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
            DevExpress.Utils.SuperToolTip superToolTip5 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem5 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip6 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem6 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip7 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem7 = new DevExpress.Utils.ToolTipItem();
            DevExpress.Utils.SuperToolTip superToolTip8 = new DevExpress.Utils.SuperToolTip();
            DevExpress.Utils.ToolTipItem toolTipItem8 = new DevExpress.Utils.ToolTipItem();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BurnTKForm));
            this.barManager = new DevExpress.XtraBars.BarManager(this.components);
            this.mainToolBar = new DevExpress.XtraBars.Bar();
            this.btnUpdateComPort = new DevExpress.XtraBars.BarButtonItem();
            this.BtnDoanhThu = new DevExpress.XtraBars.BarButtonItem();
            this.btnComSettingDropDown = new DevExpress.XtraBars.BarSubItem();
            this.btnResetComPort = new DevExpress.XtraBars.BarButtonItem();
            this.btnResetCom = new DevExpress.XtraBars.BarButtonItem();
            this.btnRestoreSettings = new DevExpress.XtraBars.BarButtonItem();
            this.barBtnRule = new DevExpress.XtraBars.BarButtonItem();
            this.barDockControlTop = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
            this.barDockControlRight = new DevExpress.XtraBars.BarDockControl();
            this.txtMinAccount = new DevExpress.XtraBars.BarEditItem();
            this.repositoryItemTextEdit1 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.btnStartStop = new DevExpress.XtraBars.BarButtonItem();
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.labelControl2 = new DevExpress.XtraEditors.LabelControl();
            this.btnUpdateMinAccountControl = new DevExpress.XtraEditors.SimpleButton();
            this.txtMinAccountControl = new DevExpress.XtraEditors.TextEdit();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.gcCOM = new DevExpress.XtraGrid.GridControl();
            this.gvCOM = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.IMEI = new DevExpress.XtraGrid.Columns.GridColumn();
            this.COM = new DevExpress.XtraGrid.Columns.GridColumn();
            this.STT = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemComboBox1 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.ICCID = new DevExpress.XtraGrid.Columns.GridColumn();
            this.PhoneNumber = new DevExpress.XtraGrid.Columns.GridColumn();
            this.TKChinh = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Message101 = new DevExpress.XtraGrid.Columns.GridColumn();
            this.Message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.SmsId = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemTextEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            this.TimerCheckSim = new System.Windows.Forms.Timer(this.components);
            this.Telecom = new DevExpress.XtraGrid.Columns.GridColumn();
            ((System.ComponentModel.ISupportInitialize)(this.barManager)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinAccountControl.Properties)).BeginInit();
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
            this.barManager.Form = this;
            this.barManager.Items.AddRange(new DevExpress.XtraBars.BarItem[] {
            this.txtMinAccount,
            this.btnStartStop,
            this.btnComSettingDropDown,
            this.btnUpdateComPort,
            this.btnResetComPort,
            this.btnResetCom,
            this.btnRestoreSettings,
            this.BtnDoanhThu,
            this.barBtnRule});
            this.barManager.MaxItemId = 11;
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
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnUpdateComPort, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(this.BtnDoanhThu),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnComSettingDropDown, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(this.barBtnRule)});
            this.mainToolBar.Text = "Main Toolbar";
            // 
            // btnUpdateComPort
            // 
            this.btnUpdateComPort.Caption = "Cập nhật STT cổng COM";
            this.btnUpdateComPort.Id = 3;
            this.btnUpdateComPort.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdateComPort.ImageOptions.Image")));
            this.btnUpdateComPort.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnUpdateComPort.ImageOptions.LargeImage")));
            this.btnUpdateComPort.Name = "btnUpdateComPort";
            toolTipItem5.Text = "Cập nhật lại số thứ tự cổng COM";
            superToolTip5.Items.Add(toolTipItem5);
            this.btnUpdateComPort.SuperTip = superToolTip5;
            this.btnUpdateComPort.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnUpdateComPort_ItemClick);
            // 
            // BtnDoanhThu
            // 
            this.BtnDoanhThu.Caption = "Thống kê";
            this.BtnDoanhThu.Id = 9;
            this.BtnDoanhThu.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("BtnDoanhThu.ImageOptions.Image")));
            this.BtnDoanhThu.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("BtnDoanhThu.ImageOptions.LargeImage")));
            this.BtnDoanhThu.Name = "BtnDoanhThu";
            this.BtnDoanhThu.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            this.BtnDoanhThu.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnDoanhThu_ItemClick);
            // 
            // btnComSettingDropDown
            // 
            this.btnComSettingDropDown.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
            this.btnComSettingDropDown.Caption = "Cài đặt chung";
            this.btnComSettingDropDown.Id = 2;
            this.btnComSettingDropDown.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnComSettingDropDown.ImageOptions.Image")));
            this.btnComSettingDropDown.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnComSettingDropDown.ImageOptions.LargeImage")));
            this.btnComSettingDropDown.LinksPersistInfo.AddRange(new DevExpress.XtraBars.LinkPersistInfo[] {
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnResetComPort, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnResetCom, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph),
            new DevExpress.XtraBars.LinkPersistInfo(DevExpress.XtraBars.BarLinkUserDefines.PaintStyle, this.btnRestoreSettings, DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph)});
            this.btnComSettingDropDown.Name = "btnComSettingDropDown";
            // 
            // btnResetComPort
            // 
            this.btnResetComPort.Caption = "Đặt lại STT cổng COM";
            this.btnResetComPort.Id = 4;
            this.btnResetComPort.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetComPort.ImageOptions.Image")));
            this.btnResetComPort.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnResetComPort.ImageOptions.LargeImage")));
            this.btnResetComPort.Name = "btnResetComPort";
            toolTipItem6.Text = "Đặt lại số thứ tự cổng COM";
            superToolTip6.Items.Add(toolTipItem6);
            this.btnResetComPort.SuperTip = superToolTip6;
            this.btnResetComPort.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnResetComPort_ItemClick);
            // 
            // btnResetCom
            // 
            this.btnResetCom.Caption = "Reset cổng COM";
            this.btnResetCom.Id = 5;
            this.btnResetCom.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnResetCom.ImageOptions.Image")));
            this.btnResetCom.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnResetCom.ImageOptions.LargeImage")));
            this.btnResetCom.Name = "btnResetCom";
            toolTipItem7.Text = "Reset lại cổng COM";
            superToolTip7.Items.Add(toolTipItem7);
            this.btnResetCom.SuperTip = superToolTip7;
            this.btnResetCom.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnResetCom_ItemClick);
            // 
            // btnRestoreSettings
            // 
            this.btnRestoreSettings.Caption = "Khôi phục cài đặt gốc";
            this.btnRestoreSettings.Id = 6;
            this.btnRestoreSettings.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnRestoreSettings.ImageOptions.Image")));
            this.btnRestoreSettings.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("btnRestoreSettings.ImageOptions.LargeImage")));
            this.btnRestoreSettings.Name = "btnRestoreSettings";
            toolTipItem8.Text = "Khôi phục lại cài đặt mặc định";
            superToolTip8.Items.Add(toolTipItem8);
            this.btnRestoreSettings.SuperTip = superToolTip8;
            this.btnRestoreSettings.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BtnRestoreSettings_ItemClick);
            // 
            // barBtnRule
            // 
            this.barBtnRule.Caption = "Điều khoản dịch vụ";
            this.barBtnRule.Id = 10;
            this.barBtnRule.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("barBtnRule.ImageOptions.Image")));
            this.barBtnRule.ImageOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("barBtnRule.ImageOptions.LargeImage")));
            this.barBtnRule.Name = "barBtnRule";
            this.barBtnRule.PaintStyle = DevExpress.XtraBars.BarItemPaintStyle.CaptionGlyph;
            this.barBtnRule.ItemClick += new DevExpress.XtraBars.ItemClickEventHandler(this.BarBtnRule_ItemClick);
            // 
            // barDockControlTop
            // 
            this.barDockControlTop.CausesValidation = false;
            this.barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.barDockControlTop.Location = new System.Drawing.Point(0, 0);
            this.barDockControlTop.Manager = this.barManager;
            this.barDockControlTop.Size = new System.Drawing.Size(1278, 27);
            // 
            // barDockControlBottom
            // 
            this.barDockControlBottom.CausesValidation = false;
            this.barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.barDockControlBottom.Location = new System.Drawing.Point(0, 688);
            this.barDockControlBottom.Manager = this.barManager;
            this.barDockControlBottom.Size = new System.Drawing.Size(1278, 0);
            // 
            // barDockControlLeft
            // 
            this.barDockControlLeft.CausesValidation = false;
            this.barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
            this.barDockControlLeft.Location = new System.Drawing.Point(0, 27);
            this.barDockControlLeft.Manager = this.barManager;
            this.barDockControlLeft.Size = new System.Drawing.Size(0, 661);
            // 
            // barDockControlRight
            // 
            this.barDockControlRight.CausesValidation = false;
            this.barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
            this.barDockControlRight.Location = new System.Drawing.Point(1278, 27);
            this.barDockControlRight.Manager = this.barManager;
            this.barDockControlRight.Size = new System.Drawing.Size(0, 661);
            // 
            // txtMinAccount
            // 
            this.txtMinAccount.Caption = "Min tài khoản:";
            this.txtMinAccount.Edit = this.repositoryItemTextEdit1;
            this.txtMinAccount.EditWidth = 120;
            this.txtMinAccount.Id = 0;
            this.txtMinAccount.Name = "txtMinAccount";
            // 
            // repositoryItemTextEdit1
            // 
            this.repositoryItemTextEdit1.AutoHeight = false;
            this.repositoryItemTextEdit1.Mask.EditMask = "n0";
            this.repositoryItemTextEdit1.Mask.MaskType = DevExpress.XtraEditors.Mask.MaskType.Numeric;
            this.repositoryItemTextEdit1.Mask.UseMaskAsDisplayFormat = true;
            this.repositoryItemTextEdit1.Name = "repositoryItemTextEdit1";
            // 
            // btnStartStop
            // 
            this.btnStartStop.Id = 8;
            this.btnStartStop.Name = "btnStartStop";
            // 
            // panelControl1
            // 
            this.panelControl1.Appearance.BackColor = System.Drawing.Color.White;
            this.panelControl1.Appearance.Options.UseBackColor = true;
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.labelControl3);
            this.panelControl1.Controls.Add(this.labelControl2);
            this.panelControl1.Controls.Add(this.btnUpdateMinAccountControl);
            this.panelControl1.Controls.Add(this.txtMinAccountControl);
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 27);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(1278, 52);
            this.panelControl1.TabIndex = 4;
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(255, 9);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(203, 33);
            this.labelControl3.TabIndex = 5;
            this.labelControl3.Text = "trong TK Chính";
            // 
            // labelControl2
            // 
            this.labelControl2.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.labelControl2.Appearance.ForeColor = System.Drawing.Color.Red;
            this.labelControl2.Appearance.Options.UseFont = true;
            this.labelControl2.Appearance.Options.UseForeColor = true;
            this.labelControl2.Location = new System.Drawing.Point(189, 9);
            this.labelControl2.Name = "labelControl2";
            this.labelControl2.Size = new System.Drawing.Size(60, 33);
            this.labelControl2.TabIndex = 4;
            this.labelControl2.Text = "VNĐ";
            // 
            // btnUpdateMinAccountControl
            // 
            this.btnUpdateMinAccountControl.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnUpdateMinAccountControl.Appearance.Options.UseFont = true;
            this.btnUpdateMinAccountControl.Appearance.Options.UseTextOptions = true;
            this.btnUpdateMinAccountControl.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.btnUpdateMinAccountControl.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.btnUpdateMinAccountControl.AppearanceHovered.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnUpdateMinAccountControl.AppearanceHovered.Options.UseFont = true;
            this.btnUpdateMinAccountControl.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            this.btnUpdateMinAccountControl.ImageOptions.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.False;
            this.btnUpdateMinAccountControl.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnUpdateMinAccountControl.ImageOptions.Image")));
            this.btnUpdateMinAccountControl.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.btnUpdateMinAccountControl.Location = new System.Drawing.Point(479, 6);
            this.btnUpdateMinAccountControl.Name = "btnUpdateMinAccountControl";
            this.btnUpdateMinAccountControl.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnUpdateMinAccountControl.Size = new System.Drawing.Size(130, 40);
            this.btnUpdateMinAccountControl.TabIndex = 2;
            this.btnUpdateMinAccountControl.Text = "Xác nhận";
            this.btnUpdateMinAccountControl.Click += new System.EventHandler(this.BtnUpdateMinAccountControl_Click);
            // 
            // txtMinAccountControl
            // 
            this.txtMinAccountControl.EditValue = "";
            this.txtMinAccountControl.Location = new System.Drawing.Point(57, 6);
            this.txtMinAccountControl.Name = "txtMinAccountControl";
            this.txtMinAccountControl.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.txtMinAccountControl.Properties.Appearance.ForeColor = System.Drawing.Color.Red;
            this.txtMinAccountControl.Properties.Appearance.Options.UseFont = true;
            this.txtMinAccountControl.Properties.Appearance.Options.UseForeColor = true;
            this.txtMinAccountControl.Properties.Appearance.Options.UseTextOptions = true;
            this.txtMinAccountControl.Properties.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.txtMinAccountControl.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
            this.txtMinAccountControl.Properties.DisplayFormat.FormatString = "n0";
            this.txtMinAccountControl.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtMinAccountControl.Properties.EditFormat.FormatString = "n0";
            this.txtMinAccountControl.Properties.EditFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.txtMinAccountControl.Properties.Mask.UseMaskAsDisplayFormat = true;
            this.txtMinAccountControl.Properties.MaskSettings.Set("MaskManagerType", typeof(DevExpress.Data.Mask.NumericMaskManager));
            this.txtMinAccountControl.Properties.MaskSettings.Set("mask", "n0");
            this.txtMinAccountControl.Size = new System.Drawing.Size(126, 40);
            this.txtMinAccountControl.TabIndex = 1;
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(14, 9);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(37, 33);
            this.labelControl1.TabIndex = 3;
            this.labelControl1.Text = "Để";
            // 
            // gcCOM
            // 
            this.gcCOM.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcCOM.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcCOM.Location = new System.Drawing.Point(0, 79);
            this.gcCOM.MainView = this.gvCOM;
            this.gcCOM.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcCOM.Name = "gcCOM";
            this.gcCOM.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemComboBox1,
            this.repositoryItemTextEdit2});
            this.gcCOM.Size = new System.Drawing.Size(1278, 609);
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
            this.TKChinh,
            this.Message101,
            this.Message,
            this.IMEI,
            this.SmsId,
            this.Telecom});
            this.gvCOM.GridControl = this.gcCOM;
            this.gvCOM.Name = "gvCOM";
            this.gvCOM.OptionsSelection.MultiSelect = true;
            this.gvCOM.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CellSelect;
            this.gvCOM.OptionsView.ShowGroupPanel = false;
            this.gvCOM.RowCellStyle += new DevExpress.XtraGrid.Views.Grid.RowCellStyleEventHandler(this.GvCOM_RowCellStyle);
            // 
            // IMEI
            // 
            this.IMEI.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.IMEI.AppearanceCell.Options.UseFont = true;
            this.IMEI.AppearanceCell.Options.UseTextOptions = true;
            this.IMEI.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.IMEI.Caption = "IMEI";
            this.IMEI.FieldName = "IMEI";
            this.IMEI.Name = "IMEI";
            this.IMEI.OptionsColumn.AllowEdit = false;
            this.IMEI.OptionsColumn.AllowSize = false;
            this.IMEI.OptionsColumn.FixedWidth = true;
            this.IMEI.OptionsFilter.AllowAutoFilter = false;
            this.IMEI.Width = 120;
            // 
            // COM
            // 
            this.COM.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.COM.AppearanceCell.Options.UseFont = true;
            this.COM.AppearanceCell.Options.UseTextOptions = true;
            this.COM.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.COM.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.COM.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
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
            this.STT.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.STT.AppearanceCell.Options.UseFont = true;
            this.STT.AppearanceCell.Options.UseTextOptions = true;
            this.STT.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.STT.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.STT.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
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
            this.ICCID.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ICCID.AppearanceCell.Options.UseFont = true;
            this.ICCID.AppearanceCell.Options.UseTextOptions = true;
            this.ICCID.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.ICCID.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.ICCID.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
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
            this.PhoneNumber.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.PhoneNumber.AppearanceCell.Options.UseFont = true;
            this.PhoneNumber.AppearanceCell.Options.UseTextOptions = true;
            this.PhoneNumber.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.PhoneNumber.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.PhoneNumber.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
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
            // TKChinh
            // 
            this.TKChinh.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.TKChinh.AppearanceCell.Options.UseFont = true;
            this.TKChinh.AppearanceCell.Options.UseTextOptions = true;
            this.TKChinh.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.TKChinh.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.TKChinh.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.TKChinh.AppearanceHeader.Options.UseTextOptions = true;
            this.TKChinh.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.TKChinh.Caption = "TK Chính";
            this.TKChinh.DisplayFormat.FormatString = "n0";
            this.TKChinh.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.TKChinh.FieldName = "TKChinh";
            this.TKChinh.Name = "TKChinh";
            this.TKChinh.OptionsColumn.AllowEdit = false;
            this.TKChinh.OptionsColumn.AllowSize = false;
            this.TKChinh.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.TKChinh.OptionsColumn.FixedWidth = true;
            this.TKChinh.OptionsFilter.AllowAutoFilter = false;
            this.TKChinh.OptionsFilter.AllowFilter = false;
            this.TKChinh.Visible = true;
            this.TKChinh.VisibleIndex = 4;
            this.TKChinh.Width = 80;
            // 
            // Message101
            // 
            this.Message101.AppearanceCell.Options.UseTextOptions = true;
            this.Message101.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message101.Caption = "*101#";
            this.Message101.FieldName = "Message101";
            this.Message101.Name = "Message101";
            this.Message101.Visible = true;
            this.Message101.VisibleIndex = 5;
            this.Message101.Width = 603;
            // 
            // Message
            // 
            this.Message.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Message.AppearanceCell.Options.UseFont = true;
            this.Message.AppearanceCell.Options.UseTextOptions = true;
            this.Message.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.Message.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.Message.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.Message.Caption = "Thông báo";
            this.Message.FieldName = "Message";
            this.Message.MinWidth = 23;
            this.Message.Name = "Message";
            this.Message.OptionsColumn.AllowEdit = false;
            this.Message.OptionsColumn.AllowSize = false;
            this.Message.OptionsColumn.AllowSort = DevExpress.Utils.DefaultBoolean.False;
            this.Message.OptionsFilter.AllowAutoFilter = false;
            this.Message.OptionsFilter.AllowFilter = false;
            this.Message.Visible = true;
            this.Message.VisibleIndex = 6;
            this.Message.Width = 300;
            // 
            // SmsId
            // 
            this.SmsId.AppearanceCell.Font = new System.Drawing.Font("Tahoma", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.SmsId.AppearanceCell.Options.UseFont = true;
            this.SmsId.AppearanceCell.Options.UseTextOptions = true;
            this.SmsId.AppearanceCell.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
            this.SmsId.Caption = "SmsId";
            this.SmsId.FieldName = "SmsId";
            this.SmsId.Name = "SmsId";
            // 
            // repositoryItemTextEdit2
            // 
            this.repositoryItemTextEdit2.AutoHeight = false;
            this.repositoryItemTextEdit2.Name = "repositoryItemTextEdit2";
            this.repositoryItemTextEdit2.ReadOnly = true;
            // 
            // TimerCheckSim
            // 
            this.TimerCheckSim.Enabled = true;
            this.TimerCheckSim.Interval = 10000;
            this.TimerCheckSim.Tick += new System.EventHandler(this.TimerCheckSim_Tick);
            // 
            // Telecom
            // 
            this.Telecom.Caption = "Telecom";
            this.Telecom.FieldName = "Telecom";
            this.Telecom.Name = "Telecom";
            // 
            // BurnTKForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1278, 688);
            this.Controls.Add(this.gcCOM);
            this.Controls.Add(this.panelControl1);
            this.Controls.Add(this.barDockControlLeft);
            this.Controls.Add(this.barDockControlRight);
            this.Controls.Add(this.barDockControlBottom);
            this.Controls.Add(this.barDockControlTop);
            this.IconOptions.SvgImage = ((DevExpress.Utils.Svg.SvgImage)(resources.GetObject("BurnTKForm.IconOptions.SvgImage")));
            this.LookAndFeel.SkinName = "Office 2019 Colorful";
            this.LookAndFeel.UseDefaultLookAndFeel = false;
            this.Name = "BurnTKForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Luck Burn";
            this.Load += new System.EventHandler(this.BurnTKForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.barManager)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtMinAccountControl.Properties)).EndInit();
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
        private DevExpress.XtraBars.BarEditItem txtMinAccount;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit1;
        private DevExpress.XtraBars.BarButtonItem btnStartStop;
        private DevExpress.XtraBars.BarSubItem btnComSettingDropDown;
        private DevExpress.XtraBars.BarButtonItem btnUpdateComPort;
        private DevExpress.XtraBars.BarButtonItem btnResetComPort;
        private DevExpress.XtraBars.BarButtonItem btnResetCom;
        private DevExpress.XtraBars.BarButtonItem btnRestoreSettings;
        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.SimpleButton btnUpdateMinAccountControl;
        private DevExpress.XtraEditors.TextEdit txtMinAccountControl;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl2;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraGrid.GridControl gcCOM;
        private DevExpress.XtraGrid.Views.Grid.GridView gvCOM;
        private DevExpress.XtraGrid.Columns.GridColumn IMEI;
        private DevExpress.XtraGrid.Columns.GridColumn COM;
        private DevExpress.XtraGrid.Columns.GridColumn STT;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox1;
        private DevExpress.XtraGrid.Columns.GridColumn ICCID;
        private DevExpress.XtraGrid.Columns.GridColumn PhoneNumber;
        private DevExpress.XtraGrid.Columns.GridColumn Message;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit2;
        private DevExpress.XtraGrid.Columns.GridColumn TKChinh;
        private System.Windows.Forms.Timer TimerCheckSim;
        private DevExpress.XtraGrid.Columns.GridColumn SmsId;
        private DevExpress.XtraBars.BarButtonItem BtnDoanhThu;
        private DevExpress.XtraGrid.Columns.GridColumn Message101;
        private DevExpress.XtraBars.BarButtonItem barBtnRule;
        private DevExpress.XtraGrid.Columns.GridColumn Telecom;
    }
}