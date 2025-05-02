using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncCommon;
using Microsoft.Extensions.Logging;

namespace SyncLibrary
{
    public interface IDataSyncProcessor
    {
        Task ProcessLogsAsync();
    }

    public abstract class BaseDataSyncProcessor : IDataSyncProcessor
    {
        protected static XmlToSQLScript xmlToSQLScript = new XmlToSQLScript();
        protected readonly string proxyConnectionString;
        protected string localConnectionString;
        private readonly SyncTaskJob _syncTaskJob;
        DbConnectionInfoProvider dbConnectionInfoProvider;

        protected int MaxRetryAttempts = 3;
        protected int RetryDelayMilliseconds = 2000;
        protected readonly SqlLogger _logger;

        public BaseDataSyncProcessor(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfo, SyncTaskJob syncTaskJob)
        {
            _logger = logger;
            dbConnectionInfoProvider = dbConnectionInfo;
            _syncTaskJob = syncTaskJob;
            this.proxyConnectionString = dbConnectionInfoProvider.ProxyServer();
            this.localConnectionString = dbConnectionInfoProvider.LocalServer();
        }

        protected async Task ExecuteQueryWithRetriesAsync(SqlConnection connection, string queryText, SqlTransaction transaction)
        {
            int retryCount = 0;

            while (retryCount < MaxRetryAttempts)
            {
                try
                {
                    using (SqlCommand command = new SqlCommand(queryText, connection, transaction))
                    {
                        await command.ExecuteNonQueryAsync();
                    }

                    return;
                }
                catch (Exception ex)
                {
                    retryCount++;

                    if (retryCount >= MaxRetryAttempts)
                    {
                        Console.WriteLine($"Base-ExecuteQueryWithRetriesAsync 쿼리 실행 실패: {ex.Message}");
                        throw new Exception($"쿼리 실행 실패: {ex.Message}");
                    }                    
                    _logger.LogError($"SQL 오류 발생: {ex.Message}", ex.ToString());
                    Console.WriteLine($"재시도... 시도 횟수 {retryCount}");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
        }

        protected void ProcessSuccess(string queryText, int logId, List<int> processedLogIds, Action<string> onSqlExecuted)
        {
            onSqlExecuted?.Invoke(queryText);
            _logger.LogInformation($"LogID-{logId} 처리 완료: {DateTime.Now}");

            processedLogIds.Add(logId);
        }

        protected string GenerateQueryText(string changeType, string changeDetails, string tableName, List<string> primaryKeys, Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes)
        {
            if (changeType.ToUpper() == "I")
            {
                return xmlToSQLScript.GenerateInsertSql(changeDetails, tableName, fieldTypes);
            }
            else if (changeType.ToUpper() == "U")
            {
                return xmlToSQLScript.GenerateUpdateSql(changeDetails, tableName, primaryKeys, fieldTypes);
            }
            return string.Empty;
        }

        protected (Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)>, List<string>) GetFieldTypesAndPrimaryKeyFromDatabase(string tableName, string connstr)
        {
            var fieldTypes = new Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)>();
            List<string> primaryKeys = new List<string>();

            using (SqlConnection connection = new SqlConnection(connstr))
            {
                connection.Open();

                string columnQuery = "SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, NUMERIC_PRECISION, NUMERIC_SCALE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
                using (SqlCommand columnCommand = new SqlCommand(columnQuery, connection))
                {
                    columnCommand.Parameters.AddWithValue("@TableName", tableName);

                    using (SqlDataReader reader = columnCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            int? maxLength = reader.IsDBNull(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"))
                                ? (int?)null
                                : reader.GetInt32(reader.GetOrdinal("CHARACTER_MAXIMUM_LENGTH"));
                            int? precision = reader.IsDBNull(reader.GetOrdinal("NUMERIC_PRECISION"))
                                            ? (int?)null
                                            : reader.GetByte(reader.GetOrdinal("NUMERIC_PRECISION"));
                            int? scale = reader.IsDBNull(reader.GetOrdinal("NUMERIC_SCALE"))
                                        ? (int?)null
                                        : reader.GetInt32(reader.GetOrdinal("NUMERIC_SCALE"));

                            fieldTypes[columnName] = (dataType, maxLength, precision, scale);
                        }
                    }
                }

                if (fieldTypes.Count == 0)
                {
                    throw new InvalidOperationException($"테이블 '{tableName}'에 대한 열 정보를 찾을 수 없습니다.");
                }

                string pkQuery = @"
                    SELECT column_name
                    FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS TC
                    JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE KU
                    ON TC.CONSTRAINT_NAME = KU.CONSTRAINT_NAME
                    WHERE TC.TABLE_NAME = @TableName AND TC.CONSTRAINT_TYPE = 'PRIMARY KEY'";

                using (SqlCommand pkCommand = new SqlCommand(pkQuery, connection))
                {
                    pkCommand.Parameters.AddWithValue("@TableName", tableName);

                    using (SqlDataReader reader = pkCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            primaryKeys.Add(reader["COLUMN_NAME"].ToString());
                        }

                        if (primaryKeys.Count == 0)
                        {
                            Console.WriteLine($"테이블 '{tableName}'에 기본 키가 존재하지 않습니다.");
                            throw new InvalidOperationException($"테이블 '{tableName}'에 기본 키가 존재하지 않습니다.");
                        }
                    }
                }
            }

            return (fieldTypes, primaryKeys);
        }


        protected bool IsQuerySafe(string queryText)
        {
            string lowerQuery = queryText.ToLower();
            return !( lowerQuery.Contains("delete *"));
        }

        protected void UpdateStatus(string message)
        {
            _logger.LogInformation(message);
        }

        protected void LogOperation(string message, string sqlQuery = null)
        {
            _logger.LogInformation(message, sqlQuery);
        }

        protected void LogError(string message, string sqlQuery = null)
        {
            _logger.LogError(message, sqlQuery);
        }

        protected DataTable LoadLogs(int batchSize)
        {
            DataTable logData = new DataTable();
            try
            {

                using (SqlConnection connection = new SqlConnection(localConnectionString))
                {
                    connection.Open();

                    string referenceTablesCondition = string.Join(",", _syncTaskJob.ReferenceTables.Select(table => $"'{table}'"));

                    string query = $@"
                                    SELECT TOP (@BatchSize) LogId, TableName, ChangeType, ChangeDetails 
                                    FROM WMSDataSync_ChangeLog with(nolock)
                                    WHERE Processed = 0 
                                    AND TableName IN ({referenceTablesCondition}) Order by logid";

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {

                        command.Parameters.AddWithValue("@BatchSize", batchSize);

                        using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                        {
                            adapter.Fill(logData);
                        }
                    }
                }

                return logData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WMSDataSync_ChangeLog Read ERROR: {ex.Message}");
                throw new Exception($"WMSDataSync_ChangeLog Read ERROR: {ex.Message}");
            }
        }

        public bool MarkLogsAsProcessed(List<int> logIds, string connectionString)
        {
            if (logIds == null || logIds.Count == 0)
            {
                throw new Exception("No log IDs provided to update.");
            }

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    connection.Open();

                    string query = "UPDATE WMSDataSync_ChangeLog SET Processed = 1 WHERE LogId IN (" + string.Join(",", logIds) + ")";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.ExecuteNonQuery();
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Base-MarkLogsAsProcessed: {ex.Message}");
                throw new Exception($"Failed to update log status: {ex.Message}");
            }
        }

        protected bool IsConnectionActive(SqlConnection connection)
        {
            try
            {
                connection.Open();
                connection.Close();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public abstract Task ProcessLogsAsync();
    }
}
