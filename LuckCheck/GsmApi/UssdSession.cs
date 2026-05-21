using System;

namespace LuckCheck.GsmApi
{
    public enum UssdStep
    {
        AwaitingMenu,       // Đã gửi USSD code, chờ menu hiện ra
        AwaitingPhoneInput, // Đã chọn "1 - Nạp hộ", chờ prompt nhập số điện thoại
        AwaitingPinInput,   // Chờ prompt nhập mã thẻ cào
        AwaitingConfirm,    // Đã nhập PIN, chờ prompt xác nhận 1=OK / 0=Huỷ
        AwaitingResult,     // Đã gửi "1" xác nhận, chờ kết quả cuối
        Completed
    }

    public class UssdSession
    {
        public string SessionId     { get; set; }
        public string TransactionId { get; set; }   // idempotency key from Go BE
        public string ReservationId { get; set; }   // optional: linked SIM reservation
        public string PortName      { get; set; }
        public string SimId         { get; set; }
        public string GatewayId     { get; set; }
        public int    SlotIndex     { get; set; }
        public string UssdCode      { get; set; }   // e.g. "*100*123456789012#"
        public string TargetPhone   { get; set; }   // Số khách hàng cần nạp
        public string SimMsisdn     { get; set; }   // Số điện thoại của SIM đang dùng
        public string PinCode       { get; set; }   // Mã thẻ cào, extract từ UssdCode
        public UssdStep  Step       { get; set; }
        public DateTime  SubmittedAt  { get; set; }
        public DateTime? CompletedAt  { get; set; }
        public string    Status       { get; set; }   // submitted / processing / success / failed / cancelled
        public string    ResultMessage{ get; set; }

        // Khi true: state machine chỉ log + phát hiện kết quả; người dùng tự bấm response
        public bool IsManualMode { get; set; }

        // Dùng Interlocked.CompareExchange để đảm bảo CompleteUssdSession chỉ chạy 1 lần
        // 0 = chưa complete, 1 = đã complete
        public int CompletionFlag;
    }
}
