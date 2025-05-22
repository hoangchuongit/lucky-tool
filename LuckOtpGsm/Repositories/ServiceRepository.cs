using LuckOTP.Model;
using Npgsql;
using System;
using System.Collections.Generic;

namespace LuckOTP.Repositories
{
    public class ServiceRepository
    {
        public List<Service> GetAllService()
        {
            var query = $"SELECT service.code, service.\"name\", service.status FROM service WHERE status = TRUE";
            List<Service> lists = new List<Service>();
            try
            {
                using (var connection = new NpgsqlConnection(Utils.Common.connectionString))
                {
                    connection.Open();
                    using (var command = new NpgsqlCommand(query, connection))
                    {
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var list = new Service
                                {
                                    code = reader["code"].ToString(),
                                    name = reader["name"].ToString(),
                                };
                                lists.Add(list);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            return lists;
        }
    }
}