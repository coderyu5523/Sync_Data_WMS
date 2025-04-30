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
    public partial class frmSyncMonitoring_history : Form
    {
        private Main_Sync_WMSData mdiParentForm; // MDI 부모 폼 참조
        //private Schedule_Stats scheduleStats;  // 스케줄 통계 객체
        private System.Windows.Forms.Timer refreshTimer; // 주기적으로 갱신할 타이머

        public frmSyncMonitoring_history()
        {
            InitializeComponent();
            // MDI 자식 폼을 최대화 상태로 설정
            this.WindowState = FormWindowState.Maximized;
            InitializeGrid();
            //mdiParentForm = parentForm; // 부모 폼 저장
            //scheduleStats = stats;


        }
         

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

       

        private void btnLoadLogs_Click(object sender, EventArgs e)
        {
            // 사용자가 선택한 날짜 가져오기
            DateTime selectedDate = dateTimePicker1.Value;

            // 그리드에 로그 표시
            LoadLogsToGrid(selectedDate);
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
                    dataGridView1.Rows.Add(logDetails[0], logDetails[1], logDetails[2], logDetails[3], logDetails[4], logDetails[5], logDetails[6], logDetails[7], logDetails[8], logDetails[9], logDetails[10], logDetails[11]);
                }
            }
        }


    }
}
