using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using SyncCommon;
using SyncLibrary;
namespace DioImplant_batch
{
    public partial class MainForm : Form
    {
        private readonly SyncManager _syncManager; // SyncManager 인스턴스
        private readonly DbConnectionInfoProvider _dbConnectionInfoProvider;
        
        //private readonly DataSyncLogProcessor _dataSyncProcessor;
        private readonly DataSyncLogProcessorForSave _dataSyncProcessorForSave;
        private readonly DataSyncLogProcessorForTransfer _dataSyncProcessorForTransfer;

        private readonly Logger _logger; // Logger 인스턴스
        private string localConnectionString = "";
        private string remoteConnectionString = "";


        //public MainForm(DataSyncLogProcessor dataSyncLogProcessor, DataSyncLogProcessorForSave dataSyncProcessorForSave,
        //DataSyncLogProcessorForTransfer dataSyncProcessorForTransfer,
        //Logger logger, DbConnectionInfoProvider dbConnectionInfoProvider)
        public MainForm( DataSyncLogProcessorForSave dataSyncProcessorForSave,
        DataSyncLogProcessorForTransfer dataSyncProcessorForTransfer,
        Logger logger, DbConnectionInfoProvider dbConnectionInfoProvider)
        {
            InitializeComponent();
            
            //_dataSyncProcessor = dataSyncLogProcessor;
            _dataSyncProcessorForSave = dataSyncProcessorForSave;
            _dataSyncProcessorForTransfer = dataSyncProcessorForTransfer;
            _logger = logger;

            // DB 연결 정보를 가져와서 설정합니다.
            //var connectionInfo = _dbConnectionInfoProvider.GetDbConnectionInfo("C");

            // Logger 인스턴스 생성
            //_logger = new Logger(localConnectionString); // 실제 연결 문자열 사용
            //dataSyncLogProcessor.ConnectionString(localConnectionString, remoteConnectionString);
            // SyncManager 인스턴스 생성 및 이벤트 핸들러 등록
            _dbConnectionInfoProvider = dbConnectionInfoProvider; // DI로 주입받음
            _syncManager = new SyncManager(_dataSyncProcessorForSave, _logger);
            _syncManager.StatusUpdated += OnStatusUpdated;
            _syncManager.LogUpdated += OnLogUpdated;
            
        }

        //private void InitMsDb()
        //{
        //    string des_dbIp = "192.168.10.152";
        //    string src_dbIp = "192.168.10.155";
        //    string dbId = "erp";
        //    string dbPw = "itsp@7735";

        //    localConnectionString = Setting(src_dbIp, dbId, dbPw, "smart_db", "1616");
        //    remoteConnectionString = Setting(des_dbIp, dbId, dbPw, "smart_db_ir", "1616");
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

        private async void button1_Click(object sender, EventArgs e)
        {
            // 동기화 방식 선택
            //if (comboBoxSyncType.SelectedItem.ToString() == "데이터 저장 동기화")
            //{
            //    _syncManager.SetDataSyncProcessor(_dataSyncProcessorForSave);
            //}
            //else if (comboBoxSyncType.SelectedItem.ToString() == "데이터 이관 동기화")
            //{
            //    _syncManager.SetDataSyncProcessor(_dataSyncProcessorForTransfer);
            //}
            // 동기화 작업 시작
            UpdateStatus("동기화 작업 시작");

            // 비동기 동기화 작업 시작
            await Task.Run(() => _syncManager.StartSync());
        }

        // 상태 업데이트 이벤트 핸들러
        private void OnStatusUpdated(string status)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(OnStatusUpdated), new object[] { status });
                return;
            }
            lblStatus.Text = status;
            lstSyncLog.Items.Add(status);
        }

        // 로그 업데이트 이벤트 핸들러
        private void OnLogUpdated(string logMessage)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(OnLogUpdated), new object[] { logMessage });
                return;
            }
            lstSyncLog.Items.Add(logMessage);
        }

        private void UpdateStatus(string message)
        {
            OnStatusUpdated(message);
        }

        private void btnSetSyncPeriod_Click(object sender, EventArgs e)
        {
            // 사용자가 선택한 동기화 주기를 설정
            //var selectedInterval = comboBoxSyncInterval.SelectedItem.ToString();
            //_syncManager.SetSyncInterval(selectedInterval); // SyncManager를 통해 주기 설정
        }

        private void btnSelectTargetData_Click(object sender, EventArgs e)
        {
            // 사용자가 선택한 대상 데이터를 가져옴
            //var selectedItems = checkedListBoxTargetData.CheckedItems;
           // _syncManager.SetTargetData(selectedItems); // SyncManager를 통해 대상 데이터 설정
        }

        private void UpdateSyncLog(string logMessage)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<string>(UpdateSyncLog), new object[] { logMessage });
                return;
            }
            lstSyncLog.Items.Add(logMessage); // 로그 메시지를 ListBox에 추가
        }

        private async void  btn_order_sync_Click(object sender, EventArgs e)
        {
            _syncManager.SetDataSyncProcessor(_dataSyncProcessorForSave);
            // 동기화 작업 시작
            //UpdateStatus("동기화 작업 시작");

            // 비동기 동기화 작업 시작
            await Task.Run(() => _syncManager.StartSync());

            //UpdateStatus("동기화 작업 완료");
        }
    }
}
