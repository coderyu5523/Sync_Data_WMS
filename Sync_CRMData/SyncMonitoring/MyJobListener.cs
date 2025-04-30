using Quartz;
using Sync_WMSData.SyncMonitoring;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace Sync_WMSData
{
    
    /*
        * Quartz.NET의 Listener 사용
           JobListener나 TriggerListener를 사용하면, 각 작업(Job)이 언제 시작되고 언제 완료되는지 또는 오류가 발생하는지 실시간으로 확인할 수 있습니다.
       */
    public class MyJobListener : IJobListener
    {
        //private readonly Schedule_Stats _scheduleStats; // 스케줄 통계를 관리할 객체
        private Action<string> _jobExecutedCallback1; // 콜백 메서드 저장

        // 생성자에서 Schedule_Stats 객체를 받아 저장
        //public MyJobListener(Schedule_Stats scheduleStats)
        //public MyJobListener(Action<string> jobExecutedCallback)
        //{
        //    //_scheduleStats = scheduleStats;
        //    _jobExecutedCallback1 = jobExecutedCallback; // 콜백 메서드를 받아 저장
        //}

        private readonly Action<int, string, DateTime, string, string, TimeSpan?, string, string, string, string,string,string> _jobExecutedCallback;
        private readonly LogManager _logManager; // LogManager 인스턴스 추가

        public MyJobListener(Action<int, string, DateTime, string, string, TimeSpan?, string, string, string, string, string, string> jobExecutedCallback)
        {
            _jobExecutedCallback = jobExecutedCallback;
            _logManager = new LogManager(); // LogManager 초기화
        }

    public string Name => "MyJobListener";
        // Job 실행 전 호출되는 메서드
        public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            var jobData = context.JobDetail.JobDataMap;

            
            // 정수형 필드 TaskId와 ScheduleId 가져오기
            int taskId = jobData.GetInt("TaskId"); // TaskId는 정수형
            string taskName = jobData.GetString("TaskName"); // TaskName은 문자열
            DateTime startTime = DateTime.Now; // 현재 시간을 기록
            string status = "Running";
            // 스케줄 타입, 인터벌, 특정 시간 등 가져오기
            string scheduleType = jobData.GetString("ScheduleType"); // 문자열
            TimeSpan? interval = jobData.ContainsKey("Interval") && jobData["Interval"] != null
                ? (TimeSpan?)TimeSpan.Parse(jobData.GetString("Interval"))
                : null; // TimeSpan으로 변환
            DateTime? specificTime = jobData.ContainsKey("SpecificTime") && jobData["SpecificTime"] != null
                ? (DateTime?)DateTime.Parse(jobData.GetString("SpecificTime"))
                : null; // Nullable DateTime 변환

            // SourceDB, TargetDB, SyncDirection 가져오기
            string sourceDB = jobData.GetString("SourceDB");
            string targetDB = jobData.GetString("TargetDB");
            string referenceTables = jobData.GetString("ReferenceTables");
            string procedureName = jobData.GetString("ProcedureName");
            string syncDirection = jobData.GetString("SyncDirection");
            string targettable = jobData.GetString("TargetTable");
            // 로그 정보를 생성
            string logInfo = $"{taskId}, {taskName},{startTime}, {status}, {scheduleType}, {interval}, {sourceDB}, {targetDB}, {referenceTables}, {procedureName},{syncDirection},{targettable}";

            // LogManager를 사용해 로그 파일에 저장
            _logManager.SaveLogToFile(logInfo);

            _jobExecutedCallback?.Invoke(taskId, taskName, startTime, "Running", scheduleType, interval, sourceDB, targetDB, referenceTables, procedureName, syncDirection, targettable);
            return Task.CompletedTask;
            //_jobExecutedCallback?.Invoke(taskId, taskName, DateTime.Now, "Running", scheduleType, interval, sourceDB, targetDB, specificTime);
            //return Task.CompletedTask;

            //string taskName = jobData.GetString("TaskName");
            //string scheduleType = jobData.GetString("ScheduleType");
            //string interval = jobData.GetString("Interval");
            //string specificTime = jobData.GetString("SpecificTime");

            //string jobInfo = $"Job {taskName} 실행 시작 (스케줄: {scheduleType}, 주기: {interval ?? specificTime}): {DateTime.Now}";

            ////string jobInfo = $"Job {context.JobDetail.Key.Name} 실행 시작: {DateTime.Now}";
            //_jobExecutedCallback?.Invoke(jobInfo); // 콜백 메서드를 통해 폼에 전달
            //return Task.CompletedTask;
        }

        // Job 실행 전 호출되는 메서드 (CancellationToken 추가)
        //public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken cancellationToken)
        //{
        //    Console.WriteLine($"Job {context.JobDetail.Key.Name} 실행 예정");
        //    return Task.CompletedTask;
        //}

            // Job이 실행 완료 후 호출되는 메서드
        public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        {
            var jobData = context.JobDetail.JobDataMap;
            

            // 정수형 필드 TaskId와 ScheduleId 가져오기
            int taskId = jobData.GetInt("TaskId"); // TaskId는 정수형
            string taskName = jobData.GetString("TaskName"); // TaskName은 문자열
            DateTime endTime = DateTime.Now; // 현재 시간을 기록

            // 스케줄 타입, 인터벌, 특정 시간 등 가져오기
            string scheduleType = jobData.GetString("ScheduleType"); // 문자열
            TimeSpan? interval = jobData.ContainsKey("Interval") && jobData["Interval"] != null
                ? (TimeSpan?)TimeSpan.Parse(jobData.GetString("Interval"))
                : null; // TimeSpan으로 변환
            //DateTime? specificTime = jobData.ContainsKey("SpecificTime") && jobData["SpecificTime"] != null
            //    ? (DateTime?)DateTime.Parse(jobData.GetString("SpecificTime"))
            //    : null; // Nullable DateTime 변환

            // SourceDB, TargetDB, SyncDirection 가져오기
            string sourceDB = jobData.GetString("SourceDB");
            string targetDB = jobData.GetString("TargetDB");
            string referenceTables = jobData.GetString("ReferenceTables");
            string procedureName = jobData.GetString("ProcedureName");
            string syncDirection = jobData.GetString("SyncDirection");
            string targettable = jobData.GetString("TargetTable");

            string status = jobException == null ? "Success" : $"Failed ({jobException.Message})";

            // 로그 정보를 생성
            string logInfo = $"{taskId}, {taskName},{endTime}, {status}, {scheduleType}, {interval}, {sourceDB}, {targetDB}, {referenceTables}, {procedureName},{syncDirection},{targettable}";

            // LogManager를 사용해 로그 파일에 저장
            _logManager.SaveLogToFile(logInfo);

            _jobExecutedCallback?.Invoke(taskId, taskName, endTime, status, scheduleType, interval, sourceDB, targetDB, referenceTables, procedureName, syncDirection, targettable);
            return Task.CompletedTask;

            //var jobData = context.JobDetail.JobDataMap;

            //string taskName = jobData.GetString("TaskName");
            //string scheduleType = jobData.GetString("ScheduleType");
            //string interval = jobData.GetString("Interval");
            //string specificTime = jobData.GetString("SpecificTime");

            //string jobInfo;

            //if (jobException == null)
            //{
            //    jobInfo = $"Job {taskName} 정상적으로 완료됨 (스케줄: {scheduleType}, 주기: {interval ?? specificTime}): {DateTime.Now}";

            ////string jobInfo = $"Job {context.JobDetail.Key.Name} 정상적으로 완료됨: {DateTime.Now}";

            //}
            //else
            //{
            //    //string jobInfo = $"Job {context.JobDetail.Key.Name} 실패: {jobException.Message} ({DateTime.Now})";
            //    jobInfo = $"Job {taskName} 실패 (스케줄: {scheduleType}, 주기: {interval ?? specificTime}): {DateTime.Now}, 오류: {jobException.Message}";
            //}
            //_jobExecutedCallback?.Invoke(jobInfo);
            //return Task.CompletedTask;
        }

        //// Job이 취소될 때 호출되는 메서드
        public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken cancellationToken)
        {
            // 작업이 취소되거나 중복 실행될 경우 처리 (스킵된 경우)
            var jobData = context.JobDetail.JobDataMap;
            int taskId = jobData.GetInt("TaskId");
            string taskName = jobData.GetString("TaskName");

            Console.WriteLine($"Task {taskId} ({taskName}) 중복 실행으로 스킵됨.");

            // 추가로 스킵된 작업을 로그에 저장하거나 필요한 작업 수행
            return Task.CompletedTask;

            //Console.WriteLine($"Job {context.JobDetail.Key.Name} 취소됨");
            //return Task.CompletedTask;
        }
        //// Job 실행 후 호출되는 메서드
        //public Task JobWasExecuted(IJobExecutionContext context, JobExecutionException jobException, CancellationToken cancellationToken)
        //{
        //    if (jobException == null)
        //    {
        //        //Console.WriteLine($"Job {context.JobDetail.Key.Name} 정상적으로 완료됨");
        //        _scheduleStats.JobExecuted(context.JobDetail.Key.Name);
        //    }
        //    else
        //    {
        //        //Console.WriteLine($"Job {context.JobDetail.Key.Name} 실행 중 오류 발생: {jobException.Message}");
        //        _scheduleStats.JobFailed(context.JobDetail.Key.Name);
        //    }
        //    return Task.CompletedTask;
        //}
    }
}
