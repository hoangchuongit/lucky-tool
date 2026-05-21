namespace LuckCheck.Model
{
    public class ComDto
    {
        public string COM         { get; set; }
        public string STT         { get; set; }
        public string ICCID       { get; set; }
        public string PhoneNumber { get; set; }
        public string HSD         { get; set; }
        public int    TKChinh     { get; set; }
        public string Message     { get; set; }

        // Hiển thị trạng thái / kết quả thao tác cho người dùng
        public string Message101  { get; set; }

        // SIM đang bị giữ (reserve) cho một giao dịch USSD
        public bool   IsBusy      { get; set; }

        // Cường độ tín hiệu 0–100 (từ AT+CSQ), dùng cho health check
        public int    SignalStrength { get; set; }

        // Carrier name (từ AT+COPS), e.g. "Viettel", "Vinaphone"
        public string Network { get; set; }

        // Thời gian giao dịch USSD cuối cùng (UTC)
        public DateTime? LastUssdAt { get; set; }

        // Bị vô hiệu hóa bởi admin (persistent qua GsmStore.sim_disabled)
        public bool IsDisabled { get; set; }

        // Trạng thái hiển thị (computed, không lưu DB)
        public string StatusText =>
            IsDisabled                           ? "Vô hiệu"
            : IsBusy                             ? "Bận"
            : !string.IsNullOrEmpty(PhoneNumber) ? "Rảnh"
            : "Offline";
    }
}