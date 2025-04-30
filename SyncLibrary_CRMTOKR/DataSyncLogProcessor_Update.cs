using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyncCommon;

namespace SyncLibrary
{
    public class DataSyncLogProcessor_Update : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        private readonly SyncTaskJob _syncTaskJob;
        private readonly SqlLogger _logger;
        private const int BatchSize = 2; // 배치로 처리할 로그 수

        public DataSyncLogProcessor_Update(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfoProvider, SyncTaskJob syncTaskJob)
            : base(logger, dbConnectionInfoProvider, syncTaskJob)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
            _syncTaskJob = syncTaskJob;
        }

        public override async Task ProcessLogsAsync()
        {
            var table = "";
            // ReferenceTables 리스트에서 첫 번째 값 가져오기
            if (_syncTaskJob.ReferenceTables != null && _syncTaskJob.ReferenceTables.Count > 0)
            {
                table = _syncTaskJob.ReferenceTables[0];
                
            }
            _syncTaskJob.TargetTable = table;
            DataTable sourceData = await LoadTableDataAsync(_dbConnectionInfoProvider.LocalServer(), table,false); // 원본 테이블의 전체 데이터 로드
            if (sourceData.Rows.Count == 0)
            {
                UpdateStatus("No data to process.");
                return;
            }

            string currentSqlQuery = null;
            try
            {
                var (localConnectionString, remoteConnectionString) = _dbConnectionInfoProvider.GetConnectionInfo(
                    _syncTaskJob.SourceDB,
                    _syncTaskJob.TargetDB);

                // 대상 테이블에서 기존 데이터를 읽어옴
                DataTable targetData = await LoadTableDataAsync(remoteConnectionString, table,true);

                Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes = null;

                List<string> primaryKeys = new List<string>();

                (fieldTypes, primaryKeys) = GetFieldTypesAndPrimaryKeyFromDatabase(table, localConnectionString);

                //원본테이블이 대상 테이블컬럼보다 많은 경우 컬럼 생성구문
                AddMissingColumnsAsync(sourceData, targetData, table, remoteConnectionString);
                // 원본 데이터와 대상 데이터를 비교하여 차이점만 처리
                bool success = await SyncTableDataWithUpsertAsync(sourceData, targetData, fieldTypes, primaryKeys, remoteConnectionString);

                if (success)
                {
                    UpdateStatus("Data sync completed successfully.");
                }
                else
                {
                    UpdateStatus("Data sync failed.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing logs: {ex.Message}", currentSqlQuery);
                throw;
            }
        }

        /// <summary>
        /// sourceData테이블 기준으로 봤을때 targetData테이블에 필드가 부족한 경우 해당 필드들을 찾아서 먼저 추가해준다.
        /// </summary>
        /// <param name="sourceData"></param>
        /// <param name="targetData"></param>
        /// <param name="targetTableName"></param>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        private async Task AddMissingColumnsAsync(DataTable sourceData, DataTable targetData, string targetTableName, string connectionString)
        {
            // SourceTable과 TargetTable의 컬럼 이름을 추출
            var sourceColumns = sourceData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();
            var targetColumns = targetData.Columns.Cast<DataColumn>().Select(c => c.ColumnName).ToList();

            // TargetTable에 없는 SourceTable의 컬럼을 추출
            var missingColumns = sourceColumns.Except(targetColumns).ToList();

            if (missingColumns.Count > 0)
            {
                // 필드 타입 정보 얻기 (SourceTable 기준)
                var fieldTypes = GetFieldTypesAndPrimaryKeyFromDatabase(targetTableName, connectionString).Item1;

                using (SqlConnection connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync();
                    using (SqlCommand command = new SqlCommand())
                    {
                        command.Connection = connection;

                        // 필요한 컬럼들을 추가하는 ALTER TABLE 구문 생성
                        foreach (var columnName in missingColumns)
                        {
                            if (fieldTypes.ContainsKey(columnName))
                            {
                                var fieldType = fieldTypes[columnName];
                                string columnType = fieldType.DataType;

                                if (fieldType.MaxLength.HasValue)
                                {
                                    columnType += $"({fieldType.MaxLength.Value})";
                                }
                                else if (fieldType.Precision.HasValue && fieldType.Scale.HasValue)
                                {
                                    columnType += $"({fieldType.Precision.Value}, {fieldType.Scale.Value})";
                                }

                                string alterTableQuery = $"ALTER TABLE {targetTableName} ADD {columnName} {columnType};";
                                command.CommandText = alterTableQuery;

                                // 컬럼 추가 쿼리 실행
                                await command.ExecuteNonQueryAsync();
                            }
                        }
                    }
                }
            }
        }
        private static SemaphoreSlim semaphore = new SemaphoreSlim(3);
        // 원본 데이터와 대상 데이터를 비교하여 차이점만 처리하는 메서드 (Insert, Update, Delete)
        private async Task<bool> SyncTableDataWithUpsertAsync(DataTable sourceData, DataTable targetData, Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes ,List<string> primaryKeys, string remoteConnectionString)
        {
            try
            {
                // 임시 테이블명 설정
                string tempTableName = _syncTaskJob.TargetTable + "_Temp";
                using (SqlConnection connection = new SqlConnection(remoteConnectionString))
                {
                    await connection.OpenAsync();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string dropTempTableQuery = $"DROP TABLE IF EXISTS {tempTableName}";
                            using (SqlCommand dropCommand = new SqlCommand(dropTempTableQuery, connection, transaction))
                            {
                                await dropCommand.ExecuteNonQueryAsync();
                            }

                            // 1. _Temp 테이블 생성
                            string createTempTableQuery = GenerateCreateTempTableQuery(tempTableName, fieldTypes, primaryKeys);
                            using (SqlCommand createTempTableCmd = new SqlCommand(createTempTableQuery, connection, transaction))
                            {
                                await createTempTableCmd.ExecuteNonQueryAsync();
                            }

                            // 2. sourceData를 _Temp 테이블에 삽입
                            await BulkInsertToTempTableAsync(connection, transaction, tempTableName, sourceData,300000);

                            // 3. _Temp 테이블과 대상 테이블을 MERGE
                            string mergeQuery = GenerateMergeQuery(tempTableName, _syncTaskJob.TargetTable, fieldTypes, primaryKeys, excludedField: "status,,row_status");
                            using (SqlCommand mergeCmd = new SqlCommand(mergeQuery, connection, transaction))
                            {
                                // CommandTimeout 값을 600초(10분)로 설정 (필요에 따라 변경 가능)
                                mergeCmd.CommandTimeout = 600;
                                await mergeCmd.ExecuteNonQueryAsync();
                            }

                            // 4. 트랜잭션 커밋
                            transaction.Commit();
                            return true;


                          
                        }
                        catch (Exception ex)
                        {
                            // 트랜잭션 롤백
                            transaction.Rollback();
                            _logger.LogError($"Sync Table Merge {_syncTaskJob.TargetTable}  SQL오류: {ex.Message}");
                            Console.WriteLine($"Sync Table Merge {_syncTaskJob.TargetTable} SQL오류: {ex.Message}");
                            //_logger.LogError($"SQL 오류 발생: {sqlEx.Message}", sqlEx.ToString());
                            throw;
                        }
                        finally
                        {
                            // 6. Temp 테이블 삭제
                            //string dropTempTableQuery = $"DROP TABLE IF EXISTS {tempTableName}";
                            //using (SqlCommand dropCommand = new SqlCommand(dropTempTableQuery, connection))
                            //{
                            //    await dropCommand.ExecuteNonQueryAsync();
                            //}
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Sync failed: {ex.Message}");
                throw;
            }
        }

        // 임시 테이블 생성 쿼리

        private string GenerateCreateTempTableQuery(string tableName,Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes,  List<string> primaryKeys)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {tableName} (");

            foreach (var field in fieldTypes)
            {
                string fieldType = field.Value.DataType; // DataType 추출
                int? maxLength = field.Value.MaxLength;  // MaxLength 추출
                int? precision = field.Value.Precision;  // Precision 추출
                int? scale = field.Value.Scale;          // Scale 추출

                // 데이터 타입에 따라 크기나 정밀도 설정
                if ((fieldType.StartsWith("varchar") || fieldType.StartsWith("nvarchar") || fieldType.StartsWith("char")) && maxLength.HasValue)
                {
                    fieldType += $"({maxLength.Value})"; // MaxLength 설정
                }
                else if (fieldType == "decimal" && precision.HasValue && scale.HasValue)
                {
                    fieldType += $"({precision.Value}, {scale.Value})"; // Decimal의 Precision과 Scale 설정
                }

                sb.AppendLine($"[{field.Key}] {fieldType},");
            }

            // 마지막 ',' 제거
            sb.Remove(sb.Length - 1, 1);

            // Primary Key 제약 조건 추가
            if (primaryKeys != null && primaryKeys.Count > 0)
            {
                string primaryKeyConstraint = string.Join(", ", primaryKeys.Select(pk => $"[{pk}]"));
                sb.AppendLine($" PRIMARY KEY({primaryKeyConstraint})");
            }

            sb.AppendLine(");");
            return sb.ToString();
        }
  

        // MERGE 쿼리 생성
        private string GenerateMergeQuery(string tempTableName, string targetTableName, Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes, List<string> primaryKeys, string excludedField)
        {

            // 제외할 필드를 배열로 변환
            var excludedFields = excludedField.Split(',').Select(f => f.Trim()).ToList();

            // MERGE 쿼리 시작 부분 생성
            StringBuilder mergeQuery = new StringBuilder();
            mergeQuery.AppendLine($"MERGE INTO {targetTableName} AS target");
            mergeQuery.AppendLine($"USING {tempTableName} AS source");
            mergeQuery.AppendLine("ON");


            // ON 절에 PRIMARY KEY 조건 추가
            var primaryKeyConditions = primaryKeys.Select(pk => $"target.{pk} = source.{pk}").ToList();
            mergeQuery.AppendLine(string.Join(" AND ", primaryKeyConditions));
            //mergeQuery.AppendLine(string.Join(" AND status ='S'"));
            // WHEN MATCHED THEN UPDATE 절
            mergeQuery.AppendLine("WHEN MATCHED THEN");
            mergeQuery.AppendLine("UPDATE SET");


            //foreach (var field in fieldTypes.Keys.Where(f => !excludedFields.Contains(f) && f != "itrg_cdt"))
            foreach (var field in fieldTypes.Keys.Where(f => !excludedFields.Contains(f) && f != "ROW_STATUS" && f != "STATUS" && f != "UPD_DT" && f != "REG_DT" && f != "UPD_NO"))
           {

                // 필드가 변경되었을 때만 업데이트
                mergeQuery.AppendLine($"target.{field} = source.{field},");
            }



            // status는 필드가 변경된 경우 N 로 업데이트
            mergeQuery.AppendLine("target.STATUS = CASE WHEN ");
            mergeQuery.AppendLine(string.Join(" OR ", fieldTypes.Keys
                .Where(f => !excludedFields.Contains(f) && f != "STATUS")
                .Select(f => $"(target.{f} <> source.{f} OR (target.{f} IS NULL AND source.{f} IS NOT NULL) OR (target.{f} IS NOT NULL AND source.{f} IS NULL))")));
            mergeQuery.AppendLine(" THEN 'N' ELSE target.STATUS END,");

            // ROW_STATUS 필드는 데이터가 변경된 경우에 U 로 업데이트
            mergeQuery.AppendLine("target.ROW_STATUS = CASE WHEN ");
            mergeQuery.AppendLine(string.Join(" OR ", fieldTypes.Keys
                .Where(f => !excludedFields.Contains(f) && f != "ROW_STATUS")
                .Select(f => $"(target.{f} <> source.{f} OR (target.{f} IS NULL AND source.{f} IS NOT NULL) OR (target.{f} IS NOT NULL AND source.{f} IS NULL))")));
            //mergeQuery.AppendLine(" THEN null END,");
            mergeQuery.AppendLine(" THEN 'U' ELSE target.ROW_STATUS END,");

            mergeQuery.AppendLine("target.UPD_DT = getdate(),"); 


            // 마지막 ',' 제거
            mergeQuery.Remove(mergeQuery.Length - 3, 1);

            // WHEN NOT MATCHED BY SOURCE THEN DELETE 절을 삭제하고, 'intg_com'을 'D'로 업데이트
            //mergeQuery.AppendLine("WHEN NOT MATCHED BY SOURCE THEN");
            //mergeQuery.AppendLine("UPDATE SET target.intg_com = 'D'");

            // WHEN NOT MATCHED THEN INSERT 절
            mergeQuery.AppendLine("WHEN NOT MATCHED THEN");
            mergeQuery.AppendLine("INSERT (");


            // INSERT할 필드 목록
            var insertFields = fieldTypes.Keys.ToList();
                //.Where(f => !excludedFields.Contains(f)).ToList();
            mergeQuery.AppendLine(string.Join(", ", insertFields));

            mergeQuery.AppendLine(") VALUES (");

            // INSERT할 값 목록
            foreach (var field in insertFields)
            {

                if (field == "STATUS")
                {
                    mergeQuery.AppendLine("'N',"); // 현재 시간
                }
                else if(field == "ROW_STATUS")
                {
                    mergeQuery.AppendLine("'I',"); // 현재 시간
                }
                else if(field == "REG_DT")
                {
                    mergeQuery.AppendLine("getdate(),"); // 현재 시간
                }
                else
                {
                    mergeQuery.AppendLine($"source.{field},");
                }
            }

            // 마지막 ',' 제거
            mergeQuery.Remove(mergeQuery.Length - 3, 1);
            mergeQuery.AppendLine(");");

            
            return mergeQuery.ToString();
        }
        // MERGE 쿼리 생성
     
        private async Task BulkInsertToTempTableAsync(SqlConnection connection, SqlTransaction transaction, string tempTableName, DataTable sourceData, int chunkSize = 50000)
        {
            // DataTable을 청크 단위로 나눠서 처리
            for (int i = 0; i < sourceData.Rows.Count; i += chunkSize)
            {
                // Semaphore에 접근을 시도
                await semaphore.WaitAsync(); // 대기
                try
                {
                    // 청크에 해당하는 DataTable 생성
                    DataTable chunk = sourceData.Clone(); // 스키마 복사

                    for (int j = i; j < i + chunkSize && j < sourceData.Rows.Count; j++)
                    {
                        chunk.ImportRow(sourceData.Rows[j]); // 각 행을 청크에 복사
                       
                    }

                    // 청크 단위로 Bulk Insert 처리
                    using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
                    {
                        bulkCopy.DestinationTableName = tempTableName;
                        bulkCopy.BatchSize = 10000;  // 한번에 처리할 배치 크기 설정
                        bulkCopy.NotifyAfter = 50000;  // 5000개 행마다 이벤트 발생
                        bulkCopy.SqlRowsCopied += (sender, e) => {
                            // 로그 기록이나 진행 상황 표시
                            Console.WriteLine($"{e.RowsCopied} rows copied.");
                        };

                        // 타임아웃 설정 (예: 10분)
                        bulkCopy.BulkCopyTimeout = 600; // 기본 30초 -> 300초(5분)로 설정
                        try
                        {
                            await bulkCopy.WriteToServerAsync(chunk); // 청크별로 데이터를 _Temp 테이블에 삽입
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"청크 단위로 Bulk Insert 처리  Error(566): {ex.Message}");
                            throw new Exception($"Bulk insert failed for chunk starting at row {i}. Error: {ex.Message}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"DataTable을 청크 단위로 나눠서 처리  Error(573): {ex.Message}");
                    throw new Exception($"Bulk insert failed for chunk starting at row {i}. Error: {ex.Message}");
                }
                finally
                {
                    // Semaphore 해제
                    semaphore.Release();
                }
            }
        }


        // DataTable의 데이터 유형을 SQL Server 테이블의 데이터 유형에 맞게 조정
        private DataTable AdjustDataTableToSqlTypes(DataTable sourceData, SqlConnection connection, string tableName, SqlTransaction transaction)
        {
            // 새로운 DataTable 생성 (sourceData와 동일한 스키마를 따름)
            DataTable adjustedData = sourceData.Clone(); // 스키마 복사

            // 각 행을 순회하면서 데이터를 복사
            foreach (DataRow row in sourceData.Rows)
            {
                // 새로운 DataRow 생성
                DataRow newRow = adjustedData.NewRow();
                newRow.ItemArray = row.ItemArray; // 데이터 복사
                adjustedData.Rows.Add(newRow);
            }

            return adjustedData;

        }

  


        // 기본 키 값을 가져오는 도우미 메서드
        private List<object> GetPrimaryKeyValues(DataRow row, List<string> primaryKeys)
        {
            return primaryKeys.Select(pk => row[pk]).ToList();
        }
        // 원본 테이블 데이터 로드
        private async Task<DataTable> LoadTableDataAsync(string connectionString, string tableName,bool isTop = false )
        {
            DataTable data = new DataTable();
            string query = "";
            if (isTop==true)
                query = $"SELECT top 1 * FROM {tableName}  with(nolock)";
            else
                query = $"SELECT  * FROM {tableName}  with(nolock)";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                    {
                        adapter.Fill(data);
                    }
                }
            }

            return data;
        }


    }
}
