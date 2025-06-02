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
    public class DataSyncLogProcessor_Update_S4 : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        private readonly SyncTaskJob _syncTaskJob;
        private readonly SqlLogger _logger;
        private const int BatchSize = 2; // 배치로 처리할 로그 수

        public DataSyncLogProcessor_Update_S4(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfoProvider, SyncTaskJob syncTaskJob)
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
            DataTable sourceData = await LoadTableDataAsync(_dbConnectionInfoProvider.LocalServer(), table); // 원본 테이블의 전체 데이터 로드
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
                DataTable targetData = await LoadTableDataAsync(remoteConnectionString, table);

                Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes = null;
                List<string> primaryKeys = new List<string>();

                (fieldTypes, primaryKeys) = GetFieldTypesAndPrimaryKeyFromDatabase(table, localConnectionString);

                //원본테이블이 대상 테이블컬럼보다 많은 경우 컬럼 생성구문
                AddMissingColumnsAsync(sourceData, targetData, table, remoteConnectionString);

                // 소스 DB와 타겟 DB 모두에 대한 트랜잭션 시작
                using (SqlConnection sourceConnection = new SqlConnection(localConnectionString))
                using (SqlConnection targetConnection = new SqlConnection(remoteConnectionString))
                {
                    await sourceConnection.OpenAsync();
                    await targetConnection.OpenAsync();

                    using (SqlTransaction sourceTransaction = sourceConnection.BeginTransaction())
                    using (SqlTransaction targetTransaction = targetConnection.BeginTransaction())
                    {
                        try
                        {
                            // 타겟 DB에 MERGE 실행
                            bool success = await ExecuteMergeAsync(sourceData, targetData, fieldTypes, primaryKeys, targetConnection, targetTransaction);
                            
                            if (success)
                            {
                                // 소스 DB의 STATUS만 'N'에서 'S'로 업데이트
                                await UpdateSourceStatusAsync(sourceData, primaryKeys, sourceConnection, sourceTransaction);
                                
                                // 두 트랜잭션 모두 커밋
                                sourceTransaction.Commit();
                                targetTransaction.Commit();
                                
                                UpdateStatus("Data sync completed successfully.");
                            }
                            else
                            {
                                // 실패 시 롤백
                                sourceTransaction.Rollback();
                                targetTransaction.Rollback();
                                UpdateStatus("Data sync failed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            // 예외 발생 시 롤백
                            sourceTransaction.Rollback();
                            targetTransaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing logs: {ex.Message}", currentSqlQuery);
                throw;
            }
        }

        private async Task<bool> ExecuteMergeAsync(DataTable sourceData, DataTable targetData,
            Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes,
            List<string> primaryKeys, SqlConnection connection, SqlTransaction transaction)
        {
            try
            {
                // 임시 테이블명 설정
                string tempTableName = _syncTaskJob.TargetTable + "_Temp";

                // 1. _Temp 테이블 생성
                string createTempTableQuery = GenerateCreateTempTableQuery(tempTableName, fieldTypes, primaryKeys);
                using (SqlCommand createTempTableCmd = new SqlCommand(createTempTableQuery, connection, transaction))
                {
                    await createTempTableCmd.ExecuteNonQueryAsync();
                }

                // 2. sourceData를 _Temp 테이블에 삽입
                await BulkInsertToTempTableAsync(connection, transaction, tempTableName, sourceData);

                // 3. _Temp 테이블과 대상 테이블을 MERGE
                string mergeQuery = GenerateMergeQuery(tempTableName, _syncTaskJob.TargetTable, fieldTypes, primaryKeys, "status,row_status");
                using (SqlCommand mergeCmd = new SqlCommand(mergeQuery, connection, transaction))
                {
                    mergeCmd.CommandTimeout = 600; // 10분 타임아웃
                    await mergeCmd.ExecuteNonQueryAsync();
                }

                // 4. qty 컬럼이 있는 경우 로그 테이블 생성 및 데이터 기록
                if (fieldTypes.Keys.Any(k => k.ToLower() == "qty"))
                {
                    await CreateAndPopulateQtyLogTableAsync(connection, transaction, sourceData, targetData);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error in ExecuteMergeAsync: {ex.Message}");
                return false;
            }
        }

        private async Task CreateAndPopulateQtyLogTableAsync(SqlConnection connection, SqlTransaction transaction, 
            DataTable sourceData, DataTable targetData)
        {
            string logTableName = $"{_syncTaskJob.TargetTable}_qty_log";

            // 1. 어제 날짜의 qty 데이터 수집
            var yesterday = DateTime.Now.AddDays(-1).Date;
            var sourceQty = sourceData.AsEnumerable()
                .Where(r => r["qty"] != DBNull.Value && 
                       r["reg_dt"] != DBNull.Value && 
                       Convert.ToDateTime(r["reg_dt"]).Date == yesterday)
                .Sum(r => Convert.ToDecimal(r["qty"]));

            var targetQty = targetData.AsEnumerable()
                .Where(r => r["qty"] != DBNull.Value && 
                       r["reg_dt"] != DBNull.Value && 
                       Convert.ToDateTime(r["reg_dt"]).Date == yesterday)
                .Sum(r => Convert.ToDecimal(r["qty"]));

            // 2. 로그 데이터 삽입
            string insertLogQuery = $@"
                INSERT INTO {logTableName} (table_name, source_qty, target_qty, reg_dt)
                VALUES (@tableName, @sourceQty, @targetQty, @regDt)";

            using (SqlCommand insertLogCmd = new SqlCommand(insertLogQuery, connection, transaction))
            {
                insertLogCmd.Parameters.AddWithValue("@tableName", _syncTaskJob.TargetTable);
                insertLogCmd.Parameters.AddWithValue("@sourceQty", sourceQty);
                insertLogCmd.Parameters.AddWithValue("@targetQty", targetQty);
                insertLogCmd.Parameters.AddWithValue("@regDt", yesterday);
                await insertLogCmd.ExecuteNonQueryAsync();
            }
        }

        private async Task UpdateSourceStatusAsync(DataTable sourceData, List<string> primaryKeys, 
            SqlConnection connection, SqlTransaction transaction)
        {
            foreach (DataRow sourceRow in sourceData.Rows)
            {
                // STATUS가 'N'인 경우에만 'S'로 업데이트
                if (sourceRow["STATUS"].ToString() == "N")
                {
                    var whereClauses = new List<string>();
                    var parameters = new List<SqlParameter>();

                    // WHERE 절에 복합 기본 키 처리
                    foreach (var pk in primaryKeys)
                    {
                        whereClauses.Add($"{pk} = @{pk}");
                        parameters.Add(new SqlParameter($"@{pk}", sourceRow[pk]));
                    }

                    string query = $"UPDATE {_syncTaskJob.TargetTable} SET STATUS = 'S', UPD_DT = GETDATE() WHERE {string.Join(" AND ", whereClauses)}";

                    using (SqlCommand command = new SqlCommand(query, connection, transaction))
                    {
                        command.Parameters.AddRange(parameters.ToArray());
                        await command.ExecuteNonQueryAsync();
                    }
                }
            }
        }

        private async Task<DataTable> LoadTableDataAsync(string connectionString, string tableName)
        {
            DataTable data = new DataTable();
            string query = $"SELECT * FROM {tableName} with(nolock)";

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

        private string GenerateMergeQuery(string tempTableName, string targetTableName, 
            Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes,
            List<string> primaryKeys, string excludedField)
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

            // WHEN MATCHED THEN UPDATE 절
            mergeQuery.AppendLine("WHEN MATCHED THEN");
            mergeQuery.AppendLine("UPDATE SET");

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
            mergeQuery.AppendLine(" THEN 'U' ELSE target.ROW_STATUS END,");

            mergeQuery.AppendLine("target.UPD_DT = getdate(),");

            // 마지막 ',' 제거
            mergeQuery.Remove(mergeQuery.Length - 3, 1);

            // WHEN NOT MATCHED THEN INSERT 절
            mergeQuery.AppendLine("WHEN NOT MATCHED THEN");
            mergeQuery.AppendLine("INSERT (");

            // INSERT할 필드 목록
            var insertFields = fieldTypes.Keys.ToList();
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

        private string GenerateCreateTempTableQuery(string tempTableName, 
            Dictionary<string, (string DataType, int? MaxLength, int? Precision, int? Scale)> fieldTypes,
            List<string> primaryKeys)
        {
            var createTableQuery = new StringBuilder();
            createTableQuery.AppendLine($"CREATE TABLE {tempTableName} (");

            var columnDefinitions = new List<string>();
            foreach (var field in fieldTypes)
            {
                string columnDefinition = $"{field.Key} {field.Value.DataType}";
                if (field.Value.MaxLength.HasValue)
                {
                    columnDefinition += $"({field.Value.MaxLength})";
                }
                else if (field.Value.Precision.HasValue && field.Value.Scale.HasValue)
                {
                    columnDefinition += $"({field.Value.Precision}, {field.Value.Scale})";
                }
                columnDefinitions.Add(columnDefinition);
            }

            createTableQuery.AppendLine(string.Join(",\n", columnDefinitions));
            createTableQuery.AppendLine(");");

            return createTableQuery.ToString();
        }

        private async Task BulkInsertToTempTableAsync(SqlConnection connection, SqlTransaction transaction, 
            string tempTableName, DataTable sourceData)
        {
            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = tempTableName;
                bulkCopy.BatchSize = 1000;
                bulkCopy.BulkCopyTimeout = 600; // 10분 타임아웃

                foreach (DataColumn column in sourceData.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(sourceData);
            }
        }
    }
} 