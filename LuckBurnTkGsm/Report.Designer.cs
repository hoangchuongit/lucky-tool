using System;

namespace LuckBurnTK
{
    partial class Report
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Report));
            this.panelControl1 = new DevExpress.XtraEditors.PanelControl();
            this.txtToDate = new DevExpress.XtraEditors.DateEdit();
            this.txtFromDate = new DevExpress.XtraEditors.DateEdit();
            this.btnXuatExcel = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl3 = new DevExpress.XtraEditors.LabelControl();
            this.btnXemBaoCao = new DevExpress.XtraEditors.SimpleButton();
            this.labelControl1 = new DevExpress.XtraEditors.LabelControl();
            this.gcReport = new DevExpress.XtraGrid.GridControl();
            this.gvReport = new DevExpress.XtraGrid.Views.Grid.GridView();
            this.created_at = new DevExpress.XtraGrid.Columns.GridColumn();
            this.phone_number = new DevExpress.XtraGrid.Columns.GridColumn();
            this.call_center_number = new DevExpress.XtraGrid.Columns.GridColumn();
            this.amount = new DevExpress.XtraGrid.Columns.GridColumn();
            this.message = new DevExpress.XtraGrid.Columns.GridColumn();
            this.status = new DevExpress.XtraGrid.Columns.GridColumn();
            this.repositoryItemComboBox1 = new DevExpress.XtraEditors.Repository.RepositoryItemComboBox();
            this.repositoryItemTextEdit2 = new DevExpress.XtraEditors.Repository.RepositoryItemTextEdit();
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).BeginInit();
            this.panelControl1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtToDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtToDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFromDate.Properties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFromDate.Properties.CalendarTimeProperties)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcReport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvReport)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).BeginInit();
            this.SuspendLayout();
            // 
            // panelControl1
            // 
            this.panelControl1.Appearance.BackColor = System.Drawing.Color.White;
            this.panelControl1.Appearance.Options.UseBackColor = true;
            this.panelControl1.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
            this.panelControl1.Controls.Add(this.txtToDate);
            this.panelControl1.Controls.Add(this.txtFromDate);
            this.panelControl1.Controls.Add(this.btnXuatExcel);
            this.panelControl1.Controls.Add(this.labelControl3);
            this.panelControl1.Controls.Add(this.btnXemBaoCao);
            this.panelControl1.Controls.Add(this.labelControl1);
            this.panelControl1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelControl1.Location = new System.Drawing.Point(0, 0);
            this.panelControl1.Name = "panelControl1";
            this.panelControl1.Size = new System.Drawing.Size(1022, 52);
            this.panelControl1.TabIndex = 5;
            // 
            // txtToDate
            // 
            this.txtToDate.EditValue = null;
            this.txtToDate.Location = new System.Drawing.Point(481, 6);
            this.txtToDate.Name = "txtToDate";
            this.txtToDate.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 20F);
            this.txtToDate.Properties.Appearance.Options.UseFont = true;
            this.txtToDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.txtToDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.txtToDate.Properties.CalendarView = DevExpress.XtraEditors.Repository.CalendarView.Classic;
            this.txtToDate.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.txtToDate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.txtToDate.Properties.MaskSettings.Set("mask", "dd/MM/yyyy");
            this.txtToDate.Properties.VistaDisplayMode = DevExpress.Utils.DefaultBoolean.False;
            this.txtToDate.Size = new System.Drawing.Size(200, 40);
            this.txtToDate.TabIndex = 7;
            // 
            // txtFromDate
            // 
            this.txtFromDate.EditValue = null;
            this.txtFromDate.Location = new System.Drawing.Point(128, 6);
            this.txtFromDate.Name = "txtFromDate";
            this.txtFromDate.Properties.Appearance.Font = new System.Drawing.Font("Tahoma", 20F);
            this.txtFromDate.Properties.Appearance.Options.UseFont = true;
            this.txtFromDate.Properties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.txtFromDate.Properties.CalendarTimeProperties.Buttons.AddRange(new DevExpress.XtraEditors.Controls.EditorButton[] {
            new DevExpress.XtraEditors.Controls.EditorButton(DevExpress.XtraEditors.Controls.ButtonPredefines.Combo)});
            this.txtFromDate.Properties.CalendarView = DevExpress.XtraEditors.Repository.CalendarView.Classic;
            this.txtFromDate.Properties.DisplayFormat.FormatString = "dd/MM/yyyy";
            this.txtFromDate.Properties.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.txtFromDate.Properties.MaskSettings.Set("mask", "dd/MM/yyyy");
            this.txtFromDate.Properties.VistaDisplayMode = DevExpress.Utils.DefaultBoolean.False;
            this.txtFromDate.Size = new System.Drawing.Size(200, 40);
            this.txtFromDate.TabIndex = 6;
            // 
            // btnXuatExcel
            // 
            this.btnXuatExcel.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnXuatExcel.Appearance.Options.UseFont = true;
            this.btnXuatExcel.Appearance.Options.UseTextOptions = true;
            this.btnXuatExcel.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.btnXuatExcel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.btnXuatExcel.AppearanceHovered.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnXuatExcel.AppearanceHovered.Options.UseFont = true;
            this.btnXuatExcel.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            this.btnXuatExcel.ImageOptions.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.False;
            this.btnXuatExcel.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnXuatExcel.ImageOptions.Image")));
            this.btnXuatExcel.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.btnXuatExcel.Location = new System.Drawing.Point(838, 6);
            this.btnXuatExcel.Name = "btnXuatExcel";
            this.btnXuatExcel.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnXuatExcel.Size = new System.Drawing.Size(155, 40);
            this.btnXuatExcel.TabIndex = 3;
            this.btnXuatExcel.Text = "Xuất Excel";
            this.btnXuatExcel.Click += new System.EventHandler(this.BtnXuatExcel_Click);
            // 
            // labelControl3
            // 
            this.labelControl3.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.labelControl3.Appearance.Options.UseFont = true;
            this.labelControl3.Location = new System.Drawing.Point(347, 9);
            this.labelControl3.Name = "labelControl3";
            this.labelControl3.Size = new System.Drawing.Size(128, 33);
            this.labelControl3.TabIndex = 5;
            this.labelControl3.Text = "Đến ngày";
            // 
            // btnXemBaoCao
            // 
            this.btnXemBaoCao.Appearance.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnXemBaoCao.Appearance.Options.UseFont = true;
            this.btnXemBaoCao.Appearance.Options.UseTextOptions = true;
            this.btnXemBaoCao.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.btnXemBaoCao.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
            this.btnXemBaoCao.AppearanceHovered.Font = new System.Drawing.Font("Tahoma", 12F, System.Drawing.FontStyle.Bold);
            this.btnXemBaoCao.AppearanceHovered.Options.UseFont = true;
            this.btnXemBaoCao.ButtonStyle = DevExpress.XtraEditors.Controls.BorderStyles.HotFlat;
            this.btnXemBaoCao.ImageOptions.AllowGlyphSkinning = DevExpress.Utils.DefaultBoolean.False;
            this.btnXemBaoCao.ImageOptions.Image = ((System.Drawing.Image)(resources.GetObject("btnXemBaoCao.ImageOptions.Image")));
            this.btnXemBaoCao.ImageOptions.Location = DevExpress.XtraEditors.ImageLocation.MiddleLeft;
            this.btnXemBaoCao.Location = new System.Drawing.Point(702, 6);
            this.btnXemBaoCao.Name = "btnXemBaoCao";
            this.btnXemBaoCao.Padding = new System.Windows.Forms.Padding(5, 0, 0, 0);
            this.btnXemBaoCao.Size = new System.Drawing.Size(130, 40);
            this.btnXemBaoCao.TabIndex = 0;
            this.btnXemBaoCao.Text = "Xem";
            this.btnXemBaoCao.Click += new System.EventHandler(this.BtnXemBaoCao_Click);
            // 
            // labelControl1
            // 
            this.labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 20F, System.Drawing.FontStyle.Bold);
            this.labelControl1.Appearance.Options.UseFont = true;
            this.labelControl1.Location = new System.Drawing.Point(12, 9);
            this.labelControl1.Name = "labelControl1";
            this.labelControl1.Size = new System.Drawing.Size(110, 33);
            this.labelControl1.TabIndex = 3;
            this.labelControl1.Text = "Từ ngày";
            // 
            // gcReport
            // 
            this.gcReport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gcReport.EmbeddedNavigator.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcReport.Location = new System.Drawing.Point(0, 52);
            this.gcReport.MainView = this.gvReport;
            this.gcReport.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.gcReport.Name = "gcReport";
            this.gcReport.RepositoryItems.AddRange(new DevExpress.XtraEditors.Repository.RepositoryItem[] {
            this.repositoryItemComboBox1,
            this.repositoryItemTextEdit2});
            this.gcReport.Size = new System.Drawing.Size(1022, 684);
            this.gcReport.TabIndex = 6;
            this.gcReport.ViewCollection.AddRange(new DevExpress.XtraGrid.Views.Base.BaseView[] {
            this.gvReport});
            // 
            // gvReport
            // 
            this.gvReport.AppearancePrint.FooterPanel.Font = new System.Drawing.Font("Tahoma", 14F);
            this.gvReport.AppearancePrint.FooterPanel.Options.UseFont = true;
            this.gvReport.Columns.AddRange(new DevExpress.XtraGrid.Columns.GridColumn[] {
            this.created_at,
            this.phone_number,
            this.call_center_number,
            this.amount,
            this.message,
            this.status});
            this.gvReport.GridControl = this.gcReport;
            this.gvReport.Name = "gvReport";
            this.gvReport.OptionsSelection.MultiSelect = true;
            this.gvReport.OptionsSelection.MultiSelectMode = DevExpress.XtraGrid.Views.Grid.GridMultiSelectMode.CellSelect;
            this.gvReport.OptionsView.ShowGroupPanel = false;
            this.gvReport.CustomColumnDisplayText += new DevExpress.XtraGrid.Views.Base.CustomColumnDisplayTextEventHandler(this.gvReport_CustomColumnDisplayText);
            // 
            // created_at
            // 
            this.created_at.AppearanceCell.Options.UseTextOptions = true;
            this.created_at.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.created_at.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.created_at.AppearanceHeader.Options.UseTextOptions = true;
            this.created_at.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.created_at.Caption = "Thời gian";
            this.created_at.DisplayFormat.FormatString = "dd/MM/yyyy HH:mm:ss";
            this.created_at.DisplayFormat.FormatType = DevExpress.Utils.FormatType.DateTime;
            this.created_at.FieldName = "created_at";
            this.created_at.MinWidth = 80;
            this.created_at.Name = "created_at";
            this.created_at.OptionsColumn.AllowEdit = false;
            this.created_at.OptionsColumn.FixedWidth = true;
            this.created_at.Visible = true;
            this.created_at.VisibleIndex = 0;
            this.created_at.Width = 80;
            // 
            // phone_number
            // 
            this.phone_number.AppearanceCell.Options.UseTextOptions = true;
            this.phone_number.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.phone_number.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.phone_number.AppearanceHeader.Options.UseTextOptions = true;
            this.phone_number.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.phone_number.Caption = "Số điện thoại";
            this.phone_number.FieldName = "phone_number";
            this.phone_number.MinWidth = 80;
            this.phone_number.Name = "phone_number";
            this.phone_number.OptionsColumn.AllowEdit = false;
            this.phone_number.OptionsColumn.FixedWidth = true;
            this.phone_number.Visible = true;
            this.phone_number.VisibleIndex = 1;
            this.phone_number.Width = 80;
            // 
            // call_center_number
            // 
            this.call_center_number.AppearanceCell.Options.UseTextOptions = true;
            this.call_center_number.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.call_center_number.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.call_center_number.AppearanceHeader.Options.UseTextOptions = true;
            this.call_center_number.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
            this.call_center_number.Caption = "Đầu số";
            this.call_center_number.FieldName = "call_center_number";
            this.call_center_number.MinWidth = 50;
            this.call_center_number.Name = "call_center_number";
            this.call_center_number.OptionsColumn.AllowEdit = false;
            this.call_center_number.OptionsColumn.FixedWidth = true;
            this.call_center_number.Visible = true;
            this.call_center_number.VisibleIndex = 2;
            this.call_center_number.Width = 50;
            // 
            // amount
            // 
            this.amount.AppearanceCell.Options.UseTextOptions = true;
            this.amount.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.amount.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.amount.AppearanceHeader.Options.UseTextOptions = true;
            this.amount.AppearanceHeader.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Far;
            this.amount.Caption = "Giá tiền";
            this.amount.DisplayFormat.FormatString = "n0";
            this.amount.DisplayFormat.FormatType = DevExpress.Utils.FormatType.Numeric;
            this.amount.FieldName = "amount";
            this.amount.MinWidth = 50;
            this.amount.Name = "amount";
            this.amount.OptionsColumn.AllowEdit = false;
            this.amount.OptionsColumn.FixedWidth = true;
            this.amount.Visible = true;
            this.amount.VisibleIndex = 3;
            this.amount.Width = 50;
            // 
            // message
            // 
            this.message.AppearanceCell.Options.UseTextOptions = true;
            this.message.AppearanceCell.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Near;
            this.message.AppearanceHeader.FontStyleDelta = System.Drawing.FontStyle.Bold;
            this.message.Caption = "Tin nhắn";
            this.message.FieldName = "message";
            this.message.MinWidth = 23;
            this.message.Name = "message";
            this.message.OptionsColumn.AllowEdit = false;
            this.message.OptionsColumn.FixedWidth = true;
            this.message.Visible = true;
            this.message.VisibleIndex = 4;
            this.message.Width = 200;
            // 
            // status
            // 
            this.status.Caption = "Trạng thái";
            this.status.FieldName = "status";
            this.status.MinWidth = 50;
            this.status.Name = "status";
            this.status.OptionsColumn.AllowEdit = false;
            this.status.OptionsColumn.FixedWidth = true;
            this.status.Visible = true;
            this.status.VisibleIndex = 5;
            this.status.Width = 50;
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
            // repositoryItemTextEdit2
            // 
            this.repositoryItemTextEdit2.AutoHeight = false;
            this.repositoryItemTextEdit2.Name = "repositoryItemTextEdit2";
            this.repositoryItemTextEdit2.ReadOnly = true;
            // 
            // Report
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1022, 736);
            this.Controls.Add(this.gcReport);
            this.Controls.Add(this.panelControl1);
            this.IconOptions.LargeImage = ((System.Drawing.Image)(resources.GetObject("Report.IconOptions.LargeImage")));
            this.Name = "Report";
            this.Text = "Luck Burn Report";
            ((System.ComponentModel.ISupportInitialize)(this.panelControl1)).EndInit();
            this.panelControl1.ResumeLayout(false);
            this.panelControl1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.txtToDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtToDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFromDate.Properties.CalendarTimeProperties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFromDate.Properties)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gcReport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.gvReport)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemComboBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.repositoryItemTextEdit2)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private DevExpress.XtraEditors.PanelControl panelControl1;
        private DevExpress.XtraEditors.LabelControl labelControl3;
        private DevExpress.XtraEditors.SimpleButton btnXemBaoCao;
        private DevExpress.XtraEditors.LabelControl labelControl1;
        private DevExpress.XtraEditors.SimpleButton btnXuatExcel;
        private DevExpress.XtraGrid.GridControl gcReport;
        private DevExpress.XtraGrid.Views.Grid.GridView gvReport;
        private DevExpress.XtraGrid.Columns.GridColumn created_at;
        private DevExpress.XtraGrid.Columns.GridColumn phone_number;
        private DevExpress.XtraGrid.Columns.GridColumn call_center_number;
        private DevExpress.XtraGrid.Columns.GridColumn message;
        private DevExpress.XtraGrid.Columns.GridColumn amount;
        private DevExpress.XtraGrid.Columns.GridColumn status;
        private DevExpress.XtraEditors.Repository.RepositoryItemComboBox repositoryItemComboBox1;
        private DevExpress.XtraEditors.Repository.RepositoryItemTextEdit repositoryItemTextEdit2;
        private DevExpress.XtraEditors.DateEdit txtFromDate;
        private DevExpress.XtraEditors.DateEdit txtToDate;
    }
}