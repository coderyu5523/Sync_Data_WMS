using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SyncCommon
{
    

    public class DBConnInfo
    {


        //string src_dbIp = "192.168.10.152";
        //private static readonly string dbIp = "192.168.10.155";
        //private static readonly string dbId = "erp";
        //private static readonly string dbPw = "itsp@7735";
        //private static readonly string dbName = "smart_db";
        //private static readonly string dbPort = "1616";

        
        public   string proxyDbIp = "192.168.10.158";
        public   string proxyDbId = "eis";
        public   string proxyDbPw = "itsp@7735";
        public   string proxyDbName = "DIO_WMS";
        public   string proxyDbPort = "1616";

        //string dbId = "erp";
        //string dbPw = "itsp@7735";
        //string localconnectionString = Setting(dbIp, dbId, dbPw, "smart_db", "1616");

        //dbIp = "192.168.10.155";
        //    dbId = "erp";
        //    dbPw = "itsp@7735";
        // Primary DB 연결 문자열 반환

        //public static string GetLocalConnectionString()
        //{
        //    return Setting(dbIp, dbId, dbPw, dbName, dbPort);
        //}

        // Proxy DB 연결 문자열 반환
        public string GetProxyConnectionString()
        {
            return Setting(proxyDbIp, proxyDbId, proxyDbPw, proxyDbName, proxyDbPort);
        }

        private static string Setting(string ip = "localhost", string id = "sa", string password = "1234", string dbName = "dio_implant", string port = "1433")
        {
            string dbConn = "SERVER=" + ip + "," + port + ";" +
                            "DATABASE=" + dbName + ";" +
                            "UID=" + id + ";" +
                            "PWD=" + password + ";" +
                            "Connection Timeout=10";
            return dbConn;
        }

    }

}
