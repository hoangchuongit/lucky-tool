using System;

namespace LuckBurn.Model
{
    public class ComDto
    {
        public string COM { get; set; }
        public string STT { get; set; }
        public string ICCID { get; set; }
        public string PhoneNumber { get; set; }
        public int TKChinh { get; set; }
        public string Message101 { get; set; }
        public string Message { get; set; }
        public bool IsFinish { get; set; }
        public string Telecom { get; set; }
        public string HSD { get; set; }
    }

    public class FileToCom
    {
        public int fd { get; set; }
        public byte[] data { get; set; }
    }

    public class CallDetail
    {
        // id trên redis
        public string request_id { get; set; }

        // đầu số gửi SMS hoặc Call
        public string prefix { get; set; }

        // đơn vị cung cấp đầu số
        public string prefix_unit { get; set; }

        // tin nhắn cần gửi đi hoặc content cần phát trong cuộc gọi
        public string message { get; set; }

        // id của bảng history_sms
        public Guid history_id { get; set; }

        public DateTime start_call { get; set; }
        public DateTime start_record { get; set; }
        public DateTime end_record { get; set; }

        // Thời gian cuộc gọi trong khoảng 10-25s hoặc 45-60s
        public int call_duration { get; set; }

        public bool no_carrier { get; set; }
    }
}