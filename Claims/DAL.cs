using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.OracleClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace ClaimService
{
    class DAL
    {
        private static IDbConnection _db = new OracleConnection(ConfigurationSettings.AppSettings["ConnectionString"].ToString());

        public bool CheckIfFileExist(string FileID, int TransactionSource)
        {
            string cmdText = "Select Count(*) From ECLAIM_BATCH_FILES  WHERE File_ID='" + FileID + "' AND Transaction_Type=1  AND SYS_FILE_SOURCE= " + TransactionSource;
            int num = Convert.ToInt32(_db.ExecuteScalar(cmdText));
            return (num > 0);
        }

        public string WriteEclaimFilesLogToDB(EclaimFile file, int TransactionType, int TransactionStatus, string TransactionError, int SYS_FILE_SOURCE)
        {
            string Result = "";
            try
            {
                string InsertQuery = "INSERT INTO ECLAIM_BATCH_FILES (SYS_ID,FILE_ID, FILE_NAME, SENDER_ID, RECEIVER_ID, TRANSACTION_DATE, " +
                    " RECORD_COUNT, TRANSACTION_TYPE, TRANSACTION_ERROR, FILE_LOCATION, STATUS, SYS_FILE_SOURCE) VALUES " +
                    "((SELECT NVL(MAX(SYS_ID),0)+1 FROM ECLAIM_BATCH_FILES),'" + file.FileID + "','" + Path.GetFileName(file.FileLocation) + "','" + file.SenderID + "', " +
                    "'" + file.ReceiverID + "', " +
                    " To_Date('" + file.TransactionDate.Day + "/" + file.TransactionDate.Month + "/" + file.TransactionDate.Year + "','DD/MM/RRRR')," + file.RecordCount + "," + TransactionType + ", " +
                    " '" + TransactionError + "','" + Path.GetDirectoryName(file.FileLocation) + "'," + TransactionStatus + "," + SYS_FILE_SOURCE + ")";
                _db.Execute(InsertQuery);
                Result = "S";
            }
            catch (Exception e)
            {
                Result = e.Message;
            }
            return Result;
        }

        public void WritePriorAuthorizationRequestFilesLogToDB(EclaimFile file, int TransactionType, int TransactionStatus, string TransactionError, int SYS_FILE_SOURCE)
        {
            string InsertQuery = "INSERT INTO AUTH_BATCH_FILES (SYS_ID,FILE_ID, FILE_NAME, SENDER_ID, RECEIVER_ID, TRANSACTION_DATE, " +
                     " RECORD_COUNT, TRANSACTION_TYPE, TRANSACTION_ERROR, FILE_LOCATION, STATUS, SYS_FILE_SOURCE) VALUES " +
                     "((SELECT NVL(MAX(SYS_ID),0)+1 FROM AUTH_BATCH_FILES),'" + file.FileID + "','" + Path.GetFileName(file.FileLocation) + "','" + file.SenderID + "', " +
                     "'" + file.ReceiverID + "', " +
                     " To_Date('" + file.TransactionDate.Day + "/" + file.TransactionDate.Month + "/" + file.TransactionDate.Year + "','DD/MM/RRRR')," + file.RecordCount + "," + 6 + ", " +
                     " '" + TransactionError + "','" + Path.GetDirectoryName(file.FileLocation) + "'," + TransactionStatus + "," + 6 + ")";
            _db.Execute(InsertQuery);
            
        }

        public List<EclaimBatch> GetEclaimBatchFiles(int SYS_FILE_SOURCE)
        {
            List<EclaimBatch> EclaimBatchFiles = _db.Query<EclaimBatch>(" Select SYS_ID Sys_File_ID,File_Name,File_Location from ECLAIM_BATCH_FILES Where Status = 1 And Transaction_type = 1 And To_Date(Transaction_Date,'DD/MM/RRRR') > To_Date('01/01/2018','DD/MM/RRRR') And Sys_File_Source = " + SYS_FILE_SOURCE + "").ToList();
            return EclaimBatchFiles;
        }

        public long ExecuteScalar(string SqlQuery)
        {
            return Convert.ToInt64(_db.ExecuteScalar(SqlQuery));
        }

        public string ExecuteQuery(string SqlQuery)
        {
            string Result = "";
            try
            {
                _db.Execute(SqlQuery);
                Result = "S";
            }
            catch (Exception e)
            {
                Result = e.Message;
            }
            return Result;
        }

        public DataTable GetDataTable(string SqlQuery)
        {
            OracleConnection Conn = new OracleConnection(ConfigurationSettings.AppSettings["ConnectionString"].ToString());
            Conn.Open();
            DataSet _ds = new DataSet();
            OracleCommand cmd = new OracleCommand(SqlQuery);
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Conn;
            OracleDataAdapter dataAdapter = new OracleDataAdapter();
            dataAdapter.SelectCommand = cmd;
            dataAdapter.Fill(_ds);
            return _ds.Tables[0];
        }

        public DataSet GetDataSet(string SqlQuery)
        {
            OracleConnection Conn = new OracleConnection(ConfigurationSettings.AppSettings["ConnectionString"].ToString());
            Conn.Open();
            DataSet _ds = new DataSet();
            OracleCommand cmd = new OracleCommand(SqlQuery);
            cmd.CommandType = CommandType.Text;
            cmd.Connection = Conn;
            OracleDataAdapter dataAdapter = new OracleDataAdapter();
            dataAdapter.SelectCommand = cmd;
            dataAdapter.Fill(_ds);
            return _ds;
        }

        public DataSet GetClaims(string PaymentNo, int SYS_FILE_SOURCE)
        {
            string sqlquery1 = "Select * From ECLAIMS Where SYS_CLAIM_STATUS=3 and Payment_Reference = '" + PaymentNo + "' and SYS_CLAIM_SOURCE=" + SYS_FILE_SOURCE + " Order By SENDER_ID";
            string sqlquery2 = "select * FROM eclaim_activities Where sys_claim_id in (select sys_claim_id FROM eclaims WHERE sys_claim_status=3 and Payment_Reference = '" + PaymentNo + "' AND  SYS_CLAIM_SOURCE=" + SYS_FILE_SOURCE + ")";
            DataTable Table1 = GetDataTable(sqlquery1);
            Table1.TableName = "EClaim";
            DataTable Table2 = GetDataTable(sqlquery2);
            Table2.TableName = "EClaim_Activity";
            DataSet ds = new DataSet();
            ds.Tables.Add(Table1.Copy());
            ds.Tables.Add(Table2.Copy());
            return ds;
        }
    }
}
