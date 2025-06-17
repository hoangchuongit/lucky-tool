using System;

namespace LuckBurnTK.Models
{
    public class PrefixNumberDto
    {
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
            public int duration { get; set; }
            public int no_carrier { get; set; }
        }

        public class GetPrefixSmsRes
        {
            public string request_id { get; set; }
            public string prefix { get; set; }
            public string prefix_unit { get; set; }
            public PrefixNumberType type { get; set; }
            public string message { get; set; }
            public Guid history_id { get; set; }
        }

        public enum PrefixNumberType
        {
            SMS,
            CALL
        }
    }
}
