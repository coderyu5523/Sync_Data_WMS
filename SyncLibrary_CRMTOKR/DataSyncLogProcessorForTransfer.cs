using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SyncCommon;
namespace SyncLibrary
{
    public class DataSyncLogProcessorForTransfer : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private SqlLogger _logger; // Logger 인스턴스 추가
        List<int> processedLogIds = new List<int>(); // 클래스 필드로 선언
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        private readonly SyncTaskJob _syncTaskJob;
        public DataSyncLogProcessorForTransfer(SqlLogger logger, DbConnectionInfoProvider dbConnectionInfoProvider, SyncTaskJob syncTaskJob) : base(logger, dbConnectionInfoProvider, syncTaskJob)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
            _syncTaskJob = syncTaskJob;
        }

        

        public void SetConnectionStrings(string localConnectionString, string remoteConnectionString)
        {
            this.localConnectionString = localConnectionString;
            this.remoteConnectionString = remoteConnectionString;
        }

        public override async Task ProcessLogsAsync()
        {
            // ERP DB에서 데이터를 읽어와서 CRM DB에 이관하는 로직
            DataTable logData = LoadLogs();
            if (logData.Rows.Count == 0)
            {
                LogOperation("No logs to transfer.");
                return;
            }

            await TransferDataAsync(logData);
            LogOperation("Data transfer completed.");
        }

        private async Task TransferDataAsync(DataTable logData)
        {
            // ERP에서 CRM으로 데이터 전송 로직
        }

        private DataTable LoadLogs()
        {
            // ERP DB에서 데이터 로드 로직
            return new DataTable();
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
    }

}
