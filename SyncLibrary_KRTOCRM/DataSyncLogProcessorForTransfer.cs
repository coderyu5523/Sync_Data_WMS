using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncCommon;
namespace SyncLibrary
{
    public class DataSyncLogProcessorForTransfer : BaseDataSyncProcessor, IDataSyncProcessor
    {
        private Logger _logger; // Logger 인스턴스 추가
        List<int> processedLogIds = new List<int>(); // 클래스 필드로 선언
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;

        public DataSyncLogProcessorForTransfer(Logger logger, DbConnectionInfoProvider dbConnectionInfoProvider) : base(logger, dbConnectionInfoProvider)
        {
            _logger = logger;
            _dbConnectionInfoProvider = dbConnectionInfoProvider ?? throw new ArgumentNullException(nameof(dbConnectionInfoProvider));
        }

        public void SetConnectionStrings(string localConnectionString, string remoteConnectionString)
        {
            this.localConnectionString = localConnectionString;
            this.remoteConnectionString = remoteConnectionString;
        }

        public override async Task ProcessLogsAsync()
        {
            // ERP DB에서 데이터를 읽어와서 WMS DB에 이관하는 로직
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
            // ERP에서 WMS으로 데이터 전송 로직
        }

        private DataTable LoadLogs()
        {
            // ERP DB에서 데이터 로드 로직
            return new DataTable();
        }
    }

}
