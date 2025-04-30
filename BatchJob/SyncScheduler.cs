using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection; // 확장 메서드 사용을 위한 using 추가
using System;
using System.Threading;
using System.Threading.Tasks;


namespace DioImplant_batch
{

    public class SyncScheduler : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;

        public SyncScheduler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteSync, null, TimeSpan.Zero, TimeSpan.FromMinutes(100)); // 10분마다 실행
            return Task.CompletedTask;
        }

        private void ExecuteSync(object state)
        {
            using (var scope = _serviceProvider.CreateScope()) // 확장 메서드 CreateScope 사용
            {
                var processor = scope.ServiceProvider.GetRequiredService<DataSyncLogProcessor>();
                processor.Batch_DataGet();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}