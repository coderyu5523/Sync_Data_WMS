using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using SyncCommon;

namespace SyncLibrary
{

    public class DataSyncLogProcessorForSave : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;

        private const int BatchSize = 10; // 배치로 처리할 로그 수
        //List<int> processedLogIds = new List<int>(); // 클래스 필드로 선언
        private readonly Logger _logger;

        public DataSyncLogProcessorForSave(Logger logger, DbConnectionInfoProvider dbConnectionInfoProvider) : base(logger, dbConnectionInfoProvider)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
        }


        public override async Task ProcessLogsAsync()
        {
            // WMS DB에서 데이터를 읽어와서 ERP DB 임시 테이블에 저장하는 로직
            DataTable logData = LoadLogs(BatchSize);
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
                bool success = await ApplyBatchToTempTableAndExecuteProcedureAsync(group.CopyToDataTable(), localConnectionString, remoteConnectionString);
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

        private async Task<bool> ApplyBatchToTempTableAndExecuteProcedureAsync(DataTable logData, string localConnectionString, string remoteConnectionString)
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
                        // 처리된 로그 ID를 저장할 지역 변수
                        List<int> processedLogIds = new List<int>();
                        // 1. 임시 테이블에 데이터 저장
                        bool isTempTableUpdateSuccessful = await ApplyBatchToTempTableAsync(logData, connection, transaction, processedLogIds);

                        // 임시 테이블에 데이터 저장이 성공한 경우에만 저장 프로시저 호출
                        if (isTempTableUpdateSuccessful)
                        {
                            // 2. 저장 프로시저 호출
                            await ExecuteStoredProcedureAsync(connection, transaction);

                            // 3. 처리된 로그의 상태를 업데이트
                            if (processedLogIds == null || processedLogIds.Count == 0)
                            {
                                throw new Exception("No logs processed. Cannot update log status.");
                            }

                            // 모든 작업이 성공하면 커밋
                            transaction.Commit();
                            UpdateStatus("Batch processed and procedure executed successfully.");

                            // 로그의 상태를 업데이트
                            bool updateSuccess = MarkLogsAsProcessed(processedLogIds, localConnectionString);
                            if (!updateSuccess)
                            {

                                //transaction.Rollback();
                                UpdateStatus("MarkLogsAsProcessed Error ");// executed successfully.");
                                return false;

                            }
                            return true;
                        }
                        else
                        {
                            // 임시 테이블 저장 실패 시 롤백
                            transaction.Rollback();
                            UpdateStatus("Failed to update temp table, rolling back transaction.");
                            return false;
                        }

                    }
                    catch (Exception ex)
                    {
                        // 오류 발생 시 롤백
                        transaction.Rollback();
                        _logger.LogError($"Transaction error: {ex.Message}");
                        UpdateStatus($"Error: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        private async Task<bool> ApplyBatchToTempTableAsync(DataTable logData, SqlConnection connection, SqlTransaction transaction, List<int> processedLogIds)
        {
            try
            {
                string old_tableName = "";
                Dictionary<string, string> fieldTypes = null;
                //List<int> processedLogIds = new List<int>(); // 메서드의 지역 변수로 선언
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

                    // SQL 쿼리 생성
                    string queryText = GenerateQueryText(changeType, changeDetails, tableName, primaryKeys, fieldTypes);

                    // 쿼리의 안전성 검사
                    if (!IsQuerySafe(queryText))
                    {
                        throw new InvalidOperationException("Unsafe query detected");
                    }

                    // 쿼리를 재시도와 함께 실행
                    await ExecuteQueryWithRetriesAsync(connection, queryText, transaction);

                    /// 공통 성공 처리 메서드 호출
                    ProcessSuccess(queryText, logId, processedLogIds, (sql) => queryText = sql);
                }

                UpdateStatus("Batch processed successfully.");
                return true;
            }
            catch (SqlException sqlEx)
            {
                transaction.Rollback();
                _logger.LogError($"SQL 오류 발생: {sqlEx.Message}", sqlEx.ToString());
                UpdateStatus($"SQL 오류: {sqlEx.Message}");
                return false;
            }
            catch (InvalidOperationException invEx)
            {
                transaction.Rollback();
                _logger.LogError($"유효성 검사 오류 발생: {invEx.Message}", invEx.ToString());
                UpdateStatus($"유효성 오류: {invEx.Message}");
                return false;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                _logger.LogError($"알 수 없는 오류 발생: {ex.Message}", ex.ToString());
                UpdateStatus($"오류: {ex.Message}");
                return false;
            }
        }



        // 저장 프로시저를 호출하는 메서드
        private async Task ExecuteStoredProcedureAsync(SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                using (SqlCommand command = new SqlCommand("SDB100_IF_WMSTOERP", connection, transaction))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // 필요한 파라미터 추가
                    string value = "";
                    command.Parameters.AddWithValue("@wrk_ty", value);
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (SqlException sqlEx)
            {
                // SQL 오류 발생 시 처리
                _logger.LogError($"SQL 오류 발생: {sqlEx.Message}", sqlEx.ToString());
                UpdateStatus($"SQL 오류: {sqlEx.Message}");
                throw; // 예외를 다시 던져서 호출자에게 알림
            }
            catch (InvalidOperationException invEx)
            {
                // 잘못된 작업 오류 발생 시 처리
                _logger.LogError($"유효성 검사 오류 발생: {invEx.Message}", invEx.ToString());
                UpdateStatus($"유효성 오류: {invEx.Message}");
                throw;
            }
            catch (Exception ex)
            {
                // 일반적인 오류 발생 시 처리
                _logger.LogError($"알 수 없는 오류 발생: {ex.Message}", ex.ToString());
                UpdateStatus($"오류: {ex.Message}");
                throw;
            }
        }

        //private DataTable LoadLogs()
        //{
        //    // WMS DB에서 데이터 로드 로직
        //    // 실제 로직을 구현해야 합니다. 예를 들어, 데이터베이스 쿼리를 실행하여 DataTable을 반환하는 로직
        //    return new DataTable();
        //}

        private void LogOperation(string message)
        {
            _logger.LogOperation(message);
        }

        private void UpdateStatus(string message)
        {
            // 상태 업데이트 로직 추가
        }
    }
}
