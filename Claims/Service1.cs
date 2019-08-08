using Dapper;
using MimeKit;
using MailKit.Net.Smtp;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Data.OracleClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using WinSCP;
using System.Net;
using System.Xml;

namespace ClaimService
{
    public partial class Service1 : ServiceBase
    {
        private Thread _thread;
        private int ThreadTimeLimit = 1;
        //private static IDbConnection _db = new OracleConnection(ConfigurationSettings.AppSettings["ConnectionString"].ToString());

        public Service1()
        {
            string TimeLimit = string.Empty;
            int HourLimit = Convert.ToInt32(ConfigurationSettings.AppSettings["HOURLIMIT"].ToString());
            if (string.IsNullOrEmpty(TimeLimit))
                TimeLimit = "12";
            ThreadTimeLimit = (Convert.ToInt16(TimeLimit) * HourLimit);
            Execute();
        }

        protected override void OnStart(string[] args)
        {
            this._thread = new Thread(new ThreadStart(this.Execute));
            this._thread.Start();
        }


        private void Execute()
        {
            List<string> Errors = new List<string>();
            bool doprocess = false;
            try
            {
                while (!doprocess)
                {
                    DateTime CurrentDate = DateTime.Now;
                    DateTime SDate = DateTime.Now.AddDays(-500);
                    DateTime EDate = DateTime.Now.AddDays(-499);
                    int DaysCount = Convert.ToInt16((CurrentDate - SDate).TotalDays) + 1;

                    for (int ds = 0; ds < DaysCount; ds++)
                    { 
                        SDate = SDate.AddDays(1);
                        EDate = EDate.AddDays(1);
                        DownloadClaims Obj = new DownloadClaims();
                        Obj.DownloadDHAAuthorization(SDate.ToShortDateString(), EDate.ToShortDateString(), 1);
                        Obj.DownloadDHAAuthorization(SDate.ToShortDateString(), EDate.ToShortDateString(), 2);
                        //Obj.DownloadDHAClaims(SDate.ToShortDateString(), EDate.ToShortDateString(), 1);// New DHA Claims
                        //Obj.DownloadDHAClaims(SDate.ToShortDateString(), EDate.ToShortDateString(), 2); // Downloaded DHA Claims
                        //Obj.InsertClaims(1); // Insert DHA Claims
                        //Obj.DownloadHAADClaims(SDate.ToShortDateString(), EDate.ToShortDateString(), 1);// New HAAD Claims
                        //Obj.DownloadHAADClaims(SDate.ToShortDateString(), EDate.ToShortDateString(), 2);// Downloaded HAAD Claims
                        //Obj.InsertClaims(2); // Insert HAAD Claims
                        //UploadClaims Obj1 = new UploadClaims();
                        //Obj1.UploadDHAClaims();
                        //Obj1.UploadHAADClaims(); 
                    }
                    Thread.Sleep(ThreadTimeLimit);
                }
            }
            catch
            {
            }
        }

        protected override void OnStop()
        {
            if (this._thread != null)
            {
                this._thread.Abort();
                this._thread.Join();
            }
        }


    }


}
