using LuckBurnTK.Utils;
using Npgsql;
using NpgsqlTypes;
using System;
using System.Data;

namespace LuckBurnTK.Controllers
{
    public class DBController
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
        private readonly string _connectionString;

        public DBController()
        {
            var conn = "l6vLlBYtcCVQNhvBCD6xZ/zArj1VdTZ8esNCrv2LjvvmsAisJkwCVF12KltPXuS6roxtwvnDcXboajoaH88iqNN1dgfBL7n5ivKBlpJN/ApZJIe/5BJwaTudZVEYi9sj3RfAdNTdg9BPY4iWZRl1iLx1UQtSxk1GPEG1s5ca+O8=";
            _connectionString = SecureConfig.Decrypt(conn);
        }

        /// <summary>
        /// Lấy prefix và message từ database dựa trên thông tin truyền vào
        /// </summary>
        /// <param name="phoneNumber">Số điện thoại</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="amountLeft">Số tiền còn lại</param>
        /// <param name="supplierId">ID nhà cung cấp</param>
        /// <returns>Tuple chứa prefix và message</returns>
        public (string prefix, string message, int amount) GetPrefixAndMessage(string phoneNumber, double amount, double amountLeft, Guid supplierId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand("SELECT * FROM get_prefix_and_message(@p_phone_number, @p_amount, @p_amount_left, @p_supplier_id)", connection))
                    {
                        command.Parameters.AddWithValue("p_phone_number", NpgsqlDbType.Varchar, phoneNumber);
                        command.Parameters.AddWithValue("p_amount", NpgsqlDbType.Double, amount);
                        command.Parameters.AddWithValue("p_amount_left", NpgsqlDbType.Double, amountLeft);
                        command.Parameters.AddWithValue("p_supplier_id", NpgsqlDbType.Uuid, supplierId);
                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                var prefix = reader["prefix"].ToString();
                                var message = reader["message"].ToString();
                                var amountValue = int.Parse(reader["amount"].ToString());

                                return (prefix, message, amountValue);
                            }
                            else
                                return (string.Empty, string.Empty, 0);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Lỗi khi chạy GetPrefixAndMessage: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Thêm dữ liệu vào bảng history_sms
        /// </summary>
        /// <param name="iccid">ICCID của sim</param>
        /// <param name="phoneNumber">Số điện thoại</param>
        /// <param name="prefix">Đầu số</param>
        /// <param name="message">Nội dung tin nhắn</param>
        /// <param name="amount">Số tiền</param>
        /// <param name="supplierId">ID nhà cung cấp</param>
        /// <returns>ID bản ghi đã thêm</returns>
        public Guid InsertSMSHistory(string iccid, string phoneNumber, string prefix, string message, double amount, Guid supplierId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // Sử dụng function insert_sms_history
                    using (var command = new NpgsqlCommand("SELECT insert_sms_history(@p_icci, @p_phone_number, @p_call_center_number, @p_message, @p_amount, @p_account_id)", connection))
                    {
                        command.Parameters.AddWithValue("p_icci", NpgsqlDbType.Varchar, iccid);
                        command.Parameters.AddWithValue("p_phone_number", NpgsqlDbType.Varchar, phoneNumber);
                        command.Parameters.AddWithValue("p_call_center_number", NpgsqlDbType.Varchar, prefix);
                        command.Parameters.AddWithValue("p_message", NpgsqlDbType.Varchar, message);
                        command.Parameters.AddWithValue("p_amount", NpgsqlDbType.Double, amount);
                        command.Parameters.AddWithValue("p_account_id", NpgsqlDbType.Uuid, supplierId);
                        // Lấy ID vừa thêm
                        Guid smsId = (Guid)command.ExecuteScalar();
                        return smsId;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Lỗi khi chạy InsertSMSHistory: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Cập nhật trạng thái của bản ghi trong history_sms
        /// </summary>
        /// <param name="smsId">ID của bản ghi cần cập nhật</param>
        /// <param name="status">Trạng thái mới (SUCCESS, FAILED, CANCELLED,...)</param>
        /// <param name="supplierId">ID nhà cung cấp</param>
        /// <returns>True nếu cập nhật thành công, ngược lại False</returns>
        public bool UpdateSMSStatus(Guid smsId, string status, Guid supplierId)
        {
            try
            {
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();

                    // Sử dụng function update_sms_history
                    using (var command = new NpgsqlCommand("SELECT update_sms_history(@p_id, @p_status, @p_account_id)", connection))
                    {
                        command.Parameters.AddWithValue("p_id", NpgsqlDbType.Uuid, smsId);
                        command.Parameters.AddWithValue("p_status", NpgsqlDbType.Varchar, status);
                        command.Parameters.AddWithValue("p_account_id", NpgsqlDbType.Uuid, supplierId);
                        bool result = (bool)command.ExecuteScalar();
                        return result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Lỗi khi chạy UpdateSMSStatus: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Lấy dữ liệu báo cáo lịch sử SMS từ database
        /// </summary>
        /// <param name="fromDate">Ngày bắt đầu</param>
        /// <param name="toDate">Ngày kết thúc</param>
        /// <param name="userId">ID tài khoản người dùng</param>
        /// <returns>DataTable chứa dữ liệu báo cáo</returns>
        public DataTable GetReportHistorySMS(DateTime fromDate, DateTime toDate, Guid userId)
        {
            try
            {
                DateTime utcFromDate = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, DateTimeKind.Local).ToUniversalTime();
                DateTime utcToDate = new DateTime(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59, DateTimeKind.Local).ToUniversalTime();
                using (var connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    DataTable result = new DataTable();
                    using (var command = new NpgsqlCommand("SELECT * FROM get_report_history_sms(@p_from_date, @p_to_date, @p_user_account_id)", connection))
                    {
                        command.Parameters.AddWithValue("p_from_date", NpgsqlDbType.TimestampTz, utcFromDate);
                        command.Parameters.AddWithValue("p_to_date", NpgsqlDbType.TimestampTz, utcToDate);
                        command.Parameters.AddWithValue("p_user_account_id", NpgsqlDbType.Uuid, userId);
                        using (var adapter = new NpgsqlDataAdapter(command))
                        {
                            adapter.Fill(result);
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Lỗi khi chạy GetReportHistorySMS: {ex.Message}");
                throw;
            }
        }
    }
}