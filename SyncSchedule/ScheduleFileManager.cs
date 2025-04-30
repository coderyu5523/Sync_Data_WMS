using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;

namespace SyncScheduleManager
{
    public class ScheduleFileManager
    {
        private static readonly string filePath = "sync_schedule.json";

        // SyncSchedule 데이터를 파일로 저장하는 메서드
        public static void SaveSchedule(SyncSchedule schedule)
        {
            var jsonString = JsonSerializer.Serialize(schedule, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, jsonString);
        }

        // SyncSchedule 데이터를 파일에서 불러오는 메서드
        public static SyncSchedule LoadSchedule()
        {
            if (File.Exists(filePath))
            {
                var jsonString = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<SyncSchedule>(jsonString);
            }
            return null; // 파일이 없으면 null 반환
        }
    }
}
