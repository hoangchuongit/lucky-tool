using System;

namespace LuckOTP.Model
{
    public class SimMessage
    {
        public Guid id { get; set; }
        public string phone_number { get; set; }
        public string service_code { get; set; }
        public string message { get; set; }
        public DateTime time { get; set; }
    }

    public class SimCountMessage
    {
        public string service_code { get; set; }
        public Int64 service_count { get; set; }
    }
}