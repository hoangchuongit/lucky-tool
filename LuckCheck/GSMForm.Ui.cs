// Partial class — USSD interaction panel + layout setup + grid enhancements.
// Tách riêng để không đụng vào GSMForm.Designer.cs (auto-generated).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.Utils;
using DevExpress.XtraEditors;
using DevExpress.XtraGrid.Columns;
using LuckCheck.GsmApi;
using LuckCheck.Model;

namespace LuckCheck
{
    public partial class GSMForm
    {
        // ──────────────────── UI Fields ────────────────────

        private string _selectedPortName;

        private SplitContainer _mainSplit;

        // Header + log
        private LabelControl _lblSimHeader;
        private RichTextBox _rtbUssdLog;

        // "Phản hồi USSD" group
        private GroupControl _grpUssd;
        private SimpleButton _btnU1, _btnU2, _btnU3, _btnU0, _btnUCancel;
        private TextEdit _txtUssdReply;
        private SimpleButton _btnUssdSend;

        // "Nạp thẻ" group
        private GroupControl _grpNapThe;
        private TextEdit _txtSoCan, _txtMaThe, _txtUssdTemplate;
        private CheckEdit _chkAutoMode;
        private SimpleButton _btnBatDauNap;

        // Stats bar
        private LabelControl _lblStats;

        // ──────────────────── Layout setup (called from InitializeControls) ────────────────────

        internal void InitializeUssdPanel()
        {
            // Tách gcCOM ra khỏi form, đặt vào SplitContainer Panel1
            Controls.Remove(gcCOM);

            _mainSplit = new SplitContainer
            {
                Orientation = Orientation.Vertical,
                Dock = DockStyle.Fill,
                SplitterWidth = 4,
            };

            // Không set Panel1MinSize / Panel2MinSize trong object initializer.
            // Lúc Form mới khởi tạo Width có thể còn rất nhỏ, set MinSize/SplitterDistance sớm
            // sẽ gây lỗi: "SplitterDistance must be between Panel1MinSize and Width - Panel2MinSize".
            _mainSplit.HandleCreated += (s, e) => BeginInvoke(new Action(ApplySafeMainSplitLayout));
            _mainSplit.SizeChanged += (s, e) => ApplySafeMainSplitLayout();

            _mainSplit.Panel1.Controls.Add(gcCOM);
            gcCOM.Dock = DockStyle.Fill;

            BuildRightPanel(_mainSplit.Panel2);

            Controls.Add(_mainSplit);
            // Giữ đúng thứ tự dock: barDockControls docked trước, _mainSplit fill sau
            Controls.SetChildIndex(_mainSplit, 0);

            // Grid enhancements: cột Trạng thái + màu row + selection event
            SetupGridEnhancements();
        }

        private void ApplySafeMainSplitLayout()
        {
            if (_mainSplit == null || _mainSplit.IsDisposed) return;

            int width = _mainSplit.ClientSize.Width;
            if (width <= 0) return;

            // Chọn min-size theo width hiện tại để không vượt quá kích thước control.
            int desiredPanel1Min = 300;
            int desiredPanel2Min = 200;
            int totalMin = desiredPanel1Min + desiredPanel2Min + _mainSplit.SplitterWidth;

            if (width > totalMin)
            {
                _mainSplit.Panel1MinSize = desiredPanel1Min;
                _mainSplit.Panel2MinSize = desiredPanel2Min;
            }
            else
            {
                int safeMin = Math.Max(25, (width - _mainSplit.SplitterWidth) / 4);
                _mainSplit.Panel1MinSize = safeMin;
                _mainSplit.Panel2MinSize = safeMin;
            }

            int minDistance = _mainSplit.Panel1MinSize;
            int maxDistance = width - _mainSplit.Panel2MinSize - _mainSplit.SplitterWidth;
            if (maxDistance < minDistance) return;

            int desiredDistance = (int)(width * 0.70);
            int safeDistance = Math.Max(minDistance, Math.Min(desiredDistance, maxDistance));

            if (_mainSplit.SplitterDistance != safeDistance)
                _mainSplit.SplitterDistance = safeDistance;
        }

        // ──────────────────── Right panel ────────────────────

        private void BuildRightPanel(SplitterPanel container)
        {
            container.Padding = new Padding(3, 3, 3, 0);

            // ── Header ──
            _lblSimHeader = new LabelControl
            {
                Dock = DockStyle.Top,
                Height = 28,
                Text = "─  Chọn một SIM để xem hội thoại USSD  ─",
                AutoSizeMode = LabelAutoSizeMode.None,
            };
            _lblSimHeader.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
            _lblSimHeader.Appearance.Font = new Font("Verdana", 8.5f, FontStyle.Bold);
            _lblSimHeader.Appearance.BackColor = Color.FromArgb(41, 128, 185);
            _lblSimHeader.Appearance.ForeColor = Color.White;
            _lblSimHeader.Appearance.Options.UseBackColor = true;
            _lblSimHeader.Appearance.Options.UseForeColor = true;
            _lblSimHeader.Appearance.Options.UseFont = true;

            // ── Stats bar ──
            _lblStats = new LabelControl
            {
                Dock = DockStyle.Bottom,
                Height = 20,
                Text = "Tổng: 0  |  Rảnh: 0  |  Bận: 0  |  Offline: 0",
                AutoSizeMode = LabelAutoSizeMode.None,
            };
            _lblStats.Appearance.TextOptions.HAlignment = HorzAlignment.Center;
            _lblStats.Appearance.Font = new Font("Verdana", 7.5f);
            _lblStats.Appearance.BackColor = Color.FromArgb(44, 62, 80);
            _lblStats.Appearance.ForeColor = Color.White;
            _lblStats.Appearance.Options.UseBackColor = true;
            _lblStats.Appearance.Options.UseForeColor = true;
            _lblStats.Appearance.Options.UseFont = true;

            // ── "Nạp thẻ" group (bottom fixed) ──
            _grpNapThe = BuildNapTheGroup();

            // ── "Phản hồi USSD" group (bottom fixed, above Nạp thẻ) ──
            _grpUssd = BuildUssdResponseGroup();

            // ── Conversation log (fills remaining height) ──
            _rtbUssdLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(25, 25, 25),
                ForeColor = Color.FromArgb(0, 210, 80),
                Font = new Font("Consolas", 9f),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
            };

            // Add order matters for Dock stacking:
            //   Top controls fill downward in add order
            //   Bottom controls fill upward in add order
            //   Fill must be added LAST
            container.Controls.Add(_lblSimHeader);  // Top  → stays at very top
            container.Controls.Add(_lblStats);      // Bottom → stays at very bottom
            container.Controls.Add(_grpNapThe);     // Bottom → above stats
            container.Controls.Add(_grpUssd);       // Bottom → above grpNapThe
            container.Controls.Add(_rtbUssdLog);    // Fill  → everything remaining
        }

        private GroupControl BuildUssdResponseGroup()
        {
            var grp = new GroupControl
            {
                Text = "Phản hồi USSD",
                Dock = DockStyle.Bottom,
                Height = 82,
            };

            // Row 1: quick buttons
            var rowBtns = new Panel { Dock = DockStyle.Top, Height = 30, Padding = new Padding(2) };

            _btnU1 = MakeQuickBtn("1", Color.FromArgb(39, 174, 96));
            _btnU2 = MakeQuickBtn("2", Color.FromArgb(41, 128, 185));
            _btnU3 = MakeQuickBtn("3", Color.FromArgb(142, 68, 173));
            _btnU0 = MakeQuickBtn("0", Color.FromArgb(127, 140, 141));
            _btnUCancel = MakeQuickBtn("Huỷ", Color.FromArgb(192, 57, 43));

            _btnU1.Dock = DockStyle.Left; _btnU1.Width = 36;
            _btnU2.Dock = DockStyle.Left; _btnU2.Width = 36;
            _btnU3.Dock = DockStyle.Left; _btnU3.Width = 36;
            _btnU0.Dock = DockStyle.Left; _btnU0.Width = 36;
            _btnUCancel.Dock = DockStyle.Right; _btnUCancel.Width = 54;

            _btnU1.Click += (s, e) => OnUssdQuickBtn("1");
            _btnU2.Click += (s, e) => OnUssdQuickBtn("2");
            _btnU3.Click += (s, e) => OnUssdQuickBtn("3");
            _btnU0.Click += (s, e) => OnUssdQuickBtn("0");
            _btnUCancel.Click += BtnUssdCancel_Click;

            // Add in order: Right-docked first, then Left-docked in reverse
            rowBtns.Controls.Add(_btnUCancel);
            rowBtns.Controls.Add(_btnU0);
            rowBtns.Controls.Add(_btnU3);
            rowBtns.Controls.Add(_btnU2);
            rowBtns.Controls.Add(_btnU1);

            // Row 2: custom input + Gửi
            var rowInput = new Panel { Dock = DockStyle.Fill, Padding = new Padding(2) };

            _txtUssdReply = new TextEdit { Dock = DockStyle.Fill };
            _txtUssdReply.Properties.NullValuePrompt = "Nhập phản hồi tùy ý...";
            _txtUssdReply.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) OnUssdSend(); };

            _btnUssdSend = new SimpleButton
            {
                Text = "Gửi",
                Dock = DockStyle.Right,
                Width = 52,
            };
            _btnUssdSend.Appearance.BackColor = Color.FromArgb(39, 174, 96);
            _btnUssdSend.Appearance.ForeColor = Color.White;
            _btnUssdSend.Appearance.Options.UseBackColor = true;
            _btnUssdSend.Appearance.Options.UseForeColor = true;
            _btnUssdSend.Click += (s, e) => OnUssdSend();

            rowInput.Controls.Add(_txtUssdReply);
            rowInput.Controls.Add(_btnUssdSend);

            grp.Controls.Add(rowInput);  // Fill  → bottom part of group
            grp.Controls.Add(rowBtns);   // Top   → top part of group
            return grp;
        }

        private GroupControl BuildNapTheGroup()
        {
            var grp = new GroupControl
            {
                Text = "Nạp thẻ tự động",
                Dock = DockStyle.Bottom,
                Height = 150,
            };

            var tbl = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                RowCount = 5,
                ColumnCount = 2,
                Padding = new Padding(2),
            };
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 72));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            for (int i = 0; i < 5; i++)
                tbl.RowStyles.Add(new RowStyle(SizeType.Percent, 20));

            _txtSoCan = new TextEdit(); _txtSoCan.Properties.NullValuePrompt = "Số cần nạp...";
            _txtMaThe = new TextEdit(); _txtMaThe.Properties.NullValuePrompt = "Mã thẻ cào...";
            _txtUssdTemplate = new TextEdit(); _txtUssdTemplate.Text = "*103*{PIN}#";
            _txtUssdTemplate.Properties.NullValuePrompt = "*103*{PIN}#";

            AddTableRow(tbl, 0, "Số nạp:", _txtSoCan);
            AddTableRow(tbl, 1, "Mã thẻ:", _txtMaThe);
            AddTableRow(tbl, 2, "USSD:", _txtUssdTemplate);

            _chkAutoMode = new CheckEdit { Text = "Chế độ tự động (state machine tự xử lý)", Checked = true };
            _chkAutoMode.Dock = DockStyle.Fill;
            tbl.Controls.Add(_chkAutoMode, 0, 3);
            tbl.SetColumnSpan(_chkAutoMode, 2);

            _btnBatDauNap = new SimpleButton
            {
                Text = "BẮT ĐẦU NẠP THẺ",
                Dock = DockStyle.Fill,
                Font = new Font("Verdana", 8.5f, FontStyle.Bold),
            };
            _btnBatDauNap.Appearance.BackColor = Color.FromArgb(52, 152, 219);
            _btnBatDauNap.Appearance.ForeColor = Color.White;
            _btnBatDauNap.Appearance.Options.UseBackColor = true;
            _btnBatDauNap.Appearance.Options.UseForeColor = true;
            _btnBatDauNap.Click += BtnBatDauNap_Click;
            tbl.Controls.Add(_btnBatDauNap, 0, 4);
            tbl.SetColumnSpan(_btnBatDauNap, 2);

            grp.Controls.Add(tbl);
            return grp;
        }

        private static void AddTableRow(TableLayoutPanel tbl, int row, string label, Control input)
        {
            var lbl = new LabelControl
            {
                Text = label,
                Dock = DockStyle.Fill,
                AutoSizeMode = LabelAutoSizeMode.None,
            };
            lbl.Appearance.TextOptions.HAlignment = HorzAlignment.Far;
            tbl.Controls.Add(lbl, 0, row);
            input.Dock = DockStyle.Fill;
            tbl.Controls.Add(input, 1, row);
        }

        private static SimpleButton MakeQuickBtn(string text, Color backColor)
        {
            var btn = new SimpleButton { Text = text };
            btn.Appearance.BackColor = backColor;
            btn.Appearance.ForeColor = Color.White;
            btn.Appearance.Font = new Font("Verdana", 8f, FontStyle.Bold);
            btn.Appearance.Options.UseBackColor = true;
            btn.Appearance.Options.UseForeColor = true;
            btn.Appearance.Options.UseFont = true;
            return btn;
        }

        // ──────────────────── Grid enhancements ────────────────────

        private void SetupGridEnhancements()
        {
            // Cột "Trạng thái" dựa trên StatusText property của ComDto
            var colStatus = new GridColumn
            {
                FieldName = "StatusText",
                Caption = "TT",
                Name = "colStatusText",
                Width = 58,
                VisibleIndex = 0,   // đặt trước cột COM
            };
            colStatus.OptionsColumn.AllowEdit = false;
            colStatus.OptionsColumn.AllowSort = DefaultBoolean.False;
            colStatus.AppearanceCell.TextOptions.HAlignment = HorzAlignment.Center;
            colStatus.AppearanceHeader.TextOptions.HAlignment = HorzAlignment.Center;
            GridViewCOM.Columns.Add(colStatus);

            // Row coloring
            GridViewCOM.RowStyle += GridViewCOM_RowStyle;
            GridViewCOM.FocusedRowChanged += GridViewCOM_FocusedRowChanged;
        }

        private void GridViewCOM_RowStyle(object sender,
            DevExpress.XtraGrid.Views.Grid.RowStyleEventArgs e)
        {
            if (e.RowHandle < 0) return; // header / group rows
            if (!(GridViewCOM.GetRow(e.RowHandle) is ComDto dto)) return;

            if (dto.IsDisabled)
            {
                e.Appearance.BackColor = Color.FromArgb(255, 204, 204); // đỏ nhạt (vô hiệu)
            }
            else if (dto.IsBusy)
            {
                e.Appearance.BackColor = Color.FromArgb(204, 229, 255); // xanh dương nhạt (bận)
            }
            else if (!string.IsNullOrEmpty(dto.PhoneNumber))
            {
                e.Appearance.BackColor = Color.FromArgb(198, 239, 206); // xanh lá nhạt (rảnh)
            }
            else
            {
                e.Appearance.BackColor = Color.FromArgb(220, 220, 220); // xám nhạt (offline)
            }
            e.HighPriority = true; // override DevExpress selection highlight
        }

        private void GridViewCOM_FocusedRowChanged(object sender,
            DevExpress.XtraGrid.Views.Base.FocusedRowChangedEventArgs e)
        {
            if (!(GridViewCOM.GetFocusedRow() is ComDto dto)) return;
            _selectedPortName = dto.COM;
            UpdateSimHeader(dto);
            RefreshUssdLog(dto.COM);
        }

        // ──────────────────── Conversation log display ────────────────────

        internal void RefreshUssdLog(string portName)
        {
            // Luôn gọi trên UI thread
            if (InvokeRequired) { BeginInvoke(new Action(() => RefreshUssdLog(portName))); return; }

            _rtbUssdLog.SuspendLayout();
            _rtbUssdLog.Clear();

            if (_ussdLogs.TryGetValue(portName, out var log))
            {
                string[] snapshot;
                lock (log) { snapshot = log.ToArray(); }

                foreach (var line in snapshot)
                {
                    Color c = line.Contains(" ← ") ? Color.FromArgb(0, 200, 80)    // carrier = green
                            : line.Contains(" ● ") ? Color.FromArgb(255, 200, 50)  // result  = yellow
                            : Color.FromArgb(100, 180, 255);                        // sent    = blue

                    _rtbUssdLog.SelectionColor = c;
                    _rtbUssdLog.AppendText(line + "\n");
                }
            }

            _rtbUssdLog.ResumeLayout();
            if (_rtbUssdLog.Text.Length > 0)
            {
                _rtbUssdLog.SelectionStart = _rtbUssdLog.Text.Length;
                _rtbUssdLog.ScrollToCaret();
            }
        }

        private void UpdateSimHeader(ComDto dto)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateSimHeader(dto))); return; }

            string phone = string.IsNullOrEmpty(dto.PhoneNumber) ? "—" : dto.PhoneNumber;
            string status = dto.StatusText;

            // Hiển thị step hiện tại nếu có session đang chạy
            string stepInfo = string.Empty;
            if (_portToSession.TryGetValue(dto.COM, out string sid) &&
                _ussdSessions.TryGetValue(sid, out var sess) &&
                sess.Step != UssdStep.Completed)
            {
                stepInfo = $"  ⟫ {StepLabel(sess.Step)}";
            }

            _lblSimHeader.Text = $"{dto.COM}   {phone}   [{status}]{stepInfo}";

            _lblSimHeader.Appearance.BackColor =
                dto.IsDisabled ? Color.FromArgb(192, 57, 43)  // red   (vô hiệu)
                : dto.IsBusy ? Color.FromArgb(41, 128, 185)  // blue  (bận)
                : !string.IsNullOrEmpty(dto.PhoneNumber) ? Color.FromArgb(39, 174, 96)  // green (rảnh)
                : Color.FromArgb(100, 100, 100);                                           // gray  (offline)
            _lblSimHeader.Appearance.Options.UseBackColor = true;

            // Bật/tắt nút nạp thẻ theo trạng thái SIM
            UpdateNapTheButtonState(dto.COM, dto.IsBusy || dto.IsDisabled);
        }

        private static string StepLabel(UssdStep step)
        {
            switch (step)
            {
                case UssdStep.AwaitingMenu: return "Chờ menu...";
                case UssdStep.AwaitingPhoneInput: return "Nhập số ĐT...";
                case UssdStep.AwaitingPinInput: return "Nhập mã thẻ...";
                case UssdStep.AwaitingConfirm: return "Chờ xác nhận...";
                case UssdStep.AwaitingResult: return "Chờ kết quả...";
                default: return "";
            }
        }

        // Cập nhật trạng thái nút "BẮT ĐẦU NẠP THẺ" — không gọi khi portName khác SIM đang chọn
        internal void UpdateNapTheButtonState(string portName, bool isBusy)
        {
            if (_btnBatDauNap == null) return;
            if (portName != _selectedPortName) return;
            if (InvokeRequired) { BeginInvoke(new Action(() => UpdateNapTheButtonState(portName, isBusy))); return; }

            _btnBatDauNap.Enabled = !isBusy;
            _btnBatDauNap.Text = isBusy ? "ĐANG NẠP THẺ..." : "BẮT ĐẦU NẠP THẺ";
        }

        // ──────────────────── Stats bar ────────────────────

        internal void UpdateStatsBar()
        {
            if (_lblStats == null) return;
            int total = _comDtoMap.Count, busy = 0, avail = 0, disabled = 0;
            foreach (var dto in _comDtoMap.Values)
            {
                if (dto.IsDisabled) disabled++;
                else if (dto.IsBusy) busy++;
                else if (!string.IsNullOrEmpty(dto.PhoneNumber)) avail++;
            }
            int offline = total - busy - avail - disabled;
            _lblStats.Text = $"Tổng: {total}  |  Rảnh: {avail}  |  Bận: {busy}  |  Offline: {offline}  |  V/hiệu: {disabled}";
        }

        // ──────────────────── USSD response handlers ────────────────────

        private void OnUssdQuickBtn(string value)
        {
            if (string.IsNullOrEmpty(_selectedPortName)) return;
            var sp = SerialPorts.FirstOrDefault(x => x.PortName == _selectedPortName);
            if (sp == null) return;
            // Gửi trực tiếp — không qua state machine
            _ = Task.Run(() => SendUssdReply(sp, value));
        }

        private void OnUssdSend()
        {
            string reply = _txtUssdReply.Text.Trim();
            if (string.IsNullOrEmpty(reply)) return;
            _txtUssdReply.Text = string.Empty;
            OnUssdQuickBtn(reply);
        }

        private void BtnUssdCancel_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPortName)) return;
            var sp = SerialPorts.FirstOrDefault(x => x.PortName == _selectedPortName);
            if (sp == null) return;
            _ = Task.Run(() =>
            {
                try
                {
                    lock (_portLocks[sp.PortName]) MessageCOMs[sp.PortName] = string.Empty;
                    sp.Write("AT+CUSD=2\r");
                    AppendUssdLog(sp.PortName, "→", "[Huỷ USSD - AT+CUSD=2]");
                }
                catch (Exception ex) { logger.Error($"HuỷUSSD: {ex.Message}"); }
            });
        }

        // ──────────────────── "Nạp thẻ" button ────────────────────

        private void BtnBatDauNap_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_selectedPortName))
            {
                XtraMessageBox.Show("Chọn một SIM trong bảng trước.", "Chưa chọn SIM",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!_comDtoMap.TryGetValue(_selectedPortName, out var dto))
                return;

            string maThe = _txtMaThe.Text.Trim();
            string template = _txtUssdTemplate.Text.Trim();

            if (string.IsNullOrEmpty(maThe) || string.IsNullOrEmpty(template))
            {
                XtraMessageBox.Show("Vui lòng nhập Mã thẻ cào và USSD template.",
                    "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!template.Contains("{PIN}"))
            {
                XtraMessageBox.Show("USSD template phải chứa {PIN} (ví dụ: *103*{PIN}#).",
                    "Template không hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string ussdCode = template.Replace("{PIN}", maThe);
            bool isManual = !_chkAutoMode.Checked;

            // Che PIN trong confirmation: chỉ hiện 4 số cuối
            string maskedPin = maThe.Length > 4
                ? new string('*', maThe.Length - 4) + maThe.Substring(maThe.Length - 4)
                : new string('*', maThe.Length);
            string soNap = string.IsNullOrEmpty(_txtSoCan.Text.Trim()) ? dto.PhoneNumber : _txtSoCan.Text.Trim();
            string mode = isManual ? "THỦ CÔNG (bạn tự bấm)" : "TỰ ĐỘNG (state machine)";

            string confirmMsg =
                $"SIM:       {dto.COM}  ({dto.PhoneNumber})\n" +
                $"Số nạp:    {(string.IsNullOrEmpty(soNap) ? "(chính SIM này)" : soNap)}\n" +
                $"Mã thẻ:    {maskedPin}\n" +
                $"USSD:      {ussdCode}\n" +
                $"Chế độ:    {mode}\n\n" +
                "Kiểm tra kỹ trước khi xác nhận — thẻ đã gửi không thể thu hồi.";

            if (XtraMessageBox.Show(confirmMsg, "Xác nhận nạp thẻ",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) != DialogResult.OK)
                return;

            // Xoá log cũ, hiện trạng thái bắt đầu
            if (_ussdLogs.TryGetValue(_selectedPortName, out var log))
                lock (log) { log.Clear(); }
            AppendUssdLog(_selectedPortName, "●", $"Bắt đầu nạp: {ussdCode}");
            RefreshUssdLog(_selectedPortName);

            var req = new UssdSendRequest
            {
                SimId = dto.ICCID,
                Msisdn = dto.PhoneNumber,
                GatewayId = GatewayId,
                UssdCode = ussdCode,
                PhoneNumber = _txtSoCan.Text.Trim(),
                TimeoutSecs = 60,
            };

            try
            {
                var response = HandleUssdSend(req);

                // Đặt manual mode sau khi session đã tạo
                if (isManual && _ussdSessions.TryGetValue(response.SessionId, out var session))
                    session.IsManualMode = true;
            }
            catch (Exception ex)
            {
                XtraMessageBox.Show($"Lỗi khởi động nạp thẻ: {ex.Message}", "Lỗi",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
