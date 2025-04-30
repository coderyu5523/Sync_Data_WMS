using System;
using System.Windows.Forms;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SyncCommon;
using SyncLibrary;
namespace DioImplant_batch
{
    static class Program
    {
        /// <summary>
        /// 해당 응용 프로그램의 주 진입점입니다.
        /// </summary>
        [STAThread]
        
        static void Main()
        {
            // WinForms 설정
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // DI 컨테이너 구성
            var serviceCollection = new ServiceCollection();
            

            ConfigureServices(serviceCollection);
            var serviceProvider = serviceCollection.BuildServiceProvider();

            // 메인 폼 시작
            var mainForm = serviceProvider.GetRequiredService<MainForm>();
            Application.Run(mainForm);
        }

        public static string Setting(string ip = "localhost", string id = "sa", string password = "1234", string dbName = "dio_implant", string port = "1433")
        {
            string dbConn = "SERVER=" + ip + "," + port + ";" +
                            "DATABASE=" + dbName + ";" +
                            "UID=" + id + ";" +
                            "PWD=" + password + ";" +
                            "Connection Timeout=10";
            return dbConn;
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            //string src_dbIp = "192.168.10.152";
            //string dbIp = "192.168.10.155";
            //string dbId = "erp";
            //string dbPw = "itsp@7735";
            //string localconnectionString = Setting(dbIp, dbId, dbPw, "smart_db", "1616");

            //dbIp = "192.168.10.155";
            //dbId = "erp";
            //dbPw = "itsp@7735";
            //string proxyconnectionString = Setting(dbIp, dbId, dbPw, "smart_db", "1616");

            
            // DI 컨테이너에 DbConnectionInfoProvider를 싱글톤으로 등록
            services.AddSingleton<DbConnectionInfoProvider>(provider => new DbConnectionInfoProvider(DBConnInfo.GetProxyConnectionString(), DBConnInfo.GetLocalConnectionString()));

            // Logger를 싱글톤으로 등록, DbConnectionInfoProvider를 주입
            services.AddSingleton<Logger>(provider =>
            {
                var dbConnectionInfo = provider.GetRequiredService<DbConnectionInfoProvider>();
                return new Logger(dbConnectionInfo); // DbConnectionInfoProvider를 Logger 생성자에 전달
            });

            
            // Logger를 싱글톤으로 등록 (모든 인스턴스가 동일한 Logger를 사용)
            //services.AddSingleton(connectionString); // 문자열을 DI 컨테이너에 추가
            //services.AddSingleton<Logger>(provider =>
            //{
            //    var connStr = provider.GetRequiredService<string>(); // 연결 문자열을 주입받음
            //    return new Logger(connStr);
            //});

            //services.AddTransient<DataSyncLogProcessor>(provider =>
            //    new DataSyncLogProcessor(provider.GetRequiredService<Logger>(), provider.GetRequiredService<DbConnectionInfoProvider>()));
            services.AddTransient<DataSyncLogProcessorForSave>(provider =>
                new DataSyncLogProcessorForSave(provider.GetRequiredService<Logger>(), provider.GetRequiredService<DbConnectionInfoProvider>()));

            services.AddTransient<DataSyncLogProcessorForTransfer>(provider =>
                new DataSyncLogProcessorForTransfer(provider.GetRequiredService<Logger>(), provider.GetRequiredService<DbConnectionInfoProvider>()));

            // DataSyncLogProcessor 등록 - Logger 인스턴스를 주입받아 생성
            //services.AddTransient<DataSyncLogProcessor>(provider =>
            //{
            //    var logger = provider.GetRequiredService<Logger>();
            //    return new DataSyncLogProcessor(logger);
            //});

            //// DataSyncLogProcessorForSave 등록 - Logger 인스턴스를 주입받아 생성
            //services.AddTransient<DataSyncLogProcessorForSave>(provider =>
            //{
            //    var logger = provider.GetRequiredService<Logger>();
            //    return new DataSyncLogProcessorForSave(logger);
            //});

            //// DataSyncLogProcessorForTransfer 등록 - Logger 인스턴스를 주입받아 생성
            //services.AddTransient<DataSyncLogProcessorForTransfer>(provider =>
            //{
            //    var logger = provider.GetRequiredService<Logger>();
            //    return new DataSyncLogProcessorForTransfer(logger);
            //});


            services.AddSingleton<XmlToSQLScript>();

            // MainForm을 서비스로 등록
            services.AddTransient<MainForm>();

            // SignalR 서비스 추가는 필요하지 않으면 제거
            // services.AddSignalR();
        }
    }
}
