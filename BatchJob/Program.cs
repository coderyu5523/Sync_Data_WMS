using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.SignalR;
using SyncCommon;
using SyncScheduler;
using DioImplant_batch;

namespace CRM_Data_Sync_Service
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // WinForms 애플리케이션 설정
            Application.SetHighDpiMode(HighDpiMode.SystemAware); // 고해상도 화면 지원
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // DI 컨테이너 구성
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // 메인 폼에 서비스 제공자 주입
            var mainForm = serviceProvider.GetRequiredService<MainForm>();

            // WinForms 애플리케이션 시작
            Application.Run(mainForm);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            // 필요한 종속성 등록
            services.AddSingleton<DataSyncLogProcessor>(provider =>
            {
                // 필요시 SignalR 허브 컨텍스트 사용
                // var hubContext = provider.GetRequiredService<IHubContext<SyncStatusHub>>();
                var connectionString = "your_connection_string"; // 실제 연결 문자열을 여기에 설정합니다.
                return new DataSyncLogProcessor();
            });

            // SignalR 서비스 추가 (필요시)
            services.AddSignalR();

            // MainForm을 서비스로 등록
            services.AddTransient<MainForm>();
        }
    }
}
