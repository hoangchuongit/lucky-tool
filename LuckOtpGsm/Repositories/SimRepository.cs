using LuckOTP.Model;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Collections.Generic;

namespace LuckOTP.Repositories
{
    public class SimRepository
    {
        private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Mỗi lần tháo/lắp sim sẽ gọi service này
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="simDisable"></param>
        /// <returns></returns>
        public bool UpsertSim(Sim obj, bool simDisable, string country, Dictionary<string, int> services)
        {
            logger.Info($"UpsertSim data: {obj.phone_number} - {obj.iccid} - {obj.supplier_id} - {simDisable} - {country}");
            string upsertQuery = @"SELECT upsert_sim(@phone_number, @iccid, @supplier_id, @sim_disable, @country, @services);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(upsertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@phone_number", obj.phone_number);
                        command.Parameters.AddWithValue("@iccid", obj.iccid);
                        command.Parameters.AddWithValue("@supplier_id", obj.supplier_id);
                        command.Parameters.AddWithValue("@sim_disable", NpgsqlTypes.NpgsqlDbType.Boolean, simDisable);
                        command.Parameters.AddWithValue("@country", NpgsqlTypes.NpgsqlDbType.Varchar, country);
                        string servicesJson = JsonConvert.SerializeObject(services);
                        command.Parameters.AddWithValue("@services", NpgsqlTypes.NpgsqlDbType.Jsonb, servicesJson);
                        var result = command.ExecuteScalar();
                        logger.Info($"result data: {obj.phone_number} - {result}");
                        return (bool)result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"UpsertSim An error occurred: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Mỗi 2 phút gửi thông tin xác nhận SIM vẫn đang cắm. Nếu quá 2 phút hệ thống sẽ tự động xác nhận rút sim đó
        /// </summary>
        /// <param name="phone_number"></param>
        /// <returns></returns>
        public bool UpsertTimeSimInsert(string phone_numbers, string account_id)
        {
            //return true;
            var query = "SELECT upsert_time_sim_insert(@phone_numbers, @account_id);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@phone_numbers", phone_numbers);
                        command.Parameters.AddWithValue("@account_id", Guid.Parse(account_id));

                        // Kiểm tra kết quả trả về từ function
                        var result = command.ExecuteScalar();
                        return (bool)result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"An error occurred: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Xử lý khi nhận được OTP từ nhà mạng
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="simDisable"></param>
        /// <returns></returns>
        public string UpdateOtpTransaction(string phone_number, string supplier_id, string message, string otp)
        {
            var query = "SELECT update_otp_transaction_voice(@phone_number, @supplier_id, @message, @otp);";
            logger.Error($"phone_number: {phone_number} - supplier_id: {supplier_id} - message: {message} - otp: {otp}");
            logger.Error($"SELECT update_otp_transaction_voice({phone_number}, {supplier_id}, {message}, {otp});");
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@phone_number", NpgsqlTypes.NpgsqlDbType.Text, phone_number);
                        command.Parameters.AddWithValue("@supplier_id", Guid.Parse(supplier_id));
                        command.Parameters.AddWithValue("@message", NpgsqlTypes.NpgsqlDbType.Text, message);
                        command.Parameters.AddWithValue("@otp", NpgsqlTypes.NpgsqlDbType.Text, otp);

                        // Kiểm tra kết quả trả về từ function
                        var result = command.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateOtpTransaction An error occurred: {ex.Message}");
                return null;
            }
        }

        public string UpdateOtpTransaction(string phone_number, string supplier_id, string brandName, string message, string otp)
        {
            var query = "SELECT update_otp_transaction(@phone_number, @supplier_id, @brand_name, @message, @otp);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@phone_number", NpgsqlTypes.NpgsqlDbType.Text, phone_number);
                        command.Parameters.AddWithValue("@supplier_id", Guid.Parse(supplier_id));
                        command.Parameters.AddWithValue("@brand_name", NpgsqlTypes.NpgsqlDbType.Text, brandName);
                        command.Parameters.AddWithValue("@message", NpgsqlTypes.NpgsqlDbType.Text, message);
                        command.Parameters.AddWithValue("@otp", NpgsqlTypes.NpgsqlDbType.Text, otp);

                        // Kiểm tra kết quả trả về từ function
                        var result = command.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"UpdateOtpTransaction An error occurred: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Kiểm tra số phone đang dùng dịch vụ nào
        /// </summary>
        /// <param name="phone_number"></param>
        /// <returns></returns>
        public string CheckServiceOfPhone(string phone_number, string account_id)
        {
            var query = "SELECT current_service_code FROM check_sim_service_code(@phone_number, @account_id);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@phone_number", NpgsqlTypes.NpgsqlDbType.Text, phone_number);
                        command.Parameters.AddWithValue("@account_id", Guid.Parse(account_id));
                        var result = command.ExecuteScalar();
                        return result?.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"CheckServiceOfPhone An error occurred: {ex.Message}");
                return null;
            }
        }

        public List<GsmReportDto> GsmReport(string account_id, DateTime from, DateTime to)
        {
            //DateTime fromDate = from.Date.ToUniversalTime(); // Lấy ngày từ 'from' và đặt thời gian là 00:00:00
            //DateTime toDate = to.Date.AddDays(1).AddTicks(-1).ToUniversalTime(); // Lấy ngày 'to' và đặt thời gian là 23:59:59.9999999

            DateTime fromDate = DateTime.SpecifyKind(from, DateTimeKind.Local).ToUniversalTime();
            DateTime toDate = DateTime.SpecifyKind(to, DateTimeKind.Local).ToUniversalTime();

            var query = "SELECT * FROM gsm_report(@p_supplier_id, @p_from_date, @p_to_date);";
            List<GsmReportDto> lists = new List<GsmReportDto>();
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@p_supplier_id", Guid.Parse(account_id));
                        command.Parameters.AddWithValue("@p_from_date", fromDate);
                        command.Parameters.AddWithValue("@p_to_date", toDate);
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var list = new GsmReportDto
                                {
                                    stt = Int64.Parse(reader["stt"].ToString()),
                                    created_date = DateTime.Parse(reader["created_date"].ToString()),
                                    code = reader["code"].ToString(),
                                    name = reader["name"].ToString(),
                                    total = Int64.Parse(reader["total"].ToString()),
                                    amount = Decimal.Parse(reader["amount"].ToString()),
                                    total_amount = Decimal.Parse(reader["total_amount"].ToString()),
                                };
                                lists.Add(list);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"GsmReport An error occurred: {ex.Message}");
            }
            return lists;
        }

        public List<GsmReportICCIDDto> GsmReportICCD(string iccd, string account_id)
        {
            var query = "SELECT * FROM gsm_report_by_iccid(@p_iccd, @p_supplier_id);";
            List<GsmReportICCIDDto> lists = new List<GsmReportICCIDDto>();
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@p_iccd", NpgsqlTypes.NpgsqlDbType.Text, iccd);
                        command.Parameters.AddWithValue("@p_supplier_id", Guid.Parse(account_id));
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var list = new GsmReportICCIDDto
                                {
                                    code = reader["code"].ToString(),
                                    name = reader["name"].ToString(),
                                    total = Int64.Parse(reader["total"].ToString()),
                                };
                                lists.Add(list);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"GsmReportICCD An error occurred: {ex.Message}");
            }
            return lists;
        }

        public string CheckTele(string phone_number, string account_id)
        {
            var query = "SELECT status FROM gsm_status_tele(@phone_number, @p_supplier_id);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.Add(new NpgsqlParameter("@phone_number", NpgsqlTypes.NpgsqlDbType.Text) { Value = phone_number });
                        command.Parameters.AddWithValue("@p_supplier_id", Guid.Parse(account_id));
                        var result = command.ExecuteScalar();
                        return result != null ? result.ToString() : "";
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"CheckTele An error occurred: {ex.Message}");
                return "";
            }
        }

        public bool GsmResetLo(string account_id)
        {
            var query = "SELECT * FROM gsm_reset_lo(@p_supplier_id);";
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@p_supplier_id", Guid.Parse(account_id));
                        var result = command.ExecuteScalar();
                        return (bool)result;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"ResetLo An error occurred: {ex.Message}");
                return false;
            }
        }
    }
}