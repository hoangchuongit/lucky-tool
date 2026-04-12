using System;

namespace LuckCheck.Model
{
    public class ComDto
    {
        public string COM { get; set; }
        public string STT { get; set; }
        public string ICCID { get; set; }
        public string PhoneNumber { get; set; }
        public string HSD { get; set; }
        public int TKChinh { get; set; }
        public string Message101 { get; set; }

        // ─── [MỚI] Dùng cho checkbox chọn row trong GridView ─────────────────
        // Không dùng trực tiếp trong CheckBoxRowSelect mode (DevExpress quản lý),
        // nhưng giữ để có thể query trạng thái chọn nếu cần về sau.
        public bool IsSelected { get; set; }
    }
}