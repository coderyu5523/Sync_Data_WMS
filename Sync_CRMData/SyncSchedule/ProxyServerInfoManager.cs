using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Quartz;
using System.Windows.Forms;

namespace SyncScheduleManager
{
    public class ProxyServerInfoManager
    {
        //private static readonly string filePath = "sync_schedule.json";
        //private static readonly string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "sync_schedule.json");

        private static string baseDirectory = @"C:\Sync_WMSData";
        private static readonly string filePath =Path.Combine(baseDirectory, "config", "proxy_serverinfo.json");


        // ProxyServerInfo 데이터를 파일로 저장하는 메서드
        public static ProxyServerInfo LoadSeverInfo()
        {
            if (File.Exists(filePath))
            {
                var jsonString = File.ReadAllText(filePath);
                try
                {
                    // JSON 데이터가 배열인 경우 처리
                    var serverInfoList = JsonSerializer.Deserialize<List<ProxyServerInfo>>(jsonString);

                    if (serverInfoList != null && serverInfoList.Count > 0)
                    {
                        return serverInfoList[0]; // 첫 번째 객체 반환
                    }
                }
                catch (JsonException)
                {
                    // JSON 데이터가 단일 객체일 가능성을 재확인
                    try
                    {
                        return JsonSerializer.Deserialize<ProxyServerInfo>(jsonString);
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON 변환 실패: {ex.Message}");
                    }
                }
            }
            MessageBox.Show("Proxy서버 정보가 저장되지 않았습니다. Proxy DB서버 연결정보를 확인하세요.");
            return null;
        }

        public static void SaveProxyServieInfo(ProxyServerInfo serverInfo)
        {
            List<ProxyServerInfo> serverInfos = new List<ProxyServerInfo>();
            // 기존 파일이 있는지 확인하고, 있으면 불러오기
            if (File.Exists(filePath))
            {
                var existingJson = File.ReadAllText(filePath);

                // 기존 스케줄 리스트를 JSON에서 역직렬화
                serverInfos = JsonSerializer.Deserialize<List<ProxyServerInfo>>(existingJson);
            }

            // 동일한 TaskId가 있는지 확인
            var existingserverInfo = serverInfos.FirstOrDefault(s => s.ServerIP == serverInfo.ServerIP);

            if (existingserverInfo != null)
            {
                // 동일한 TaskId가 있으면 기존 스케줄을 업데이트
                existingserverInfo.dbid = serverInfo.dbid;
                existingserverInfo.dbpwd = serverInfo.dbpwd;
                existingserverInfo.dbname = serverInfo.dbname;
                existingserverInfo.dbport = serverInfo.dbport;
                
                // 다른 필드들도 필요한 경우 업데이트
            }
            else
            {
                // TaskId가 중복되지 않으면 새 스케줄 추가
                serverInfos.Add(serverInfo);
            }

            // 업데이트된 리스트를 다시 JSON으로 직렬화하여 파일에 저장
            var updatedJson = JsonSerializer.Serialize(serverInfos, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, updatedJson);
            MessageBox.Show("Proxy 서버 정보가 저장되었습니다.");
        }


    }
}
