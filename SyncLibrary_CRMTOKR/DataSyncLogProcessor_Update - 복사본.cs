using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyncCommon;

namespace SyncLibrary
{
    public class DataSyncLogProcessor_Update1 : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        private readonly SyncTaskJob _syncTaskJob;
        private readonly SqlLogger _logger;
        private const int BatchSize = 2; // 배치로 처리할 로그 수

        public DataSyncLogProcessor_Update1(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfoProvider, SyncTaskJob syncTaskJob)
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

                Dictionary<string, string> fieldTypes = null;

                List<string> primaryKeys = new List<string>();

                (fieldTypes, primaryKeys) = GetFieldTypesAndPrimaryKeyFromDatabase(table, localConnectionString);
                
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
        // 원본 데이터와 대상 데이터를 비교하여 차이점만 처리하는 메서드 (Insert, Update, Delete)
        private async Task<bool> SyncTableDataWithUpsertAsync(DataTable sourceData, DataTable targetData, Dictionary<string, string> fieldTypes ,List<string> primaryKeys, string remoteConnectionString)
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
                            // 1. _Temp 테이블 생성
                            string createTempTableQuery = GenerateCreateTempTableQuery(tempTableName, fieldTypes, primaryKeys);
                            using (SqlCommand createTempTableCmd = new SqlCommand(createTempTableQuery, connection, transaction))
                            {
                                await createTempTableCmd.ExecuteNonQueryAsync();
                            }

                            // 2. sourceData를 _Temp 테이블에 삽입
                            await BulkInsertToTempTableAsync(connection, transaction, tempTableName, sourceData);

                            // 3. _Temp 테이블과 대상 테이블을 MERGE
                            string mergeQuery = GenerateMergeQuery(tempTableName, _syncTaskJob.TargetTable, fieldTypes, primaryKeys, excludedField: "intg_com");
                            using (SqlCommand mergeCmd = new SqlCommand(mergeQuery, connection, transaction))
                            {
                                await mergeCmd.ExecuteNonQueryAsync();
                            }

                            // 4. 트랜잭션 커밋
                            transaction.Commit();
                            return true;


                            // 제외할 필드
                            //string excludedField = "intg_com";

                            //                        // 제외할 필드를 제거한 primaryKeys 생성
                            //var filteredPrimaryKeys = primaryKeys.Where(pk => pk != excludedField).ToList();

                            ////                        // 복합 기본 키를 결합하여 PK 비교
                            ////                        var pkValuesSource = GetPrimaryKeyValues(sourceRow, filteredPrimaryKeys);

                            //// 1. sourceData와 targetData의 차이점 찾기
                            //var differences = from sourceRow in sourceData.AsEnumerable()
                            //                  join targetRow in targetData.AsEnumerable()
                            //                  on GetPrimaryKeyValues(sourceRow, filteredPrimaryKeys)
                            //                  equals GetPrimaryKeyValues(targetRow, filteredPrimaryKeys)
                            //                  into temp
                            //                  from targetRow in temp.DefaultIfEmpty()
                            //                  where targetRow == null || !sourceRow.Table.Columns.Cast<DataColumn>()
                            //                      .Where(c => c.ColumnName != excludedField)  // 제외할 필드를 명시적으로 제거
                            //                      .All(c => sourceRow[c].Equals(targetRow?[c])) // 비교 수행
                            //                  //where targetRow == null || !sourceRow.ItemArray.SequenceEqual(targetRow.ItemArray)
                            //                  select new { sourceRow, targetRow };

                            //// 2. 차이점을 처리 (Insert 또는 Update)
                            //foreach (var difference in differences)
                            //{
                            //    if (difference.targetRow != null)
                            //    {
                            //        // Update
                            //        await UpdateRecordAsync(connection, difference.sourceRow, difference.targetRow, _syncTaskJob.TargetTable, filteredPrimaryKeys, transaction);
                            //    }
                            //    else
                            //    {
                            //        // Insert
                            //        await InsertRecordAsync(connection, difference.sourceRow, _syncTaskJob.TargetTable, transaction);
                            //    }
                            //}

                            //// 3. Delete (대상에만 존재하는 데이터 삭제)
                            //foreach (DataRow targetRow in targetData.Rows)
                            //{
                            //    var pkValuesTarget = GetPrimaryKeyValues(targetRow, primaryKeys);
                            //    if (!sourceData.AsEnumerable().Any(row => GetPrimaryKeyValues(row, primaryKeys).SequenceEqual(pkValuesTarget)))
                            //    {
                            //        await DeleteRecordAsync(connection, pkValuesTarget, _syncTaskJob.TargetTable, primaryKeys, transaction);
                            //    }
                            //}

                            //// 4. 트랜잭션 커밋
                            //transaction.Commit();
                            //return true;
                        }
                        catch (Exception ex)
                        {
                            // 트랜잭션 롤백
                            transaction.Rollback();
                            _logger.LogError($"Sync failed: {ex.Message}");
                            throw;
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
        private string GenerateCreateTempTableQuery1(string tempTableName, Dictionary<string, string> fieldTypes, List<string> primaryKeys)
        {
            var columns = fieldTypes.Select(kvp => $"{kvp.Key} {kvp.Value}");
            var primaryKeyConstraint = string.Join(", ", primaryKeys);
            return $"CREATE TABLE {tempTableName} ({string.Join(", ", columns)}, PRIMARY KEY({primaryKeyConstraint}))";
        }
        private string GenerateCreateTempTableQuery(Dictionary<string, string> fieldTypes, string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {tableName}_Temp (");

            foreach (var field in fieldTypes)
            {
                string fieldType = field.Value;

                // 데이터 타입에 따라 크기를 명시적으로 지정
                if (fieldType.StartsWith("varchar") || fieldType.StartsWith("nvarchar") || fieldType.StartsWith("char"))
                {
                    fieldType += "(MAX)"; // MAX 또는 적절한 크기로 설정
                }
                else if (fieldType == "decimal")
                {
                    fieldType += "(18, 6)"; // 적절한 정밀도와 소수점 설정
                }

                sb.AppendLine($"[{field.Key}] {fieldType},");
            }

            // 마지막 ',' 제거
            sb.Remove(sb.Length - 1, 1);

            sb.AppendLine(");");
            return sb.ToString();
        }
        // MERGE 쿼리 생성
        private string GenerateMergeQuery(string tempTableName, string targetTableName, Dictionary<string, string> fieldTypes, List<string> primaryKeys, string excludedField)
        {
            // 필드 리스트에서 제외 필드 제거
            var columnsToCompare = fieldTypes.Keys.Where(c => c != excludedField);
            var joinConditions = string.Join(" AND ", primaryKeys.Select(pk => $"src.{pk} = tgt.{pk}"));
            var updateConditions = string.Join(", ", columnsToCompare.Select(col => $"tgt.{col} = src.{col}"));

            return $@"
                    MERGE INTO {targetTableName} AS tgt
                    USING {tempTableName} AS src
                    ON {joinConditions}
                    WHEN MATCHED THEN
                        UPDATE SET {updateConditions}
                    WHEN NOT MATCHED BY TARGET THEN
                        INSERT ({string.Join(", ", columnsToCompare)})
                        VALUES ({string.Join(", ", columnsToCompare.Select(c => $"src.{c}"))})
                    WHEN NOT MATCHED BY SOURCE THEN
                        DELETE;
                ";
        }


        // Bulk insert to temp table
        private async Task BulkInsertToTempTableAsync(SqlConnection connection, SqlTransaction transaction, string tempTableName, DataTable sourceData)
        {
            // 대상 테이블의 필드 유형에 맞게 sourceData의 필드를 변환
            DataTable adjustedData = AdjustDataTableToSqlTypes(sourceData, connection, tempTableName, transaction);


            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.Default, transaction))
            {
                bulkCopy.DestinationTableName = tempTableName;

                // 각 컬럼에 대해 매핑 설정
                foreach (DataColumn column in adjustedData.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(adjustedData);
            }
        }


        // DataTable의 데이터 유형을 SQL Server 테이블의 데이터 유형에 맞게 조정
        private DataTable AdjustDataTableToSqlTypes(DataTable sourceData, SqlConnection connection, string tableName, SqlTransaction transaction)
        {
            // 새로운 DataTable 생성 (sourceData와 동일한 스키마를 따름)
            DataTable adjustedData = new DataTable();

            // 원본 테이블의 열을 순회하며 대상 테이블과 맞는 데이터 타입으로 변환
            foreach (DataColumn sourceColumn in sourceData.Columns)
            {
                // sourceData의 컬럼 이름과 데이터 타입 그대로 사용하여 새로운 DataTable에 추가
                adjustedData.Columns.Add(sourceColumn.ColumnName, sourceColumn.DataType);
            }

            // sourceData의 모든 데이터를 adjustedData로 복사
            foreach (DataRow row in sourceData.Rows)
            {
                adjustedData.ImportRow(row);
            }

            // 데이터를 복사
            //foreach (DataRow row in sourceData.Rows)
            //{
            //    adjustedData.ImportRow(row);
            //}

            return adjustedData;
        }

        // 대상 테이블의 메타데이터를 가져오는 메서드
        private DataTable GetTableSchema(SqlConnection connection, string tableName)
        {
            using (SqlCommand command = new SqlCommand($"SELECT TOP 0 * FROM {tableName}", connection))
            using (SqlDataAdapter adapter = new SqlDataAdapter(command))
            {
                DataTable schemaTable = new DataTable();
                adapter.FillSchema(schemaTable, SchemaType.Source);
                return schemaTable;
            }
        }

        private DataTable GetTableSchema(SqlConnection connection, string tableName, SqlTransaction transaction = null)
        {
            using (SqlCommand command = new SqlCommand($"SELECT TOP 0 * FROM {tableName}", connection))
            {
                // 트랜잭션이 존재하면 명시적으로 설정
                if (transaction != null)
                {
                    command.Transaction = transaction;
                }

                using (SqlDataAdapter adapter = new SqlDataAdapter(command))
                {
                    DataTable schemaTable = new DataTable();
                    adapter.FillSchema(schemaTable, SchemaType.Source);
                    return schemaTable;
                }
            }
        }

        // 원본 데이터와 대상 데이터를 비교하여 차이점만 처리하는 메서드 (Insert, Update, Delete)
        //private async Task<bool> SyncTableDataWithUpsertAsync(DataTable sourceData, DataTable targetData, List<string> primaryKeys, string remoteConnectionString)
        //{
        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(remoteConnectionString))
        //        {
        //            await connection.OpenAsync();
        //            using (SqlTransaction transaction = connection.BeginTransaction())
        //            {
        //                try
        //                {
        //                    // 1. Insert 또는 Update (원본에 있지만 대상에 없는 데이터 삽입 또는 수정)
        //                    foreach (DataRow sourceRow in sourceData.Rows)
        //                    {
        //                        // 제외할 필드
        //                        string excludedField = "intg_com";

        //                        // 제외할 필드를 제거한 primaryKeys 생성
        //                        var filteredPrimaryKeys = primaryKeys.Where(pk => pk != excludedField).ToList();

        //                        // 복합 기본 키를 결합하여 PK 비교
        //                        var pkValuesSource = GetPrimaryKeyValues(sourceRow, filteredPrimaryKeys);
        //                        DataRow targetRow = targetData.AsEnumerable()
        //                            .FirstOrDefault(row => GetPrimaryKeyValues(row, filteredPrimaryKeys).SequenceEqual(pkValuesSource));


        //                        if (targetRow != null)
        //                        {
        //                            // 모든 필드를 비교하여 동일한지 확인
        //                            bool isIdentical = sourceRow.Table.Columns.Cast<DataColumn>()
        //                                .Where(c => c.ColumnName != excludedField) // 제외할 필드
        //                                .All(c => sourceRow[c.ColumnName].Equals(targetRow[c.ColumnName]));

        //                            if (!isIdentical)
        //                            {
        //                                // 동일하지 않다면 Update 작업
        //                                await UpdateRecordAsync(connection, sourceRow, targetRow, _syncTaskJob.TargetTable, filteredPrimaryKeys, transaction);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            // Insert
        //                            await InsertRecordAsync(connection, sourceRow, _syncTaskJob.TargetTable, transaction);
        //                        }
        //                    }

        //                    // 2. Delete (대상에만 존재하는 데이터 삭제)
        //                    foreach (DataRow targetRow in targetData.Rows)
        //                    {
        //                        // 복합 기본 키를 결합하여 PK 비교
        //                        var pkValuesTarget = GetPrimaryKeyValues(targetRow, primaryKeys);

        //                        if (!sourceData.AsEnumerable().Any(row => GetPrimaryKeyValues(row, primaryKeys).SequenceEqual(pkValuesTarget)))
        //                        {
        //                            await DeleteRecordAsync(connection, pkValuesTarget, _syncTaskJob.TargetTable, primaryKeys, transaction);
        //                        }
        //                    }

        //                    // 3. 트랜잭션 커밋
        //                    transaction.Commit();
        //                    return true;
        //                }
        //                catch (Exception ex)
        //                {
        //                    // 트랜잭션 롤백
        //                    transaction.Rollback();
        //                    _logger.LogError($"Sync failed: {ex.Message}");
        //                    throw;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError($"Sync failed: {ex.Message}");
        //        throw;
        //    }
        //}
        // 기본 키 값을 가져오는 도우미 메서드
        private List<object> GetPrimaryKeyValues(DataRow row, List<string> primaryKeys)
        {
            return primaryKeys.Select(pk => row[pk]).ToList();
        }
        // 원본 테이블 데이터 로드
        private async Task<DataTable> LoadTableDataAsync(string connectionString, string tableName)
        {
            DataTable data = new DataTable();
            string query = $"SELECT * FROM {tableName}";

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

        // 데이터 삽입 메서드
        private async Task InsertRecordAsync(SqlConnection connection, DataRow sourceRow, string tableName, SqlTransaction transaction)
        {
            string columnNames = string.Join(", ", sourceRow.Table.Columns.Cast<DataColumn>().Select(c => c.ColumnName));

            string values = string.Join(", ", sourceRow.ItemArray.Select((value, index) =>
            {
                // 해당 컬럼의 데이터 타입 확인
                var columnType = sourceRow.Table.Columns[index].DataType;

                // 문자열 타입이면 작은 따옴표로 감싸기
                if (columnType == typeof(string))
                {
                    return $"'{value}'";
                    //문자열 ' 로 오류가 발생하면 아래 코드로 대체하기
                    //string escapedValue = value.ToString().Replace("'", "''"); // 작은 따옴표 이스케이프
                    //return $"'{escapedValue}'";
                }
                // DateTime 타입이면 ISO 8601 형식으로 변환
                else if (columnType == typeof(DateTime))
                {
                    DateTime dateValue = Convert.ToDateTime(value);
                    return $"'{dateValue:yyyy-MM-dd HH:mm:ss}'"; // ISO 8601 형식으로 변환
                }
                // 숫자일 경우 따옴표 없이 반환
                else if (columnType == typeof(int) || columnType == typeof(double) || columnType == typeof(decimal))
                {
                    return value.ToString();
                }
                // NULL 값일 경우
                else if (value == DBNull.Value)
                {
                    return "NULL";
                }
                // 기타 타입 처리
                else
                {
                    return $"'{value}'"; // 기본적으로 따옴표로 감싸기
                }
            }));

            string query = $"INSERT INTO {tableName} ({columnNames}) VALUES ({values})";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                await command.ExecuteNonQueryAsync();
            }

        }

        // 데이터 업데이트 메서드
        private async Task UpdateRecordAsync(SqlConnection connection, DataRow sourceRow, DataRow targetRow, string tableName, List<string> primaryKeys, SqlTransaction transaction)
        {
            try
            {
                var setClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                foreach (DataColumn column in sourceRow.Table.Columns)
                {
                    if (!primaryKeys.Contains(column.ColumnName))
                    {
                        // 업데이트할 컬럼을 정의 (기본 키는 제외)
                        setClauses.Add($"{column.ColumnName} = @{column.ColumnName}");
                        parameters.Add(new SqlParameter($"@{column.ColumnName}", sourceRow[column]));
                    }
                }

                // WHERE 절에 복합 기본 키 처리
                var whereClauses = new List<string>();
                foreach (var pk in primaryKeys)
                {
                    whereClauses.Add($"{pk} = @{pk}");
                    parameters.Add(new SqlParameter($"@{pk}", sourceRow[pk]));
                }

                string query = $"UPDATE {tableName} SET {string.Join(", ", setClauses)} WHERE {string.Join(" AND ", whereClauses)}";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating record in {tableName}: {ex.Message}");
                throw;
            }

            //string setClause = string.Join(", ", sourceRow.Table.Columns.Cast<DataColumn>()
            //    .Where(c => !c.ColumnName.Equals(primaryKey))
            //    .Select(c => $"{c.ColumnName} = '{sourceRow[c.ColumnName]}'"));

            //string query = $"UPDATE {tableName} SET {setClause} WHERE {primaryKey} = '{sourceRow[primaryKey]}'";

            //using (SqlCommand command = new SqlCommand(query, connection, transaction))
            //{
            //    await command.ExecuteNonQueryAsync();
            //}
        }

        private async Task DeleteRecordAsync(SqlConnection connection, List<object> pkValues, string tableName, List<string> primaryKeys, SqlTransaction transaction)
        {
            try
            {
                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                // WHERE 절에 복합 기본 키 처리
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    var pk = primaryKeys[i];
                    whereClauses.Add($"{pk} = @{pk}");
                    parameters.Add(new SqlParameter($"@{pk}", pkValues[i]));
                }

                string query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting record from {tableName}: {ex.Message}");
                throw;
            }
        }

/*
        // 데이터 삭제 메서드
        private async Task DeleteRecordAsync(SqlConnection connection, object pkValue, string tableName, List<string> primaryKeys, SqlTransaction transaction)
        {
            try
            {
                var whereClauses = new List<string>();
                var parameters = new List<SqlParameter>();

                // WHERE 절에 복합 기본 키 처리
                for (int i = 0; i < primaryKeys.Count; i++)
                {
                    var pk = primaryKeys[i];
                    whereClauses.Add($"{pk} = @{pk}");
                    parameters.Add(new SqlParameter($"@{pk}", pkValues[i]));
                }

                string query = $"DELETE FROM {tableName} WHERE {string.Join(" AND ", whereClauses)}";

                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                {
                    command.Parameters.AddRange(parameters.ToArray());
                    await command.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deleting record from {tableName}: {ex.Message}");
                throw;
            }

            //string query = $"DELETE FROM {tableName} WHERE {primaryKey} = '{pkValue}'";

            //using (SqlCommand command = new SqlCommand(query, connection, transaction))
            //{
            //    await command.ExecuteNonQueryAsync();
            //}
        }*/
    }
}
