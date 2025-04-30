using SyncScheduleManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using SyncScheduleManager;

namespace Sync_WMSData
{
    public class Task_Schedule_Manger
    {
        /// <summary>
        /// SyncTask JSON 파일에서 데이터를 로드합니다.
        /// </summary>
        /// <returns></returns>
        public static List<SyncTask> LoadTasks()
        {
            if (File.Exists("SyncTask.json"))
            {
                var jsonString = File.ReadAllText("SyncTask.json");
                return JsonSerializer.Deserialize<List<SyncTask>>(jsonString);
            }
            return new List<SyncTask>();
        }

        /// <summary>
        /// ProxyServerInfo JSON 파일에서 데이터를 로드합니다.
        /// </summary>
        /// <returns></returns>
        public static List<SyncSchedule> LoadSchedules()
        {
            if (File.Exists("ProxyServerInfo.json"))
            {
                var jsonString = File.ReadAllText("ProxyServerInfo.json");
                return JsonSerializer.Deserialize<List<SyncSchedule>>(jsonString);
            }
            return new List<SyncSchedule>();
        }
        /// <summary>
        /// Task와 Schedule을 TaskId로 매칭해서 합친 데이터 생성:
        /// </summary>
        /// <returns></returns>
        public static List<CombinedTaskSchedule> CombineTaskAndSchedule()
        {
            var tasks = TaskFileManager.LoadTasks();
            var schedules = ScheduleFileManager.LoadSchedules();

            // TaskId를 기준으로 Task와 Schedule을 매칭
            var combinedTaskSchedules = from task in tasks
                                        where task.IsActive // IsActive가 true인 Task만 선택
                                        join schedule in schedules on task.TaskId equals schedule.TaskId
                                        select new CombinedTaskSchedule
                                        {
                                            Task = task,
                                            Schedule = schedule
                                        };

            return combinedTaskSchedules.ToList();
        }


    }
}
