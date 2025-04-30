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

namespace Sync_CRMData
{
    public partial class frmSync_Monitoring : Form
    {
        public frmSync_Monitoring()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Start_Sync_CRMData();
        }
        private async void Start_Sync_CRMData()
        {
            // 스케줄링 로직 호출
            await StartScheduling();

            // UI 피드백 (상태 표시)
            toolStripLabel1.Text = "스케줄러 시작됨";
        }

        // Quartz.NET 스케줄링 시작 메서드
        public static async Task StartScheduling()
        {
            // Quartz 스케줄러 팩토리 생성
            ISchedulerFactory schedulerFactory = new StdSchedulerFactory();
            IScheduler scheduler = await schedulerFactory.GetScheduler();

            // 스케줄러 시작
            await scheduler.Start();

            // 합쳐진 Task와 Schedule 불러오기
            var combinedTaskSchedules = Task_Schedule_Manger.CombineTaskAndSchedule();

            foreach (var combined in combinedTaskSchedules)
            {
                var task = combined.Task;
                var schedule = combined.Schedule;

                // Quartz.NET Job 생성
                IJobDetail job = JobBuilder.Create<SyncJob>()
                    .WithIdentity($"job_{task.TaskId}", "group1")
                    .UsingJobData("TaskId", task.TaskId)
                    .UsingJobData("TaskName", task.TaskName)
                    .UsingJobData("ProcedureName", task.ProcedureName)
                    .UsingJobData("SourceDB", task.SourceDB)
                    .UsingJobData("TargetDB", task.TargetDB)
                    .UsingJobData("SyncDirection", task.SyncDirection)
                    .Build();

                // Trigger 생성 (Recurring or OneTime)
                ITrigger trigger;

                if (schedule.ScheduleType == "Recurring" && schedule.Interval != null)
                {
                    // 주기적인 동기화 설정
                    trigger = TriggerBuilder.Create()
                        .WithIdentity($"trigger_{task.TaskId}", "group1")
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
                        .WithIdentity($"trigger_{task.TaskId}", "group1")
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

    }
}
