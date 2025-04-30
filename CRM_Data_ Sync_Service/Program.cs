using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DioImplant_batch; // SyncScheduler�� DataSyncLogProcessor�� ���ǵ� ���ӽ����̽�
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
                    services.AddHostedService<SyncScheduler>(); // SyncScheduler�� ��׶��� ����
                    //services.AddTransient<DataSyncLogProcessor>(); // DataSyncLogProcessor�� Ʈ������Ʈ ���񽺷� ���
                    // DataSyncLogProcessor�� �ʿ��� ���Ӽ� ���� ����
                    services.AddTransient<DataSyncLogProcessor>(provider =>
                    {
                        var hubContext = provider.GetRequiredService<IHubContext<SyncStatusHub>>();
                        var connectionString = "your_connection_string"; // ���� ���� ���ڿ��� ���⿡ �����մϴ�.
                        return new DataSyncLogProcessor(hubContext);
                        //return new DataSyncLogProcessor(hubContext, connectionString);

                    });
                    services.AddSignalR(); // SignalR ���� �߰�
                });

                webBuilder.Configure(app =>
                {
                    app.UseRouting();
                    app.UseStaticFiles(); // ���� ������ �����ϵ��� ����

                    app.UseEndpoints(endpoints =>
                    {
                        endpoints.MapHub<SyncStatusHub>("/syncStatusHub"); // SignalR ��� ��������Ʈ ����
                    });
                });
            });
}
