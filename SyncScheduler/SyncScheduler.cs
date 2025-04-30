using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;
using SyncCommon; // 공통 모듈 참조

namespace ProxyServerInfor
{
    public class ProxyServerInfor : IHostedService, IDisposable
    {
        private Timer _timer;
        private readonly IServiceProvider _serviceProvider;
        private readonly int _syncIntervalMinutes; // 동기화 주기(분 단위)

        public ProxyServerInfor(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _syncIntervalMinutes = 10; // 기본 동기화 주기 설정 (10분)
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _timer = new Timer(ExecuteSyncTasks, null, TimeSpan.Zero, TimeSpan.FromMinutes(_syncIntervalMinutes));
            return Task.CompletedTask;
        }

        private void ExecuteSyncTasks(object state)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    // SyncSourceToTarget의 동기화 서비스 호출
                    //var sourceToTargetSyncService = scope.ServiceProvider.GetRequiredService<SourceToTargetSyncService>();
                    //sourceToTargetSyncService.SyncData();

                    //// SyncTargetToSource의 동기화 서비스 호출
                    //var targetToSourceSyncService = scope.ServiceProvider.GetRequiredService<TargetToSourceSyncService>();
                    //targetToSourceSyncService.SyncData();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during synchronization: {ex.Message}");
                }
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
