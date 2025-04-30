//using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using SyncCommon;
namespace CRM_Data_Sync_Service
{
    public class DataSyncLogProcessor
    {
        public static Destination_Svr des_db = new Destination_Svr();
        public static Source_Svr src_db = new Source_Svr();
        public static XmlToSQLScript xmlToSQLScript = new XmlToSQLScript();

        private string localConnectionString = "";
        private string remoteConnectionString = "";

        private const int BatchSize = 10; // 배치로 처리할 로그 수
        private const int MaxRetryAttempts = 3; // 재시도 횟수
        private const int RetryDelayMilliseconds = 2000; // 재시도 대기 시간 (밀리초)
        //readonly
        private Logger _logger; // Logger 인스턴스 추가
        // 진행 상태 및 로그 업데이트를 위한 이벤트
        public event Action<string> StatusUpdated;
        public event Action<string> LogUpdated;

        //연결정보를 받는 경우에는 파라메터로 받는다
        public DataSyncLogProcessor()
        {
            //InitMsDb();            
            
        }
        public void ConnectionString(string  src,string dest)
        {
            localConnectionString = src;
            remoteConnectionString = dest;
            _logger = new Logger(localConnectionString); // Logger 인스턴스 생성
        }
        // Config 파일 DB정보 값으로 ERP, Local DB 접속 설정
        //private void InitMsDb()
        //{
        //    string src_dbIp = "192.168.10.152";
        //    string des_dbIp = "192.168.10.155";
        //    string dbId = "erp";
        //    string dbPw = "itsp@7735";

        //    localConnectionString = Setting(src_dbIp, dbId, dbPw, "smart_db_ir", "1616");
        //    remoteConnectionString = Setting(des_dbIp, dbId, dbPw, "smart_db", "1616");
        //}

        //public string Setting(string ip = "localhost", string id = "sa", string password = "1234", string dbName = "dio_implant", string port = "1433")
        //{
        //    string dbConn = "SERVER=" + ip + "," + port + ";" +
        //                    "DATABASE=" + dbName + ";" +
        //                    "UID=" + id + ";" +
        //                    "PWD=" + password + ";" +
        //                    "Connection Timeout=10";
        //    return dbConn;
        //}

        public async Task ProcessLogsAsync()
        {
            List<int> processedLogIds = new List<int>();
            DataTable logData = LoadLogs(BatchSize);
            UpdateStatus($"Process Start - {logData.Rows.Count} 건 - {DateTime.Now}");
            if (logData.Rows.Count == 0)
            {
                //Console.WriteLine("No logs to process.");
                UpdateStatus("No logs to process.");
                return;
            }
            string currentSqlQuery = null; // 현재 처리 중인 SQL 문을 저장할 변수
            try
            {
                DateTime startTime = DateTime.Now; // 작업 시작 시간 기록
                bool isProcessed = await ApplyBatchToRemoteDatabaseAsync(logData, processedLogIds, (sql) => currentSqlQuery = sql); // SQL 문을 콜백을 통해 저장

                if (isProcessed)
                {
                    // 처리된 로그의 상태를 업데이트
                    MarkLogsAsProcessed(processedLogIds);

                    UpdateStatus("Batch processed successfully.");

                    // 작업 로그 기록
                    //_logger.LogOperation("Batch processed successfully.", currentSqlQuery);

                    DateTime endTime = DateTime.Now; // 작업 종료 시간 기록
                    TimeSpan processingTime = endTime - startTime;
                    string performanceMetrics = $"Processed {processedLogIds.Count} logs in {processingTime.TotalSeconds} seconds.";
                    UpdateLog(performanceMetrics);

                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing batch: {ex.Message}", currentSqlQuery);
                //Console.WriteLine($"Error processing batch: {ex.Message}");
                UpdateStatus($"Error: {ex.Message}");
            }
        }

        public async void Batch_DataGet()
        {
            await ProcessLogsAsync();
        }
        private void UpdateStatus(string message)
        {
            StatusUpdated?.Invoke(message);
        }

        private void UpdateLog(string message)
        {
            LogUpdated?.Invoke(message);
        }
        private DataTable LoadLogs(int batchSize)
        {
            DataTable logData = new DataTable();

            using (SqlConnection connection = new SqlConnection(localConnectionString))
            {
                connection.Open();
                string query = "SELECT TOP (@BatchSize) LogId, TableName, ChangeType, ChangeDetails FROM CRMDataSync_ChangeLog WHERE Processed = 0";

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

        private async Task<bool> ApplyBatchToRemoteDatabaseAsync(DataTable logData, List<int> processedLogIds, Action<string> onSqlExecuted)
        {
            using (SqlConnection connection = new SqlConnection(remoteConnectionString))
            {
                if (!IsConnectionActive(connection))
                {
                    UpdateStatus("Destination database connection is inactive. Retrying in next cycle.");
                    return false; // 연결이 비활성화된 경우
                }

                await connection.OpenAsync();

                using (SqlTransaction transaction = connection.BeginTransaction())
                {
                    try
                    {
                        string old_tableName = "";
                        Dictionary<string, string> fieldTypes = null;
                        string primaryKey = "";
                        foreach (DataRow row in logData.Rows)
                        {

                            string tableName = row["TableName"].ToString();
                            string changeType = row["ChangeType"].ToString();
                            string changeDetails = row["ChangeDetails"].ToString();
                            int logId = Convert.ToInt32(row["LogId"]); // 로그 ID 가져오기
                            if (tableName != old_tableName)
                            {
                                old_tableName = tableName;
                                (fieldTypes, primaryKey) = GetFieldTypesAndPrimaryKeyFromDatabase(tableName, localConnectionString);
                            }
                                                                                    
                            string queryText = GenerateQueryText(changeType, changeDetails, tableName, primaryKey, fieldTypes);


                            if (!IsQuerySafe(queryText))
                            {
                                throw new InvalidOperationException("Unsafe query detected");
                            }

                            await ExecuteQueryWithRetriesAsync(connection, queryText, transaction);

                            // SQL 문을 콜백을 통해 전달
                            onSqlExecuted?.Invoke(queryText);
                            _logger.LogOperation("Batch processed successfully.", queryText);
                            Console.WriteLine("LogID-"+ logId.ToString()+DateTime.Now.ToString());
                            // 로그가 성공적으로 처리된 경우 processedLogIds에 추가
                            processedLogIds.Add(logId);
                        }

                        transaction.Commit();
                        UpdateStatus("Batch processed successfully.");

                        // 작업 로그 기록


                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        //Console.WriteLine($"Transaction rolled back due to error: {ex.Message}");
                        _logger.LogError($"Transaction error: {ex.Message}", onSqlExecuted.Target.ToString());
                        return false;
                    }
                }
            }
        }

        private string GenerateQueryText(string changeType, string changeDetails, string tableName, string primaryKey, Dictionary<string, string> fieldTypes)
        {
            if (changeType.ToUpper() == "INSERT")
            {
                return xmlToSQLScript.GenerateInsertSql(changeDetails, tableName, fieldTypes);
            }
            else if (changeType.ToUpper() == "UPDATE")
            {
                return xmlToSQLScript.GenerateUpdateSql(changeDetails, tableName, primaryKey, fieldTypes);
            }
            return string.Empty;
        }

        private (Dictionary<string, string>, string) GetFieldTypesAndPrimaryKeyFromDatabase(string tableName, string connstr)
        {
            Dictionary<string, string> fieldTypes = new Dictionary<string, string>();
            string primaryKey = "";

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
                            primaryKey = reader["column_name"].ToString();
                        }
                    }
                }
            }

            return (fieldTypes, primaryKey);
        }

        private async Task ExecuteQueryWithRetriesAsync(SqlConnection connection, string queryText, SqlTransaction transaction)
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
                        // Logger 인스턴스를 사용하여 오류 로그 기록
                        _logger.LogError($"Failed to execute query after {MaxRetryAttempts} attempts: {ex.Message}", queryText);
                        throw new Exception($"Failed to execute query after {MaxRetryAttempts} attempts: {ex.Message}");
                    }

                    Console.WriteLine($"Retrying... Attempt {retryCount}");
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }
        }

        private bool IsConnectionActive(SqlConnection connection)
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

        private bool IsQuerySafe(string queryText)
        {
            string lowerQuery = queryText.ToLower();
            return !(lowerQuery.Contains("drop") || lowerQuery.Contains("delete *"));
        }

        private void MarkLogsAsProcessed(List<int> logIds)
        {
            using (SqlConnection connection = new SqlConnection(localConnectionString))
            {
                connection.Open();

                string query = "UPDATE CRMDataSync_ChangeLog SET Processed = 1 WHERE LogId IN (" + string.Join(",", logIds) + ")";
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
        }

    }
}
