using System;
using System.Data.SqlClient;

namespace SyncCommon
{
    public class DBConnectionManager
    {
        private readonly string _connectionString;

        // 생성자: 연결 문자열을 인자로 받아 설정
        public DBConnectionManager(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
                throw new ArgumentException("Connection string cannot be null or empty", nameof(connectionString));

            _connectionString = connectionString;
        }

        // 데이터베이스 연결을 가져오는 메서드
        public SqlConnection GetConnection()
        {
            try
            {
                SqlConnection connection = new SqlConnection(_connectionString);
                connection.Open();
                return connection;
            }
            catch (SqlException ex)
            {
                // 연결 실패 시 예외 처리
                Console.WriteLine($"Error opening database connection: {ex.Message}");
                throw;
            }
        }

        // 데이터베이스 연결을 닫는 메서드
        public void CloseConnection(SqlConnection connection)
        {
            if (connection != null && connection.State == System.Data.ConnectionState.Open)
            {
                try
                {
                    connection.Close();
                }
                catch (SqlException ex)
                {
                    // 연결 닫기 실패 시 예외 처리
                    Console.WriteLine($"Error closing database connection: {ex.Message}");
                }
            }
        }
    }
}
