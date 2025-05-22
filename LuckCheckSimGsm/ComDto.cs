using System.ComponentModel;

namespace LuckOTP.Model
{
    public class ComDto
    {
        [DisplayName("DeviceID")]
        public string DeviceID { get; set; }

        [DisplayName("COM")]
        public string Com { get; set; }

        [DisplayName("Stt")]
        public string Stt { get; set; }

        [DisplayName("ICCID")]
        public string ICCID { get; set; }

        [DisplayName("Phone")]
        public string Phone { get; set; }

        [DisplayName("Trạng thái")]
        public string TrangThai { get; set; }

        [DisplayName("Trạng thái Tele")]
        public string TeleStatus { get; set; }

        [DisplayName("Message")]
        public string Message { get; set; }
    }
}