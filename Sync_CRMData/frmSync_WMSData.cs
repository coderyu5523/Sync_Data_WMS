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
using Sync_WMSData.SyncMonitoring;
using Quartz.Impl.Matchers;

namespace Sync_WMSData
{
    public partial class frmSync_WMSData : Form
    {
        private Main_Sync_WMSData mdiParentForm; // MDI 부모 폼 참조
        public frmSync_WMSData(Main_Sync_WMSData parentForm)
        {
            InitializeComponent();
            // MDI 자식 폼을 최대화 상태로 설정
            this.WindowState = FormWindowState.Maximized;
            mdiParentForm = parentForm; // 부모 폼 저장
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Start_Sync_WMSData();
        }
        private async void Start_Sync_WMSData()
        {
            // 부모 폼을 통해 스케줄러 실행
            IScheduler scheduler = mdiParentForm.GetScheduler();

            if (scheduler == null)
            {
                MessageBox.Show("스케줄러가 실행 중이지 않습니다.");
                return;
            }
            // 스케줄링 로직 호출
            await StartScheduling_Add(scheduler);

            // UI 피드백 (상태 표시)
            toolStripStatusLabel1.Text = "스케줄러 시작됨";
        }
        //StartScheduling_Add
        // 스케줄링 작업 설정 메서드
        public static async Task StartScheduling(IScheduler scheduler)
        {
            scheduler.ListenerManager.AddJobListener(new MyJobListener((taskId, taskName, startTime, status, message, duration, procedureName, sourceDB, targetDB, syncDirection, referenceTables,targetTable) =>
            {
                // Job이 실행된 후의 콜백 처리 로직을 여기에 작성
                Console.WriteLine($"Task {taskId} ({taskName}) 실행 완료. 상태: {status}, 메시지: {message}");
            }));
            // 리스너를 먼저 스케줄러에 등록
            //scheduler.ListenerManager.AddJobListener(new MyJobListener());

            // 합쳐진 Task와 Schedule 불러오기
            var combinedTaskSchedules = Task_Schedule_Manger.CombineTaskAndSchedule();

            foreach (var combined in combinedTaskSchedules)
            {
                var task = combined.Task;
                var schedule = combined.Schedule;

                // JobKey 생성
                JobKey jobKey = new JobKey($"job_{task.TaskId}", "group1");

                // 기존에 동일한 JobKey가 있는지 확인
                if (await scheduler.CheckExists(jobKey))
                {
                    Console.WriteLine($"Job {jobKey} 이미 존재합니다. 덮어쓰거나 건너뜁니다.");
                    await scheduler.DeleteJob(jobKey); // 기존 Job을 삭제
                }

                // Quartz.NET Job 생성
                IJobDetail job = JobBuilder.Create<SyncJob>()
                   .WithIdentity(jobKey)
                    .UsingJobData("TaskId", task.TaskId)
                    .UsingJobData("TaskName", task.TaskName)
                    .UsingJobData("ProcedureName", task.ProcedureName)
                    .UsingJobData("SourceDB", task.SourceDB)
                    .UsingJobData("TargetDB", task.TargetDB)
                    .UsingJobData("ReferenceTables", string.Join(",", task.ReferenceTables))
                    .UsingJobData("TargetTable", task.TargetTable)
                    .UsingJobData("ProcedureName", task.ProcedureName)
                    .UsingJobData("ScheduleType", schedule.ScheduleType) // 스케줄 정보 추가
                    .UsingJobData("Interval", schedule.Interval?.ToString()) // 스케줄 주기 추가
                    .UsingJobData("SyncDirection", task.SyncDirection) // 스케줄 주기 추가
                                                                             //.UsingJobData("SpecificTime", schedule.SpecificTime?.ToString()) // 특정 시간 추가
                    .Build();

                // TriggerKey 생성
                TriggerKey triggerKey = new TriggerKey($"trigger_{task.TaskId}", "group1");
                // 기존에 동일한 TriggerKey가 있는지 확인
                if (await scheduler.CheckExists(triggerKey))
                {
                    Console.WriteLine($"Trigger {triggerKey} 이미 존재합니다. 덮어쓰거나 건너뜁니다.");
                }

                // Trigger 생성 (Recurring or OneTime)
                ITrigger trigger;

                if (schedule.ScheduleType == "Recurring" && schedule.Interval != null)
                {
                    trigger = TriggerBuilder.Create()
                       .WithIdentity(triggerKey)
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithInterval(TimeSpan.Parse(schedule.Interval.ToString()))
                            .RepeatForever())
                        .Build();
                }
                else if (schedule.ScheduleType == "OneTime" && schedule.SpecificTime != null)
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .StartAt(DateTimeOffset.Parse(schedule.SpecificTime.ToString()))
                        .Build();
                }
                else if (schedule.ScheduleType == "Daily" && schedule.SpecificTime != null)
                {
                    // 매일 특정 시간에 실행
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithCronSchedule($"0 {schedule.SpecificTime.Value.Minute} {schedule.SpecificTime.Value.Hour} * * ?")
                        .Build();
                }
                else
                {
                    // 유효한 스케줄이 없는 경우 건너뜀
                    Console.WriteLine($"Task {task.TaskId}: 유효한 동기화 스케줄이 없습니다.");
                    continue;
                }

                // 스케줄러에 Job과 Trigger 등록
                await scheduler.ScheduleJob(job, trigger);
                Console.WriteLine($"Task {task.TaskId} 스케줄링 완료.");
            }
        }

        public static async Task StartScheduling_Add(IScheduler scheduler)
        {
            scheduler.ListenerManager.AddJobListener(new MyJobListener((taskId, taskName, startTime, status, message, duration, procedureName, sourceDB, targetDB, syncDirection, referenceTables, targetTable) =>
            {
                Console.WriteLine($"Task {taskId} ({taskName}) 실행 완료. 상태: {status}, 메시지: {message}");
            }));

            // 기존에 스케줄된 작업 목록을 가져오기 위해 현재 스케줄러에서 JobKeys 조회
            var existingJobKeys = await scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());

            var combinedTaskSchedules = Task_Schedule_Manger.CombineTaskAndSchedule();

            foreach (var combined in combinedTaskSchedules)
            {
                var task = combined.Task;
                var schedule = combined.Schedule;

                JobKey jobKey = new JobKey($"job_{task.TaskId}", "group1");

                // 기존 작업 중에서 추가된 작업만 처리
                if (existingJobKeys.Contains(jobKey))
                {
                    Console.WriteLine($"Job {jobKey} 이미 존재합니다. 건너뜁니다.");
                    continue; // 이미 존재하는 작업은 스킵
                }

                IJobDetail job = JobBuilder.Create<SyncJob>()
                   .WithIdentity(jobKey)
                   .UsingJobData("TaskId", task.TaskId)
                   .UsingJobData("TaskName", task.TaskName)
                   .UsingJobData("ProcedureName", task.ProcedureName)
                   .UsingJobData("SourceDB", task.SourceDB)
                   .UsingJobData("TargetDB", task.TargetDB)
                   .UsingJobData("ReferenceTables", string.Join(",", task.ReferenceTables))
                   .UsingJobData("TargetTable", task.TargetTable)
                   .UsingJobData("ProcedureName", task.ProcedureName)
                   .UsingJobData("ScheduleType", schedule.ScheduleType)
                   .UsingJobData("Interval", schedule.Interval?.ToString())
                   .UsingJobData("SyncDirection", task.SyncDirection)
                   .Build();

                TriggerKey triggerKey = new TriggerKey($"trigger_{task.TaskId}", "group1");

                // Trigger 생성
                ITrigger trigger;

                if (schedule.ScheduleType == "Recurring" && schedule.Interval != null)
                {
                    if (task.TaskId.ToString() == "9")
                    {
                        // 특정 TaskId에 대해서만 1분 늦게 시작
                        DateTimeOffset startTime = DateTimeOffset.Now.AddMinutes(1);

                        // 트리거 생성
                        trigger = TriggerBuilder.Create()
                            .WithIdentity(triggerKey)
                            .StartAt(startTime)  // 1분 후에 시작
                            .WithSimpleSchedule(x => x
                                .WithInterval(TimeSpan.Parse(schedule.Interval.ToString())) // interval 지정
                                .RepeatForever())  // 반복
                            .Build();
                    }
                    else
                    {
                        trigger = TriggerBuilder.Create()
                           .WithIdentity(triggerKey)
                           .StartNow()
                           .WithSimpleSchedule(x => x
                               .WithInterval(TimeSpan.Parse(schedule.Interval.ToString()))
                               .RepeatForever())
                        .Build();
                    }
                }
                else if (schedule.ScheduleType == "OneTime" && schedule.SpecificTime != null)
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .StartAt(DateTimeOffset.Parse(schedule.SpecificTime.ToString()))
                        .Build();
                }
                else if (schedule.ScheduleType == "Daily" && schedule.SpecificTime != null)
                {
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .WithCronSchedule($"0 {schedule.SpecificTime.Value.Minute} {schedule.SpecificTime.Value.Hour} * * ?")
                        .Build();
                }
                else
                {
                    Console.WriteLine($"Task {task.TaskId}: 유효한 스케줄이 없습니다.");
                    continue;
                }

                // 스케줄러에 Job과 Trigger 등록
                await scheduler.ScheduleJob(job, trigger);
                Console.WriteLine($"Task {task.TaskId} 스케줄링 완료.");
            }
        }

        // Quartz.NET 스케줄링 시작 메서드
        public static async Task StartScheduling1(IScheduler scheduler)
        {
            // Quartz 스케줄러 팩토리 생성
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            //IScheduler scheduler = await schedulerFactory.GetScheduler();

            // MyJobListener를 스케줄러에 등록
            //MyJobListener는 Quartz 스케줄러에 등록되어야 합니다.스케줄러에서 작업 실행 전후의 이벤트를 감지하기 위해, 스케줄러 시작 시 MyJobListener를 등록해야 합니다.
           // scheduler.ListenerManager.AddJobListener(new MyJobListener());

            // 스케줄러 시작
            await scheduler.Start();

            // 합쳐진 Task와 Schedule 불러오기
            var combinedTaskSchedules = Task_Schedule_Manger.CombineTaskAndSchedule();
            // JobKey 생성
            

            foreach (var combined in combinedTaskSchedules)
            {
                var task = combined.Task;
                var schedule = combined.Schedule;
                JobKey jobKey = new JobKey($"job_{task.TaskId}", "group1");
                // 기존에 동일한 JobKey가 있는지 확인
                if (await scheduler.CheckExists(jobKey))
                {
                    Console.WriteLine($"Job {jobKey} 이미 존재합니다. 덮어쓰거나 건너뜁니다.");
                    await scheduler.DeleteJob(jobKey); // 기존 Job을 삭제
                }

                // Quartz.NET Job 생성
                IJobDetail job = JobBuilder.Create<SyncJob>()
                    .WithIdentity(jobKey)
                    .UsingJobData("TaskId", task.TaskId)
                    .UsingJobData("TaskName", task.TaskName)
                    .UsingJobData("ProcedureName", task.ProcedureName)
                    .UsingJobData("SourceDB", task.SourceDB)
                    .UsingJobData("TargetDB", task.TargetDB)
                    .UsingJobData("SyncDirection", task.SyncDirection)
                    .Build();

                // Trigger 생성 (Recurring or OneTime)
                ITrigger trigger;
                // TriggerKey 생성
                TriggerKey triggerKey = new TriggerKey($"trigger_{task.TaskId}", "group1");
                // 기존에 동일한 TriggerKey가 있는지 확인
                if (await scheduler.CheckExists(triggerKey))
                {
                    Console.WriteLine($"Trigger {triggerKey} 이미 존재합니다. 덮어쓰거나 건너뜁니다.");
                }

                if (schedule.ScheduleType == "Recurring" && schedule.Interval != null)
                {
                    // 주기적인 동기화 설정
                    trigger = TriggerBuilder.Create()
                         .WithIdentity(triggerKey)
                        .StartNow()
                        .WithSimpleSchedule(x => x
                            .WithInterval(TimeSpan.Parse(schedule.Interval.ToString()))
                            .RepeatForever())
                        .Build();
                }
                else if (schedule.ScheduleType == "OneTime" && schedule.SpecificTime != null)
                {
                    // 특정 시간에 한 번만 실행
                    trigger = TriggerBuilder.Create()
                        .WithIdentity(triggerKey)
                        .StartAt(DateTimeOffset.Parse(schedule.SpecificTime.ToString()))
                        .Build();
                }
                else
                {
                    // 유효한 스케줄이 없는 경우 건너뜀
                    Console.WriteLine($"Task {task.TaskId}: 유효한 동기화 스케줄이 없습니다.");
                    continue;
                }

                // 스케줄러에 Job과 Trigger 등록
                await scheduler.ScheduleJob(job, trigger);
                Console.WriteLine($"Task {task.TaskId} 스케줄링 완료.");
            }
        }

        private async void btn_Task_Exec_Click(object sender, EventArgs e)
        {
            int taskId;
            if (int.TryParse(txt_TaskID.Text, out taskId))
            {
                IScheduler scheduler = mdiParentForm.GetScheduler();
                await TriggerSpecificTaskAsync(scheduler, taskId);
            }
            else
            {
                MessageBox.Show("유효한 Task ID를 입력하세요.");
            }
        }
        public async Task TriggerSpecificTaskAsync(IScheduler scheduler, int taskId)
        {
            // JobKey로 특정 Task를 식별
            JobKey jobKey = new JobKey($"job_{taskId}", "group1");
            Console.WriteLine(jobKey);


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

            // 스케줄러에 해당 JobKey가 있는지 확인
            if (await scheduler.CheckExists(jobKey))
            {

                // Job을 즉시 실행할 트리거 생성
                ITrigger immediateTrigger = TriggerBuilder.Create()
                    .WithIdentity($"immediate_trigger_{taskId}", "group1")
                    .StartNow()  // 즉시 실행
                    .Build();

                // 해당 Job을 즉시 실행
                await scheduler.TriggerJob(jobKey);

                Console.WriteLine($"Task {taskId}가 즉시 실행되었습니다.");
            }
            else
            {

                Console.WriteLine($"Task {taskId}를 찾을 수 없습니다.");
            }
        }
    }
}
