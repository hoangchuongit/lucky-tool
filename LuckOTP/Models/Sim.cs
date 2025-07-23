using System;

namespace LuckOTP.Model
{
    public class Sim
    {
        public string iccid { get; set; }
        public string phone_number { get; set; }
        public sim_status_enum sim_status { get; set; }
    }

    public enum sim_status_enum
    {
        /// <summary>
        /// Sim is inserted
        /// </summary>
        SIM_INSERTED,

        /// <summary>
        /// Sim has been removed
        /// </summary>
        SIM_REMOVED
    }

    public class OtpStatusPayload
    {
        public string phone_number { get; set; }
        public string status { get; set; }
    }

    public class CustomerIdChangePayload
    {
        public string phone_number { get; set; }
        public string customer_id { get; set; }
        public string telegram_status { get; set; }
    }

    public class SimListenCurrentServiceCode : CustomerIdChangePayload
    {
        public string service_code { get; set; }
    }

    public class GsmReportDto
    {
        public Int64 stt { get; set; }
        public DateTime created_date { get; set; }
        public string code { get; set; }
        public string name { get; set; }
        public Int64 total { get; set; }
        public Decimal amount { get; set; }
        public Decimal total_amount { get; set; }
    }

    public class GsmReportICCIDDto
    {
        public string phone_number { get; set; }
        public Int64 total { get; set; }
    }
}