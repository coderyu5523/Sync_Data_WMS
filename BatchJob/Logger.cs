using System;
using System.Data.SqlClient;

namespace DioImplant_batch
{
    public class Logger
    {
        private readonly string _connectionString;

        public Logger(string connectionString)
        {
            _connectionString = connectionString;
        }

        // 작업 로그를 기록하는 메서드
        public void LogOperation(string message, string sqlQuery = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    string logCommand = "INSERT INTO CRMDataSync_OperationLog (OperationDetails, OperationDate, SqlQuery) VALUES (@OperationDetails, GETDATE(), @SqlQuery)";
                    using (SqlCommand command = new SqlCommand(logCommand, connection))
                    {
                        command.Parameters.AddWithValue("@OperationDetails", message);
                        command.Parameters.AddWithValue("@SqlQuery", sqlQuery ?? (object)DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log operation: {ex.Message}");
                }
            }
        }

        // 오류 로그 기록 메서드
        public void LogError(string message, string sqlQuery = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    string logCommand = "INSERT INTO ErrorLog (ErrorMessage, ErrorDate,sqlQuery) VALUES (@ErrorMessage, GETDATE(),@sqlQuery)";
                    using (SqlCommand command = new SqlCommand(logCommand, connection))
                    {
                        command.Parameters.AddWithValue("@ErrorMessage", message);
                        command.Parameters.AddWithValue("@SqlQuery", sqlQuery ?? (object)DBNull.Value);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log error: {ex.Message}");
                }
            }
        }
    }
}
