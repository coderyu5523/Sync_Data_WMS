﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace SyncCommon
{
    

    public class DBConnInfo
    {

        //public   string proxyDbIp = "192.168.10.158";
        //public   string proxyDbId = "eis";
        //public   string proxyDbPw = "itsp@7735";
        //public   string proxyDbName = "DIO_WMS";
        //public   string proxyDbPort = "1616";
        public string proxyDbIp ;
        public string proxyDbId ;
        public string proxyDbPw ;
        public string proxyDbName ;
        public string proxyDbPort ;


        // Proxy DB 연결 문자열 반환
        public string GetProxyConnectionString()
        {
            return Setting(proxyDbIp, proxyDbId, proxyDbPw, proxyDbName, proxyDbPort);
        }

        //private static string Setting(string ip = "localhost", string id = "sa", string password = "1234", string dbName = "dio_implant", string port = "1433")
        private static string Setting(string ip, string id, string password, string dbName, string port)
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
