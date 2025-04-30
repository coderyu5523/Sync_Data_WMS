using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DioImplant_batch; // SyncScheduler와 DataSyncLogProcessor가 정의된 네임스페이스
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;

public class Program
{
    public static void Main(string[] args)
    {
        CreateHostBuilder(args).Build().Run();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<SyncScheduler>(); // SyncScheduler는 백그라운드 서비스
                    //services.AddTransient<DataSyncLogProcessor>(); // DataSyncLogProcessor는 트랜지언트 서비스로 등록
                    // DataSyncLogProcessor에 필요한 종속성 주입 설정
                    services.AddTransient<DataSyncLogProcessor>(provider =>
                    {
                        var hubContext = provider.GetRequiredService<IHubContext<SyncStatusHub>>();
                        var connectionString = "your_connection_string"; // 실제 연결 문자열을 여기에 설정합니다.
                        return new DataSyncLogProcessor(hubContext);
                        //return new DataSyncLogProcessor(hubContext, connectionString);

                    });
                    services.AddSignalR(); // SignalR 서비스 추가
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseStaticFiles(); // 정적 파일을 서빙하도록 설정

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<SyncStatusHub>("/syncStatusHub"); // SignalR 허브 엔드포인트 설정
                    });
                });
            });
}
