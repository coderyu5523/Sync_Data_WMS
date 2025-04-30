//using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
//using Microsoft.Extensions.Logging;
using SyncCommon;
using SyncLibrary;
namespace SyncLibrary
{
    public class DataSyncLog_OrderUpdate : BaseDataSyncProcessor, IDataSyncProcessor
    {
        
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        //List<int> processedLogIds = new List<int>(); // 클래스 필드로 선언

        private const int BatchSize = 500000; // 배치로 처리할 로그 수
        //private const int MaxRetryAttempts = 3; // 재시도 횟수
        //private const int RetryDelayMilliseconds = 2000; // 재시도 대기 시간 (밀리초)
        //readonly
        private Logger _logger; // Logger 인스턴스 추가
        // 진행 상태 및 로그 업데이트를 위한 이벤트
        public event Action<string> StatusUpdated;
        public event Action<string> LogUpdated;
        public event Action<string> ErrorOccurred;

        //연결정보를 받는 경우에는 파라메터로 받는다

        public DataSyncLog_OrderUpdate(Logger logger, DbConnectionInfoProvider dbConnectionInfoProvider) : base(logger, dbConnectionInfoProvider)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
        }

        public override async Task ProcessLogsAsync()
        {
            
            DataTable logData = LoadLogs(BatchSize);
            //UpdateStatus($"Process Start - {logData.Rows.Count} 건 - {DateTime.Now}");

            if (logData.Rows.Count == 0)
            {
                LogOperation("No logs to process.");
                return;
            }

            // 국가별로 데이터를 그룹화
            var groupedData = logData.AsEnumerable()
                .GroupBy(row => row["Srt_Svr"].ToString())
                .ToList();

            // 각 국가 그룹에 대해 순차적으로 처리
            foreach (var group in groupedData)
            {
                string countryCode = group.Key;

                // 국가 코드에 따른 연결 정보 설정
                var (localConnectionString, remoteConnectionString) = _dbConnectionInfoProvider.GetConnectionInfo(group.First()["Srt_Svr"].ToString(), group.First()["Des_Svr"].ToString());

                // 각 국가 그룹의 데이터를 처리
                bool success = await ApplyBatchToRemoteDatabaseAsync(group.CopyToDataTable(), remoteConnectionString);
                if (success)
                {
                    // 처리된 로그의 상태를 업데이트
                    //MarkLogsAsProcessed(processedLogIds);
                    LogOperation($"Data synchronization completed successfully for {countryCode}.");
                }
                else
                {
                    LogOperation($"Data synchronization failed for {countryCode}.");
                }
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
         

        private async Task<bool> ApplyBatchToRemoteDatabaseAsync(DataTable logData,  string remoteConnectionString)
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
                        List<int> processedLogIds = new List<int>();
                        List<string> primaryKeys = new List<string>();
                        foreach (DataRow row in logData.Rows)
                        {

                            string tableName = row["TableName"].ToString();
                            string changeType = row["ChangeType"].ToString();
                            string changeDetails = row["ChangeDetails"].ToString();
                            int logId = Convert.ToInt32(row["LogId"]); // 로그 ID 가져오기

                            // 테이블 이름이 변경될 때마다 필드 타입과 기본 키 정보를 갱신
                            if (tableName != old_tableName)
                            {
                                old_tableName = tableName;
                                (fieldTypes, primaryKeys) = GetFieldTypesAndPrimaryKeyFromDatabase(tableName, _dbConnectionInfoProvider.ProxyServer());
                            }
                                                                                    
                            string queryText = GenerateQueryText(changeType, changeDetails, tableName, primaryKeys, fieldTypes);


                            if (!IsQuerySafe(queryText))
                            {
                                throw new InvalidOperationException("Unsafe query detected");
                            }

                            await ExecuteQueryWithRetriesAsync(connection, queryText, transaction);

                            /// 공통 성공 처리 메서드 호출
                            ProcessSuccess(queryText, logId, processedLogIds, (sql) => queryText = sql);

                            
                        }

                        // 로그의 상태를 업데이트
                        //await MarkLogsAsProcessed(processedLogIds, connection, transaction);

                        //if (!updateSuccess)
                        //{
                        //    throw new Exception("Failed to update log statuses.");
                        //}
                        if (processedLogIds == null || processedLogIds.Count == 0)
                        {
                            ErrorOccurred?.Invoke($"오류: No logs processed. Cannot update log status.Line-154");
                            throw new Exception("No logs processed. Cannot update log status.");
                        }

                        // 모든 작업이 성공하면 커밋
                        transaction.Commit();
                        UpdateStatus("Batch processed and procedure executed successfully.");

                        //transaction.Commit();
                        //UpdateStatus("Batch processed successfully.");
                        // 로그의 상태를 업데이트
                        bool updateSuccess = MarkLogsAsProcessed(processedLogIds, _dbConnectionInfoProvider.LocalServer());
                        if (!updateSuccess)
                        {

                            //transaction.Rollback();
                            //UpdateStatus("MarkLogsAsProcessed Error ");// executed successfully.");
                            ErrorOccurred?.Invoke($"오류: MarkLogsAsProcessed Error.Line-171");
                            return false;

                        }
                        return true;

                    }
                    catch (SqlException sqlEx)
                    {
                        transaction.Rollback();
                        _logger.LogError($"SQL 오류 발생: {sqlEx.Message}", sqlEx.ToString());
                        ErrorOccurred?.Invoke($"SQL 오류 발생: {sqlEx.Message}");
                        UpdateStatus($"SQL 오류: {sqlEx.Message}");

                        return false;
                    }
                    catch (InvalidOperationException invEx)
                    {
                        transaction.Rollback();
                        _logger.LogError($"유효성 검사 오류 발생: {invEx.Message}", invEx.ToString());
                        ErrorOccurred?.Invoke($"유효성 검사 오류 발생: {invEx.Message}");
                        //UpdateStatus($"유효성 오류: {invEx.Message}");
                        return false;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        _logger.LogError($"알 수 없는 오류 발생: {ex.Message}", ex.ToString());
                        //UpdateStatus($"오류: {ex.Message}");
                        ErrorOccurred?.Invoke($"알 수 없는 오류 발생: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        
            
        

    }
}
