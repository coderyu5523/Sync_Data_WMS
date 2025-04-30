//using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyncCommon;

namespace SyncLibrary
{

    public class DataSyncLogProcessor_Unidirection : BaseDataSyncProcessor, IDataSyncProcessor
    {

        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        List<int> processedLogIds = new List<int>(); // 클래스 필드로 선언
        private readonly SyncTaskJob _syncTaskJob;
        private const int BatchSize = 30000; // 배치로 처리할 로그 수
        //private const int MaxRetryAttempts = 3; // 재시도 횟수
        //private const int RetryDelayMilliseconds = 2000; // 재시도 대기 시간 (밀리초)
        //readonly
        private SqlLogger _logger; // Logger 인스턴스 추가
        // 진행 상태 및 로그 업데이트를 위한 이벤트
        public event Action<string> StatusUpdated;
        public event Action<string> LogUpdated;

        //연결정보를 받는 경우에는 파라메터로 받는다

        public DataSyncLogProcessor_Unidirection(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfoProvider, SyncTaskJob syncTaskJob) : base(logger, dbConnectionInfoProvider, syncTaskJob)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
            _syncTaskJob = syncTaskJob;
        }
        
        public override async Task ProcessLogsAsync()
        {

            DataTable logData = LoadLogs(BatchSize);
            UpdateStatus($"Process Start - {logData.Rows.Count} 건 - {DateTime.Now}");

            if (logData.Rows.Count == 0)
            {
                UpdateStatus("No logs to process.");
                return;
            }

            string currentSqlQuery = null;
            var tasks = new List<Task>();
            var rows = logData.AsEnumerable().ToList();
            try
            {
                // 국가별로 데이터를 그룹화
                //var groupedData = logData.AsEnumerable()                    
                //    .ToList();

                // 각 국가 그룹에 대해 비동기적으로 처리
                //foreach (var row in rows)
                //{
                    //string countryCode = group.Key;

                    //tasks.Add(Task.Run(async () =>
                    //{
                        
                        // 국가 코드에 따른 연결 정보 설정
                        var (localConnectionString, remoteConnectionString) = _dbConnectionInfoProvider.GetConnectionInfo(
                            _syncTaskJob.SourceDB,
                            _syncTaskJob.TargetDB);

                        //국가 코드에 따른 연결 정보 설정
                        //(string localConnectionString, string remoteConnectionString) = _connectionInfoProvider.GetConnectionInfo(
                        //    group.First()["src_nat_cd"].ToString(),
                        //    group.First()["des_nat_cd"].ToString()
                        //);

                        // 새로운 DataTable 생성
                        //DataTable newTable = logData.Clone(); // 구조 복사

                        //// 기존 행을 새로운 DataTable로 가져옴
                        //newTable.ImportRow(row);

                        // 비동기 작업 호출
                        bool isProcessed = await ApplyBatchToRemoteDatabaseAsync(logData, processedLogIds, (sql) => currentSqlQuery = sql, remoteConnectionString);


                        // 데이터 로직 처리
                        //bool isProcessed = await ApplyBatchToRemoteDatabaseAsync(new DataTable { Rows = { row } }, processedLogIds, (sql) => currentSqlQuery = sql);

                        if (isProcessed)
                        {
                            // 처리된 로그의 상태를 업데이트
                            MarkLogsAsProcessed(processedLogIds,localConnectionString);
                            //UpdateStatus($"Batch processed successfully for {row["src_nat_cd"].ToString()}.");
                        }
                        
                    //}));
                //}

                // 모든 작업이 완료될 때까지 대기
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                //_logger.LogError($"Error processing batch: {ex.Message}", currentSqlQuery);
                throw;
                //UpdateStatus($"Error: {ex.Message}");
            }
        }



        public async void Batch_DataGet()
        {
            await ProcessLogsAsync();
        }

        //protected void UpdateStatus(string message)
        //{
        //    _logger.LogInformation(message);  // 상태 업데이트를 로깅
        //}

        //protected void LogOperation(string message, string sqlQuery = null)
        //{
        //    _logger.LogInformation(message, sqlQuery);  // 정보성 로그
        //}

        //protected void LogError(string message, string sqlQuery = null)
        //{
        //    _logger.LogError(message, sqlQuery);
        //}

        //private void UpdateStatus(string message)
        //{
        //    StatusUpdated?.Invoke(message);
        //}

        //private void UpdateLog(string message)
        //{
        //    LogUpdated?.Invoke(message);
        //}
        //private DataTable LoadLogs(int batchSize)
        //{
        //    DataTable logData = new DataTable();

        //    using (SqlConnection connection = new SqlConnection(localConnectionString))
        //    {
        //        connection.Open();
        //        string query = "SELECT TOP (@BatchSize) LogId, TableName, ChangeType, ChangeDetails FROM WMSDataSync_ChangeLog WHERE Processed = 0";

        //        using (SqlCommand command = new SqlCommand(query, connection))
        //        {
        //            command.Parameters.AddWithValue("@BatchSize", batchSize);

        //            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
        //            {
        //                adapter.Fill(logData);
        //            }
        //        }
        //    }

        //    return logData;
        //}

        private async Task<bool> ApplyBatchToRemoteDatabaseAsync(DataTable logData, List<int> processedLogIds, Action<string> onSqlExecuted,string remoteConnectionString)
        {
            try
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
                        //Dictionary<string, string> fieldTypes = null;
                        Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes = null;
                        List<string> primaryKeys = new List<string>();
                        foreach (DataRow row in logData.Rows)
                        {

                            string tableName = row["TableName"].ToString();
                            string changeType = row["ChangeType"].ToString();
                            string changeDetails = row["ChangeDetails"].ToString();
                            int logId = Convert.ToInt32(row["LogId"]); // 로그 ID 가져오기
                            if (tableName != old_tableName)
                            {
                                old_tableName = tableName;
                                (fieldTypes, primaryKeys) = GetFieldTypesAndPrimaryKeyFromDatabase(tableName, localConnectionString);
                            }
                                //string targettable = _syncTaskJob.TargetTable;
                                string queryText =  GenerateQueryText(changeType, changeDetails, tableName, primaryKeys, fieldTypes);


                            if (!IsQuerySafe(queryText))
                            {
                                    Console.WriteLine("LogID-" + logId.ToString() + DateTime.Now.ToString() + "쿼리구문오류' - " + queryText);
                                    _logger.LogError($"SQL 오류 발생: {queryText}");
                                    throw new InvalidOperationException("Unsafe query detected");
                            }

                            await ExecuteQueryWithRetriesAsync(connection, queryText, transaction);

                            // SQL 문을 콜백을 통해 전달
                            onSqlExecuted?.Invoke(queryText);
                            //_logger.LogInformation("Batch processed successfully.", queryText);
                            Console.WriteLine("LogID-" + logId.ToString() + DateTime.Now.ToString()+"' - "+ queryText);
                            // 로그가 성공적으로 처리된 경우 processedLogIds에 추가
                            processedLogIds.Add(logId);
                        }

                        transaction.Commit();
                        //UpdateStatus("Batch processed successfully.");

                        // 작업 로그 기록


                        return true;
                    }
                    catch (SqlException sqlEx)
                    {
                            _logger.LogError($"SQL 오류 발생: {sqlEx.Message}", sqlEx.ToString());
                            transaction.Rollback();
                        
                        //UpdateStatus($"SQL 오류: {sqlEx.Message}");
                        throw;
                        //return false;
                    }
                    catch (InvalidOperationException invEx)
                    {
                        transaction.Rollback();
                        //_logger.LogError($"유효성 검사 오류 발생: {invEx.Message}", invEx.ToString());
                        //UpdateStatus($"유효성 오류: {invEx.Message}");
                        throw;
                            //return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        //_logger.LogError($"알 수 없는 오류 발생: {ex.Message}", ex.ToString());
                        //UpdateStatus($"오류: {ex.Message}");
                        throw;
                            //return false;
                    }
                }
            }
            }
            catch (Exception ex)
            {
                
                //_logger.LogError($"알 수 없는 오류 발생: {ex.Message}", ex.ToString());
                //UpdateStatus($"오류: {ex.Message}");
                throw;
                //return false;
            }
        }

        
    }
}
