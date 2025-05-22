using System;

namespace LuckBurn.Model
{
    public class ComDto
    {
        public string IMEI { get; set; }
        public string COM { get; set; }
        public string STT { get; set; }
        public string DeviceID { get; set; }
        public string ICCID { get; set; }
        public string PhoneNumber { get; set; }
        public int TKChinh { get; set; }
        public string Message101 { get; set; }
        public string Message { get; set; }
        public bool IsFinish { get; set; }
        public Guid SmsId { get; set; }
    }
}