using Quartz.Impl;
using Quartz;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Quartz.Impl.Matchers;
using Sync_WMSData.SyncMonitoring;
using System.Threading;
using System.IO;

namespace Sync_WMSData
{
    public partial class frmSyncMonitoring : Form
    {
        private Main_Sync_WMSData mdiParentForm; // MDI 부모 폼 참조
        //private Schedule_Stats scheduleStats;  // 스케줄 통계 객체
        private System.Windows.Forms.Timer refreshTimer; // 주기적으로 갱신할 타이머

        public frmSyncMonitoring(Main_Sync_WMSData parentForm)
        {
            InitializeComponent();
            // MDI 자식 폼을 최대화 상태로 설정
            this.WindowState = FormWindowState.Maximized;
            mdiParentForm = parentForm; // 부모 폼 저장
            //scheduleStats = stats;

            // 타이머 초기화
            //refreshTimer = new System.Windows.Forms.Timer();
            //refreshTimer.Interval = 2000; // 2초마다 갱신
            //refreshTimer.Tick += RefreshTimer_Tick;


            // 스케줄러에 리스너를 등록하면서 콜백 메서드 전달
            IScheduler scheduler = mdiParentForm.GetScheduler();
            scheduler.ListenerManager.AddJobListener(new MyJobListener(AddJobToGridAndSaveLog));
            InitializeGrid();
            this.Load += frmSyncMonitorying_Load;
            _logManager = new LogManager();
            LoadLogToGrid();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = 1000; // 1초마다 갱신
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            lblCurrentTime.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"); // 라벨에 현재 시간 표시
        }
        // 콜백 메서드: 작업 상태를 리스트박스에 추가하는 메서드
        //public void AddJobToListBox(string jobInfo)
        //{
        //    if (InvokeRequired)
        //    {
        //        Invoke(new Action(() => listBoxJobs.Items.Add(jobInfo)));
        //    }
        //    else
        //    {
        //        listBoxJobs.Items.Add(jobInfo);
        //    }
        //}

        // 그리드 초기화 메서드
        private void InitializeGrid()
        {
            // 컬럼 설정
            //dataGridView1.Columns.Add("TaskId", "Task ID");
            //dataGridView1.Columns.Add("TaskName", "Task Name");
            //dataGridView1.Columns.Add("StartTime", "Start Time");
            //dataGridView1.Columns.Add("Status", "Status");
            //dataGridView1.Columns.Add("ScheduleType", "Schedule Type");
            //dataGridView1.Columns.Add("Interval", "Interval");
            //dataGridView1.Columns.Add("SpecificTime", "Specific Time");

            // TaskId는 정수형 (텍스트 박스 사용)
            DataGridViewTextBoxColumn taskIdColumn = new DataGridViewTextBoxColumn();
            taskIdColumn.Name = "TaskId";
            taskIdColumn.HeaderText = "Task ID";
            taskIdColumn.ValueType = typeof(int);
            dataGridView1.Columns.Add(taskIdColumn);

            // TaskName은 문자열
            DataGridViewTextBoxColumn taskNameColumn = new DataGridViewTextBoxColumn();
            taskNameColumn.Name = "TaskName";
            taskNameColumn.HeaderText = "Task Name";
            taskNameColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(taskNameColumn);

            // StartTime은 DateTime형
            DataGridViewTextBoxColumn startTimeColumn = new DataGridViewTextBoxColumn();
            startTimeColumn.Name = "StartTime";
            startTimeColumn.HeaderText = "Start Time";
            startTimeColumn.ValueType = typeof(DateTime);
            dataGridView1.Columns.Add(startTimeColumn);

            // Status는 문자열
            DataGridViewTextBoxColumn statusColumn = new DataGridViewTextBoxColumn();
            statusColumn.Name = "Status";
            statusColumn.HeaderText = "Status";
            statusColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(statusColumn);

            // ScheduleType은 문자열
            DataGridViewTextBoxColumn scheduleTypeColumn = new DataGridViewTextBoxColumn();
            scheduleTypeColumn.Name = "ScheduleType";
            scheduleTypeColumn.HeaderText = "Schedule Type";
            scheduleTypeColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(scheduleTypeColumn);

            // Interval은 TimeSpan형 (시간 간격)
            DataGridViewTextBoxColumn intervalColumn = new DataGridViewTextBoxColumn();
            intervalColumn.Name = "Interval";
            intervalColumn.HeaderText = "Interval";
            intervalColumn.ValueType = typeof(TimeSpan);
            dataGridView1.Columns.Add(intervalColumn);

            // SpecificTime은 DateTime형 (특정 시간)
            //DataGridViewTextBoxColumn specificTimeColumn = new DataGridViewTextBoxColumn();
            //specificTimeColumn.Name = "SpecificTime";
            //specificTimeColumn.HeaderText = "Specific Time";
            //specificTimeColumn.ValueType = typeof(DateTime);
            //dataGridView1.Columns.Add(specificTimeColumn);

            // ReferenceTables는 List<string> (참조 테이블 목록을 문자열로 보여줌)
          

            // SourceDB는 문자열
            DataGridViewTextBoxColumn sourceDBColumn = new DataGridViewTextBoxColumn();
            sourceDBColumn.Name = "SourceDB";
            sourceDBColumn.HeaderText = "Source DB";
            sourceDBColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(sourceDBColumn);

            // TargetDB는 문자열
            DataGridViewTextBoxColumn targetDBColumn = new DataGridViewTextBoxColumn();
            targetDBColumn.Name = "TargetDB";
            targetDBColumn.HeaderText = "Target DB";
            targetDBColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(targetDBColumn);

            DataGridViewTextBoxColumn referenceTablesColumn = new DataGridViewTextBoxColumn();
            referenceTablesColumn.Name = "ReferenceTables";
            referenceTablesColumn.HeaderText = "Reference Tables";
            referenceTablesColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(referenceTablesColumn);

            DataGridViewTextBoxColumn targetTable = new DataGridViewTextBoxColumn();
            targetTable.Name = "TargetTable";
            targetTable.HeaderText = "targetTable";
            targetTable.ValueType = typeof(string);
            targetTable.Visible = false;
            dataGridView1.Columns.Add(targetTable);

            // ProcedureName은 문자열
            DataGridViewTextBoxColumn procedureNameColumn = new DataGridViewTextBoxColumn();
            procedureNameColumn.Name = "ProcedureName";
            procedureNameColumn.HeaderText = "Procedure Name";
            procedureNameColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(procedureNameColumn);

            // SyncDirection은 문자열 (DB1 -> DB2, 양방향 등)
            DataGridViewTextBoxColumn syncDirectionColumn = new DataGridViewTextBoxColumn();
            syncDirectionColumn.Name = "SyncDirection";
            syncDirectionColumn.HeaderText = "Sync Direction";
            syncDirectionColumn.ValueType = typeof(string);
            dataGridView1.Columns.Add(syncDirectionColumn);

            //// IsActive는 bool (활성화 여부를 체크박스로 표시)
            //DataGridViewCheckBoxColumn isActiveColumn = new DataGridViewCheckBoxColumn();
            //isActiveColumn.Name = "IsActive";
            //isActiveColumn.HeaderText = "Is Active";
            //isActiveColumn.ValueType = typeof(bool);
            //dataGridView1.Columns.Add(isActiveColumn);
            // 그리드에 스크롤 설정
            dataGridView1.ScrollBars = ScrollBars.Both;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill; // 열 채우기
        }

        // 타이머가 주기적으로 호출하여 통계 업데이트
        //private async void RefreshTimer_Tick(object sender, EventArgs e)
        //{
        //    //DisplayStats(); // 통계 표시
        //    await UpdateJobStatus(); // 스케줄러 상태 업데이트
        //}

        // 통계 정보 UI에 표시
        private void DisplayStats()
        {
            //lblTotalJobsExecuted.Text = $"총 실행된 작업: {scheduleStats.TotalJobsExecuted}";
            //lblTotalJobsFailed.Text = $"실패한 작업: {scheduleStats.TotalJobsFailed}";
            //lblLastExecutedJob.Text = $"마지막 실행 작업: {scheduleStats.LastExecutedJob}";
        }

        // 버튼 클릭 시 통계 업데이트
        private void btnRefresh_Click(object sender, EventArgs e)
        {
            DisplayStats(); // 통계를 새로고침
        }

        private async void frmSyncMonitorying_Load(object sender, EventArgs e)
        {
            //refreshTimer.Start();
            
            //DisplayStats();
        }
        // AddJobToGrid 메서드의 서명을 MyJobListener가 요구하는 형태로 맞추기
        public void AddJobToGrid(int taskId, string taskName, DateTime startTime, string status, string scheduleType, TimeSpan? interval, string sourceDB, string targetDB, string referenceTables, string procedureName,string syncDirection,string  targetTable)
        {
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    dataGridView1.Rows.Add(taskId, taskName, startTime.ToString("yyyy-MM-dd HH:mm:ss"), status, scheduleType, interval?.ToString(), sourceDB, targetDB, referenceTables, targetTable, procedureName, syncDirection);
                }));
            }
            else
            {
                dataGridView1.Rows.Add(taskId, taskName, startTime.ToString("yyyy-MM-dd HH:mm:ss"), status, scheduleType, interval?.ToString(), sourceDB, targetDB, referenceTables, targetTable, procedureName, syncDirection);
            }
        }
        private async Task UpdateJobStatus()
        {
            IScheduler scheduler = mdiParentForm.GetScheduler(); // 부모 폼에서 스케줄러 가져옴

            if (scheduler == null || !scheduler.IsStarted)
            {
                MessageBox.Show("스케줄러가 실행 중이지 않습니다.");
                return;
            }

            var jobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

            //listBoxJobs.Items.Clear(); // 상태를 표시할 ListBox 초기화

            //foreach (var jobKey in jobKeys)
            //{
            //    var triggers = await scheduler.GetTriggersOfJob(jobKey);
            //    foreach (var trigger in triggers)
            //    {
            //        var triggerState = await scheduler.GetTriggerState(trigger.Key);
            //        listBoxJobs.Items.Add($"Job: {jobKey.Name}, 상태: {triggerState}");
            //    }
            //}
        }
        public void AddJobToGridAndSaveLog(int taskId, string taskName, DateTime startTime, string status, string scheduleType, TimeSpan? interval, string sourceDB, string targetDB,string referenceTables,string procedureName,string syncDirection,string targetTable)
        {
            
            AddJobToGrid(taskId, taskName, startTime, status, scheduleType, interval, sourceDB, targetDB, referenceTables,  procedureName, syncDirection,targetTable);

           
        }


        //public void AddJobToGridAndSaveLog(int taskId, string taskName, DateTime startTime, string status, string scheduleType, TimeSpan? interval, string specificTime)
        //{
        //    // 그리드에 작업 정보 추가
        //    AddJobToGrid(taskId, taskName, startTime, status, scheduleType, interval, specificTime);

        //    // 로그 파일에 작업 정보 저장
        //    SaveLogToFile($"{taskId},{taskName},{startTime:yyyy-MM-dd HH:mm:ss},{status},{scheduleType},{interval?.ToString()},{specificTime}");
        //}
        private readonly LogManager _logManager;
        public void LoadLogToGrid()
        {
            LoadLogsToGrid(DateTime.Today.Date);
            
        }

        public void LoadLogsToGrid(DateTime selectedDate)
        {
            // LogManager 인스턴스 생성
            LogManager logManager = new LogManager();

            // 선택한 날짜의 로그 파일을 읽음
            string[] logs = logManager.ReadLogsFromFile(selectedDate);

            // 그리드 초기화
            dataGridView1.Rows.Clear();

            // 로그 내용을 그리드에 추가
            foreach (var log in logs)
            {
                string[] logDetails = log.Split(',');
                if (logDetails.Length >= 10) // 로그 항목이 모두 포함된 경우
                {
                    dataGridView1.Rows.Add(logDetails[0], logDetails[1], logDetails[2], logDetails[3], logDetails[4], logDetails[5], logDetails[6], logDetails[7], logDetails[8], logDetails[11], logDetails[9], logDetails[10]);
                }
            }
        }        


        private void button1_Click(object sender, EventArgs e)
        {
            DisplayStats(); // 통계를 새로고침
        }

        // 폼이 닫힐 때 타이머 정지
        private void frmSyncMonitorying_FormClosing(object sender, FormClosingEventArgs e)
        {
            refreshTimer.Stop(); // 폼이 닫힐 때 타이머 정지
        }

         

    }
}
