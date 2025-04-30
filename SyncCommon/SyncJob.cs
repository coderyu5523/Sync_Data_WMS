using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using SyncLibrary;
using SyncCommon;
using Microsoft.Extensions.Logging;
namespace Sync_CRMData
{
    [DisallowConcurrentExecution]  // 동일 작업의 중복 실행 방지
    public class SyncJob : IJob
    {
        private readonly ILogger<SyncJob> _logger;

        // 생성자에서 Logger 주입
        public SyncJob(ILogger<SyncJob> logger)
        {
            _logger = logger;
        }

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
                

                // Logger로 동기화 작업 시작 로그 남기기
                _logger.LogInformation($"Task {taskId} ({taskName}) 동기화 작업을 시작합니다.");


                DbConnectionInfoProvider dbConnectionInfoProvider = new DbConnectionInfoProvider(syncTaskJob.SourceDB);

                
                if (SyncDirection.StartsWith("Bi"))
                {
                    selectedProcessor = new DataSyncLogProcessorForUpdate(_logger,dbConnectionInfoProvider, syncTaskJob);
                }
                else 
                {
                    selectedProcessor = new DataSyncLogProcessorForTransfer(_logger,dbConnectionInfoProvider, syncTaskJob);
                }

                if (selectedProcessor != null)
                {
                    // SyncManager에 선택된 프로세서 주입 후 실행
                    SyncManager syncManager = new SyncManager(selectedProcessor, _logger);
                    //syncManager.SetDataSyncProcessor(selectedProcessor);

                    // 동기화 작업 시작
                    Console.WriteLine($"Task {taskId} ({taskName}) 동기화 작업을 시작합니다.");
                    await syncManager.StartSync();
                    // Logger로 동기화 작업 완료 로그 남기기
                    _logger.LogInformation($"Task {taskId} ({taskName}) 동기화 작업이 완료되었습니다.");
                }
                else
                {
                    _logger.LogWarning($"Task {taskId} ({taskName})의 동기화 방향이 올바르지 않습니다.");
                }

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
                Console.WriteLine($"SyncJob - Task 실행 중 예외 발생: {ex.Message}");
            }
        }
         

        private Task PerformSyncTask(int taskId, string taskName)
        {
            // 실제 동기화 작업 로직 구현
            // 시간이 오래 걸리는 동기화 작업을 수행
            return Task.CompletedTask;
        }
    }
}
