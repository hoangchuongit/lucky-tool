using ExcelDataReader;
using LuckOTP.Utils;
using System;
using System.Data;
using System.Data.SQLite;
using System.IO;

namespace LuckOTP.Model
{
    public class DBController
    {
        private readonly string _connectionString;
        private readonly string _dbPath;

        public DBController(string dbPath = "data.db")
        {
            _dbPath = dbPath;
            _connectionString = $"Data Source={_dbPath};Version=3;";

            // Đảm bảo thư mục chứa file DB tồn tại
            string directory = Path.GetDirectoryName(Path.GetFullPath(_dbPath));
            if (!Directory.Exists(directory) && !string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // Tạo DB nếu chưa tồn tại
            if (!File.Exists(_dbPath))
                CreateDatabase();
        }

        /// <summary>
        /// Tạo cơ sở dữ liệu mới nếu chưa tồn tại
        /// </summary>
        private void CreateDatabase()
        {
            SQLiteConnection.CreateFile(_dbPath);

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string createTableQuery = @"
                    CREATE TABLE IF NOT EXISTS PhoneData (
                        Phone TEXT PRIMARY KEY NOT NULL,
                        Status TEXT,
                        CreatedAt DATETIME DEFAULT CURRENT_TIMESTAMP,
                        UpdatedAt DATETIME DEFAULT CURRENT_TIMESTAMP
                    );

                    CREATE INDEX IF NOT EXISTS idx_phone ON PhoneData(Phone);
                ";

                using (var command = new SQLiteCommand(createTableQuery, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

        /// <summary>
        /// Import dữ liệu từ file Excel (2 cột: Phone và Status)
        /// </summary>
        /// <param name="excelFilePath">Đường dẫn đến file Excel</param>
        /// <returns>Số lượng bản ghi đã được thêm/cập nhật</returns>
        public int ImportFromExcel(string excelFilePath)
        {
            if (!File.Exists(excelFilePath))
                throw new FileNotFoundException("Không tìm thấy file Excel", excelFilePath);

            int recordCount = 0;

            // Lưu ý: Để sử dụng ExcelDataReader trên .NET Framework 4.8,
            // bạn cần cài đặt packages ExcelDataReader và ExcelDataReader.DataSet
            using (var stream = File.Open(excelFilePath, FileMode.Open, FileAccess.Read))
            {
                IExcelDataReader reader;

                // Xác định loại file Excel (xls hoặc xlsx)
                if (Path.GetExtension(excelFilePath).ToLower() == ".xls")
                    reader = ExcelReaderFactory.CreateBinaryReader(stream);
                else
                    reader = ExcelReaderFactory.CreateOpenXmlReader(stream);

                using (reader)
                {
                    var dataSet = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = false
                        }
                    });

                    // Lấy bảng đầu tiên
                    DataTable table = dataSet.Tables[0];

                    using (var connection = new SQLiteConnection(_connectionString))
                    {
                        connection.Open();
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                string insertQuery = @"
                                    INSERT INTO PhoneData (Phone, Status)
                                    VALUES (@Phone, @Status)
                                    ON CONFLICT(Phone) DO UPDATE SET
                                        Status = @Status,
                                        UpdatedAt = CURRENT_TIMESTAMP
                                ";
                                using (var command = new SQLiteCommand(insertQuery, connection))
                                {
                                    // Thêm các tham số
                                    command.Parameters.Add("@Phone", DbType.String);
                                    command.Parameters.Add("@Status", DbType.String);
                                    // Duyệt qua từng dòng trong Excel
                                    foreach (DataRow row in table.Rows)
                                    {
                                        string phone = row[0]?.ToString();
                                        string status = row[1]?.ToString();
                                        if (!string.IsNullOrEmpty(phone) && (phone.StartsWith("84") || phone.StartsWith("0")))
                                        {
                                            command.Parameters["@Phone"].Value = Common.NormalizePhone(phone);
                                            command.Parameters["@Status"].Value = status;
                                            command.ExecuteNonQuery();
                                            recordCount++;
                                        }
                                    }
                                }
                                transaction.Commit();
                            }
                            catch (Exception)
                            {
                                transaction.Rollback();
                                throw;
                            }
                        }
                    }
                }
            }
            return recordCount;
        }

        /// <summary>
        /// Xóa trắng toàn bộ dữ liệu trong database
        /// </summary>
        /// <returns>Số lượng bản ghi đã xóa</returns>
        public int ClearDatabase()
        {
            int recordsDeleted = 0;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string deleteQuery = "DELETE FROM PhoneData";

                using (var command = new SQLiteCommand(deleteQuery, connection))
                {
                    recordsDeleted = command.ExecuteNonQuery();
                }
            }

            return recordsDeleted;
        }

        /// <summary>
        /// Tìm kiếm status dựa theo số điện thoại
        /// </summary>
        /// <param name="phone">Số điện thoại cần tìm</param>
        /// <returns>Status tương ứng hoặc null nếu không tìm thấy</returns>
        public string FindStatusByPhone(string phone)
        {
            string status = null;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Status FROM PhoneData WHERE Phone = @Phone LIMIT 1";

                using (var command = new SQLiteCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Phone", phone);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            status = reader["Status"]?.ToString();
                        }
                    }
                }
            }

            return status;
        }

        /// <summary>
        /// Đếm tổng số bản ghi trong database
        /// </summary>
        /// <returns>Số lượng bản ghi</returns>
        public int CountRecords()
        {
            int count = 0;

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT COUNT(*) FROM PhoneData";

                using (var command = new SQLiteCommand(query, connection))
                {
                    count = Convert.ToInt32(command.ExecuteScalar());
                }
            }

            return count;
        }

        /// <summary>
        /// Lấy toàn bộ dữ liệu từ database
        /// </summary>
        /// <returns>DataTable chứa dữ liệu</returns>
        public DataTable GetAllData()
        {
            DataTable dataTable = new DataTable();

            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                string query = "SELECT Phone, Status, CreatedAt, UpdatedAt FROM PhoneData";

                using (var adapter = new SQLiteDataAdapter(query, connection))
                {
                    adapter.Fill(dataTable);
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Thêm một số điện thoại mới với status
        /// </summary>
        /// <param name="phone">Số điện thoại</param>
        /// <param name="status">Trạng thái</param>
        /// <returns>true nếu thành công, false nếu thất bại</returns>
        public bool AddPhone(string phone, string status)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string insertQuery = @"
                        INSERT INTO PhoneData (Phone, Status)
                        VALUES (@Phone, @Status)
                        ON CONFLICT(Phone) DO UPDATE SET
                            Status = @Status,
                            UpdatedAt = CURRENT_TIMESTAMP
                    ";

                    using (var command = new SQLiteCommand(insertQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@Status", status);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Cập nhật status cho số điện thoại
        /// </summary>
        /// <param name="phone">Số điện thoại</param>
        /// <param name="status">Trạng thái mới</param>
        /// <returns>true nếu thành công, false nếu thất bại</returns>
        public bool UpdateStatus(string phone, string status)
        {
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();

                    string updateQuery = @"
                        UPDATE PhoneData
                        SET Status = @Status, UpdatedAt = CURRENT_TIMESTAMP
                        WHERE Phone = @Phone
                    ";

                    using (var command = new SQLiteCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("@Phone", phone);
                        command.Parameters.AddWithValue("@Status", status);

                        int rowsAffected = command.ExecuteNonQuery();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}