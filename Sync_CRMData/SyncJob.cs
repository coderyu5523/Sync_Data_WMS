using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SyncLibrary;
using SyncCommon;
using Microsoft.Extensions.Logging;
using Quartz.Logging;
using static Quartz.Logging.OperationName;
using SyncScheduleManager;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
namespace Sync_WMSData
{
    [DisallowConcurrentExecution]  // 동일 작업의 중복 실행 방지
    public class SyncJob : IJob
    {
        //public async Task Execute(IJobExecutionContext context)
        //{
        //    try
        //    {
        //        var jobData = context.JobDetail.JobDataMap;
        //        int taskId = jobData.GetInt("TaskId");
        //        string taskName = jobData.GetString("TaskName");

        //        // 동기화 작업 시작
        //        Console.WriteLine($"Task {taskId} ({taskName}) 동기화 시작...");

        //        // 동기화 작업 수행
        //        await PerformSyncTask(taskId, taskName);

        //        // 동기화 완료
        //        Console.WriteLine($"Task {taskId} ({taskName}) 동기화 완료.");
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Task 실행 중 예외 발생: {ex.Message}");
        //    }
        //}

        //private Task PerformSyncTask(int taskId, string taskName)
        //{
        //    // 실제 동기화 작업 로직 구현
        //    // 시간이 오래 걸리는 동기화 작업을 수행
        //    return Task.CompletedTask;
        //}
        

        //private readonly ILogger<SyncJob> _logger;
        //private readonly SqlLogger _logger;

        // 생성자에서 Logger 주입
        //public SyncJob(SqlLogger logger)
        //{
        //    _logger = logger;
        //}

        public async Task Execute(IJobExecutionContext context)
        {
            try
            {
                int taskId = context.MergedJobDataMap.GetInt("TaskId");
                string taskName = context.MergedJobDataMap.GetString("TaskName");
                string scheduleType = context.MergedJobDataMap.GetString("ScheduleType");
                string sourceDB = context.MergedJobDataMap.GetString("SourceDB");
                string targetDB = context.MergedJobDataMap.GetString("TargetDB");
                string SyncDirection = context.MergedJobDataMap.GetString("SyncDirection");

                string procedureName = context.MergedJobDataMap.GetString("ProcedureName");
                string referenceTables = context.MergedJobDataMap.GetString("ReferenceTables");
                //string IsActive = context.MergedJobDataMap.GetString("IsActive");

                var combinedTaskSchedules = Task_Schedule_Manger.CombineTaskAndSchedule();

                // Find the specific task based on TaskId
                var task = combinedTaskSchedules.FirstOrDefault(t => t.Task.TaskId == taskId);

                if (task == null)
                {
                    Console.WriteLine($"Task {taskId} not found.");
                    return;
                }
                // Check if the task is active
                if (!task.Task.IsActive)
                {
                    Console.WriteLine($"Task {taskId} is not active and will not be executed.");
                    return;
                }
                // SyncTaskJob 객체 생성
                var syncTaskJob = new SyncTaskJob
                {
                    TaskId = context.MergedJobDataMap.GetInt("TaskId"),
                    TaskName = context.MergedJobDataMap.GetString("TaskName"),
                    ScheduleType = context.MergedJobDataMap.GetString("ScheduleType"),
                    SourceDB = context.MergedJobDataMap.GetString("SourceDB"),
                    TargetDB = context.MergedJobDataMap.GetString("TargetDB"),
                    SyncDirection = context.MergedJobDataMap.GetString("SyncDirection"),
                    ProcedureName = context.MergedJobDataMap.GetString("ProcedureName"),
                    ReferenceTables = context.MergedJobDataMap.GetString("ReferenceTables").Split(',').ToList()
                };

                // 작업 구분에 따라 적절한 DataSyncProcessor 선택 (예시)
                IDataSyncProcessor selectedProcessor = null;

                //DBConnInfo.
                DBConnInfo dBConnInfo = new DBConnInfo();
                ProxyServerInfo proxyServerInfo = new ProxyServerInfo();

                ProxyServerInfo serverInfo = new ProxyServerInfo();
                serverInfo = ProxyServerInfoManager.LoadSeverInfo();
                if (serverInfo == null)
                {
                    return;
                }

                dBConnInfo.proxyDbIp= serverInfo.ServerIP;
                dBConnInfo.proxyDbId = serverInfo.dbid;
                dBConnInfo.proxyDbPw = serverInfo.dbpwd;
                dBConnInfo.proxyDbName = serverInfo.dbname;
                dBConnInfo.proxyDbPort = serverInfo.dbport;

                DbConnectionInfoProvider dbConnectionInfoProvider = new DbConnectionInfoProvider(syncTaskJob.SourceDB, dBConnInfo);
                SqlLogger _logger = new SqlLogger(dbConnectionInfoProvider);
                

                // Logger로 동기화 작업 시작 로그 남기기
                _logger.LogInformation($"Task {taskId} ({taskName}) 동기화 작업을 시작합니다.");

                if (SyncDirection.StartsWith("S2"))
                {
                    selectedProcessor = new DataSyncLogProcessor_Unidirection(_logger, dbConnectionInfoProvider, syncTaskJob);
                    
                    //selectedProcessor = new DataSyncLogProcessorForUpdate(_logger,dbConnectionInfoProvider, syncTaskJob);
                }
                else if (SyncDirection.StartsWith("S1"))
                {
                    selectedProcessor = new DataSyncLogProcessor_Bidirection(_logger, dbConnectionInfoProvider, syncTaskJob);
                }

                else if (SyncDirection.StartsWith("S3"))
                {
                    selectedProcessor = new DataSyncLogProcessor_Update(_logger, dbConnectionInfoProvider, syncTaskJob);
                }
                if (selectedProcessor != null)
                {
                    // SyncManager에 선택된 프로세서 주입 후 실행
                    SyncManager syncManager = new SyncManager(selectedProcessor, _logger);
                    //syncManager.SetDataSyncProcessor(selectedProcessor);
                    try
                    {

                        // 동기화 작업 시작

                        // Job 시작 로그 추가
                        Console.WriteLine($"Task {taskId} ({taskName}) started at {DateTime.Now}");
                        await syncManager.StartSync();
                        // Logger로 동기화 작업 완료 로그 남기기
                        _logger.LogInformation($"Task {taskId} ({taskName}) 동기화 작업이 완료되었습니다.");
                        // Job 완료 로그 추가
                        Console.WriteLine($"Task {taskId} ({taskName}) completed at {DateTime.Now}");
                    }
                    catch (Exception ex)
                    {
                        // SyncManager에서 발생한 예외를 처리
                        // 예외를 JobExecutionException으로 감싸서 Quartz로 던짐
                        //var jobExecutionException = new JobExecutionException($"Task {taskId} ({taskName}) 동기화 중 오류 발생", ex);
                        //jobExecutionException.RefireImmediately = false; // Job이 다시 실행되지 않도록 설정

                        throw new JobExecutionException(ex)
                        {
                            RefireImmediately = false
                        }; // 반드시 이 예외를 던져야 리스너가 인식

                        //throw jobExecutionException;
                        _logger.LogError($"SyncJob - Task {taskId} ({taskName}) 동기화 중 오류 발생: {ex.Message}", ex);
                        // 예외 발생 시 DB에 기록하거나 다른 후속 작업 수행
                        // 예: DB에 오류를 기록하거나 알림을 보냄
                        //SaveErrorToDatabase(taskId, taskName, ex.Message);
                    }
                }
                else
                {
                    _logger.LogWarning($"Task {taskId} ({taskName})의 동기화 방향이 올바르지 않습니다.");
                }
                // Job 완료 로그 추가
                //Console.WriteLine($"Task {taskId} ({taskName}) completed at {DateTime.Now}");
                // 실제 동기화 작업 수행
                //Console.WriteLine($"Task {taskId} ({taskName}) 동기화 작업을 시작합니다.");
                //// 여기에 동기화 로직 추가 (예: DB 동기화, 파일 처리 등)
                //await Task.Delay(1000); // 예시 지연
                //                        // 동기화 작업 수행
                //await PerformSyncTask(taskId, taskName);

                //Console.WriteLine($"Task {taskId} ({taskName}) 동기화 작업이 완료되었습니다.");
            }
            catch (Exception ex)
            {
                // SyncManager에서 발생한 예외를 처리
                //_logger.LogError($"SyncJob - Task {taskId} ({taskName}) 동기화 중 오류 발생: {ex.Message}", ex);
                // 예외 발생 시 DB에 기록하거나 다른 후속 작업 수행
                throw new JobExecutionException(ex);
                Console.WriteLine($"SyncJob - Task 실행 중 예외 발생: {ex.Message}");
            }
        }


        //private Task PerformSyncTask(int taskId, string taskName)
        //{
        //    // 실제 동기화 작업 로직 구현
        //    // 시간이 오래 걸리는 동기화 작업을 수행
        //    return Task.CompletedTask;
        //}

    }    
}
