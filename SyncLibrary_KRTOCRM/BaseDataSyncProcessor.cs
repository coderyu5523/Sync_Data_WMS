using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncCommon;
namespace SyncLibrary
{
    public interface IDataSyncProcessor
    {
        Task ProcessLogsAsync();
        //void SetConnectionStrings(string proxyConnectionString);
    }
    public abstract class BaseDataSyncProcessor : IDataSyncProcessor
    {
        // XmlToSQLScript 인스턴스 추가
        protected static XmlToSQLScript xmlToSQLScript = new XmlToSQLScript();
        protected readonly string proxyConnectionString;
        protected string localConnectionString;
        protected string remoteConnectionString;

        DbConnectionInfoProvider dbConnectionInfoProvider;

        protected int MaxRetryAttempts = 3;
        protected int RetryDelayMilliseconds = 2000;
        protected readonly Logger _logger;


        public BaseDataSyncProcessor(Logger logger, DbConnectionInfoProvider dbConnectionInfo)
        {
            _logger = logger;
            dbConnectionInfoProvider = dbConnectionInfo;
            //proxyConnectionString = conntionstring;
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
                        // 오류 기록
                        _logger.LogError($"쿼리 실행 실패: {ex.Message}", queryText);
                        throw new Exception($"쿼리 실행 실패: {ex.Message}");
                    }

                    Console.WriteLine($"재시도... 시도 횟수 {retryCount}");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
        }

        // 공통 작업 완료 처리 메서드
        protected void ProcessSuccess(string queryText, int logId, List<int> processedLogIds, Action<string> onSqlExecuted)
        {
            onSqlExecuted?.Invoke(queryText);
            _logger.LogOperation("Batch processed successfully.", queryText);
            Console.WriteLine("LogID-" + logId.ToString() + DateTime.Now.ToString());

            // 로그가 성공적으로 처리된 경우 processedLogIds에 추가
            processedLogIds.Add(logId);
        }

        protected string GenerateQueryText(string changeType, string changeDetails, string tableName, List<string> primaryKeys, Dictionary<string, string> fieldTypes)
        {
            if (changeType.ToUpper() == "INSERT")
            {
                return xmlToSQLScript.GenerateInsertSql(changeDetails, tableName, fieldTypes);
            }
            else if (changeType.ToUpper() == "UPDATE")
            {
                return xmlToSQLScript.GenerateUpdateSql(changeDetails, tableName, primaryKeys, fieldTypes);
            }
            return string.Empty;
        }

        protected (Dictionary<string, string>, List<string>) GetFieldTypesAndPrimaryKeyFromDatabase(string tableName, string connstr)
        {
            Dictionary<string, string> fieldTypes = new Dictionary<string, string>();
            List<string> primaryKeys = new List<string>();

            using (SqlConnection connection = new SqlConnection(connstr))
            {
                connection.Open();

                string columnQuery = "SELECT COLUMN_NAME, DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @TableName";
                using (SqlCommand columnCommand = new SqlCommand(columnQuery, connection))
                {
                    columnCommand.Parameters.AddWithValue("@TableName", tableName);

                    using (SqlDataReader reader = columnCommand.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            fieldTypes[columnName] = dataType;
                        }
                    }
                }
                // 필드 타입이 없을 경우 예외를 발생시킵니다.
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

                        if (reader.Read())
                        {
                            primaryKeys.Add(reader["COLUMN_NAME"].ToString());
                        }
                        if (primaryKeys.Count == 0)
                        {
                            // 기본 키가 없을 경우 예외를 발생시킵니다.
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
            return !(lowerQuery.Contains("drop") || lowerQuery.Contains("delete *"));
        }


        protected void UpdateStatus(string message)
        {
            // 공통 상태 업데이트 로직
        }

        protected void LogOperation(string message, string sqlQuery = null)
        {
            _logger.LogOperation(message, sqlQuery);
        }

        protected void LogError(string message, string sqlQuery = null)
        {
            _logger.LogError(message, sqlQuery);
        }

        //protected async Task<bool> ExecuteWithTransactionAsync(Func<SqlTransaction, Task> operation)
        //{
        //    using (SqlConnection connection = new SqlConnection(remoteConnectionString))
        //    {
        //        await connection.OpenAsync();
        //        using (SqlTransaction transaction = connection.BeginTransaction())
        //        {
        //            try
        //            {
        //                await operation(transaction);
        //                transaction.Commit();
        //                UpdateStatus("Transaction completed successfully.");
        //                return true;
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                LogError($"Transaction error: {ex.Message}");
        //                UpdateStatus($"Error: {ex.Message}");
        //                return false;
        //            }
        //        }
        //    }
        //}
        // 공통 메서드: 로그를 로드하는 메서드
        protected DataTable LoadLogs(int batchSize)
        {
            DataTable logData = new DataTable();

            using (SqlConnection connection = new SqlConnection(localConnectionString))
            {
                connection.Open();
                string query = "SELECT TOP (@BatchSize) LogId, TableName, ChangeType, ChangeDetails,Srt_Svr,Des_Svr FROM WMSDataSync_ChangeLog WHERE Processed = 0";

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
                return true; // 성공적으로 실행된 경우 true 반환
            }
            catch (Exception ex)
            {
                // 상태 업데이트 실패 시 예외를 발생시켜 트랜잭션이 롤백되도록 합니다.
                throw new Exception($"Failed to update log status: {ex.Message}");
            }
        }

        //public async Task MarkLogsAsProcessed(List<int> logIds, SqlConnection connection, SqlTransaction transaction)
        //{
        //    if (logIds == null || logIds.Count == 0)
        //    {
        //        throw new Exception("No log IDs provided to update.");
        //    }

        //    try
        //    {

        //        //using (SqlConnection connection = new SqlConnection(ConnectionString))
        //        //{
        //        //    connection.Open();

        //        string query = "UPDATE WMSDataSync_ChangeLog SET Processed = 1 WHERE LogId IN (" + string.Join(",", logIds) + ")";
        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            command.ExecuteNonQuery();
        //        }
        //        //}

        //        //string query = "UPDATE WMSDataSync_ChangeLog SET Processed = 1 WHERE LogId IN (" + string.Join(",", logIds) + ")";
        //        //using (SqlCommand command = new SqlCommand(query, connection, transaction))
        //        //{
        //        //    command.ExecuteNonQuery();
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        // 상태 업데이트 실패 시 예외를 발생시켜 트랜잭션이 롤백되도록 합니다.
        //        throw new Exception($"Failed to update log status: {ex.Message}");
        //    }
        //}

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


        // 공통 프로세스 로직
        public abstract Task ProcessLogsAsync();

    }

}
