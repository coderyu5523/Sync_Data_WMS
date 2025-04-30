using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;
namespace DioImplant_batch
{
    public class SyncStatusHub : Hub
    {
        // 클라이언트에 메시지를 전송하는 메서드
        public async Task SendStatusUpdate(string status)
        {
            await Clients.All.SendAsync("ReceiveStatusUpdate", status);
        }

        // 성능 지표 전송 메서드
        public async Task SendPerformanceMetrics(string metrics)
        {
            await Clients.All.SendAsync("ReceivePerformanceMetrics", metrics);
        }
    }
}