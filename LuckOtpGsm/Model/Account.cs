using System;

namespace LuckOTP.Model
{
    public class Account
    {
        public Guid id { get; set; }
        public string email { get; set; }
        public string phone_number { get; set; }
        public string fullname { get; set; }
        public string avatar { get; set; }
        public string socket_id { get; set; }
        public string role { get; set; }
        public string api_key { get; set; }
        public string access_token { get; set; }
        public string refresh_token { get; set; }
    }

    public enum role_enum
    {
        /// <summary>
        /// Quản trị
        /// </summary>
        ADMIN,

        /// <summary>
        /// Đơn vị cung cấp SIM
        /// </summary>
        SUPPLIER,

        /// <summary>
        /// Khách hàng thuê OTP
        /// </summary>
        CUSTOMER
    }
}