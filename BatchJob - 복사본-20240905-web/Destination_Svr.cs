using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DioImplant_batch
{
    public class Destination_Svr
    {
        private string ip;
        private string id;
        private string password;
        private string dbName;
        private string port;

        private string dbConn;
        private SqlConnection sqlConnection = null;

        // Local DB 접속정보 설정
        public void SetSetting(string ip = "localhost", string id = "sa", string password = "1234",
            string dbName = "dio_implant", string port = "1433")
        {
            this.ip = ip;
            this.id = id;
            this.password = password;
            this.dbName = dbName;
            this.port = port;

            dbConn =
                "SERVER=" + this.ip + "," + this.port + ";" +
                "DATABASE=" + this.dbName + ";" +
                "UID=" + this.id + ";" +
                "PWD=" + this.password + ";" +
                "Connection Timeout=10";
        }


        // Local DB 연결
        private SqlConnection Connect()
        {
            if (sqlConnection == null)
            {
                sqlConnection = new SqlConnection(dbConn);
                sqlConnection.Open();
            }

            return sqlConnection;
        }

        // Local DB 연결해제
        private void DisConnect()
        {
            if (sqlConnection != null)
            {
                sqlConnection.Close();
                sqlConnection = null;
            }
        }

        // 다건 조회
        public DataTable ExecuteCommandMulti(string cmd)
        {
            DataTable dt = new DataTable();
            SqlConnection conn = Connect();
            SqlCommand command = new SqlCommand(cmd, conn);

            try
            {
                SqlDataAdapter sda = new SqlDataAdapter(command);
                sda.Fill(dt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            DisConnect();
            return dt;
        }

        // 단건 조회
        public T ExecuteCommandSingle<T>(string cmd)
        {
            object scalarValue;

            SqlConnection conn = Connect();
            SqlCommand command = new SqlCommand(cmd, conn);

            try
            {
                scalarValue = command.ExecuteScalar();
            }
            catch (Exception ex)
            {
                scalarValue = default(T);
                Debug.WriteLine(ex);
            }

            DisConnect();
            return (T)scalarValue;
        }

        // DML 사용
        public bool ExecuteCommandDML(string cmd)
        {
            bool ret = false;

            SqlConnection conn = Connect();
            SqlCommand command = new SqlCommand(cmd, conn);

            try
            {
                command.ExecuteNonQuery();
                ret = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            DisConnect();
            return ret;
        }

        // DML 사용(트랜잭션)
        public bool ExecuteCommandTranDML(string cmd)
        {
            bool ret = false;

            SqlConnection conn = Connect();
            SqlTransaction tran = conn.BeginTransaction();
            SqlCommand command = new SqlCommand(cmd, conn, tran);

            try
            {
                command.ExecuteNonQuery();
                tran.Commit();
                ret = true;
            }
            catch (Exception ex)
            {
                tran.Rollback();
                DisConnect();
                throw ex;
            }

            DisConnect();
            return ret;
        }
    }
}
