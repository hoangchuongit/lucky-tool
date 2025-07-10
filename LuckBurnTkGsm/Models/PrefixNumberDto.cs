using System;

namespace LuckBurnTK.Models
{
    public class PrefixNumberDto
    {
        public class GetUserInfor
        {
            public string fullname { get; set; }
            public string ngan_hang { get; set; }
            public string so_tai_khoan { get; set; }
            public string chu_tai_khoan { get; set; }
        }

        public class GetPrefixSmsReq
        {
            public string phone_number { get; set; }
            public string telecom { get; set; }
            public double amount { get; set; }
            public double amount_left { get; set; }
        }

        public class ReleaseSlotReq
        {
            public string request_id { get; set; }
            public string history_id { get; set; }
            public string prefix { get; set; }
            public string prefix_unit { get; set; }
            public string start_call { get; set; }
            public string end_call { get; set; }
            public string start_record { get; set; }
            public string end_record { get; set; }
            public int? duration { get; set; }
            public int no_carrier { get; set; }
        }

        public class ReleaseUploadFileReq
        {
            public string history_id { get; set; }
        }

        public class GetPrefixSmsRes
        {
            public string request_id { get; set; }
            public string prefix { get; set; }
            public string prefix_unit { get; set; }
            public PrefixNumberType type { get; set; }
            public string message { get; set; }
            public Guid history_id { get; set; }
            public int duration { get; set; }
        }

        public class GetRevenueTotalRes
        {
            public string status { get; set; }
            public int count_status { get; set; }
            public float sum_status { get; set; }
        }

        public class GetRevenueDetailRes
        {
            public string created_at { get; set; }
            public string phone_number { get; set; }
            public float amount { get; set; }
            public string status { get; set; }
        }

        public enum PrefixNumberType
        {
            SMS,
            CALL
        }

        public enum StatusEnum
        {
            WAITING,
            PENDING,
            SUCCESS,
            FAIL,
            DONE
        }

        public class VMGSmsRes
        {
            public string phone { get; set; }
            public string serviceCode { get; set; }
            public string commandCode { get; set; }
            public bool result { get; set; }
        }

        public class VMGSmsReq
        {
            public string request_id { get; set; }
            public string history_id { get; set; }
            public string prefix { get; set; }
            public string prefix_unit { get; set; }
            public string status { get; set; }
        }

        public enum TranferMoneyEnum
        {
            TWO_FRIENDS,
            SENDI
        }

        public class TranferMoneyReq
        {
            public string phone_number { get; set; }
            public double amount { get; set; }
            public double amount_left { get; set; }
            public string type { get; set; }
            public Guid history_id { get; set; }
        }

        public class TranferMoneyRes
        {
            public string sim_dst { get; set; }
            public string message { get; set; }
            public Guid history_id { get; set; }
        }

        public class TranferMoneySMSPort
        {
            public string history_id { get; set; }
            public string message { get; set; }
        }
    }
}