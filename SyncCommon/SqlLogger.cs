using System;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace SyncCommon
{
    public class SqlLogger : ILogger
    {
        private readonly string _connectionString;

        public SqlLogger(DbConnectionInfoProvider dbConnectionInfo)
        {
            _connectionString = dbConnectionInfo.ProxyServer();
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel)
        {
            // 로그 레벨에 따라 기록할지 여부를 결정
            return logLevel >= LogLevel.Information;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
                return;

            var message = formatter(state, exception);

            // 로그 메시지를 SQL 서버에 저장
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    string logCommand = "INSERT INTO WMSDataSync_OperationLog (OperationDetails, OperationDate, LogLevel, SqlQuery) VALUES (@OperationDetails, GETDATE(), @LogLevel, @SqlQuery)";
                    using (SqlCommand command = new SqlCommand(logCommand, connection))
                    {
                        command.Parameters.AddWithValue("@OperationDetails", message);
                        command.Parameters.AddWithValue("@LogLevel", logLevel.ToString());
                        command.Parameters.AddWithValue("@SqlQuery", exception?.Message ?? (object)DBNull.Value);
 // 예외 메시지를 SQL에 저장
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to log operation to SQL: {ex.Message}");
                }
            }
        }
        // 오류 로그를 기록하는 메서드 추가
        public void LogError(string message, string sqlQuery = null)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                try
                {
                    connection.Open();
                    string logCommand = "INSERT INTO WMSDataSync_ErrorLog (ErrorMessage, ErrorDate, SqlQuery) VALUES (@ErrorMessage, GETDATE(), @SqlQuery)";
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

    public class SqlLoggerProvider : ILoggerProvider
    {
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;

        public SqlLoggerProvider(DbConnectionInfoProvider dbConnectionInfoProvider)
        {
            _dbConnectionInfoProvider = dbConnectionInfoProvider;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SqlLogger(_dbConnectionInfoProvider);
        }

        public void Dispose()
        {
            // 리소스 해제 처리
        }
    }
}
