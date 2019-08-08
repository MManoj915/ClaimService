using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Threading.Tasks;
using System.IO;
using ClaimService.DHPO;
using ClaimService.HAAD;

namespace ClaimService
{
    class UploadClaims
    {
        ValidateTransactions DHPOSrv = new ValidateTransactions();
        Webservices HAADSrv = new Webservices();

        public void UploadDHAClaims()
        {
            int[] eclaimIDs;
            string[] UploadResult;
            double PaymentAmount;
            DataTable Headers = new DataTable();
            DataTable PaymentHeaders = new DataTable();
            List<string> FileLocation = new List<string>();
            DataView view;
            DataView paymentview;
            DAL Obj = new DAL();
            try
            {
                DataSet dsp = Obj.GetDataSet("Select Distinct Payment_Reference From EClaims Where Sys_Claim_Status = 3 And Sys_Claim_Source = 1");
                if (dsp.Tables.Count > 0)
                {
                    paymentview = new DataView(dsp.Tables[0]);
                    PaymentHeaders = paymentview.ToTable(true, "PAYMENT_REFERENCE");
                }
                foreach (DataRow paymentfile in PaymentHeaders.Rows)
                {
                    DataSet ds = Obj.GetClaims(paymentfile["PAYMENT_REFERENCE"].ToString(), 1);
                    if (ds.Tables.Count > 0)
                    {
                        view = new DataView(ds.Tables[0]);
                        Headers = view.ToTable(true, "SENDER_ID");
                    }

                    foreach (DataRow file in Headers.Rows)
                    {
                        EclaimFile efile = new EclaimFile();
                        efile = GenarateDHAXmlRemittanceFile(file["SENDER_ID"].ToString(), ds, out eclaimIDs, out PaymentAmount);
                        if (efile.TransactionError == "1")//1= File Generated Successfully
                        {
                            try
                            {
                                efile.FileLocation = WriteGeneratedXmlFile(efile.ReceiverID, efile.SenderID, efile.XmlData, efile.TransactionDate.ToString("yyyyMMdd"), 1);
                                efile.FileName = Path.GetFileName(efile.FileLocation);
                                efile.FileID = "1";
                                //log File To database 
                                Obj.WriteEclaimFilesLogToDB(efile, 2, 1, "", 1);

                                //Upload Transaction File, Log File to db and Relate between File and it's claims
                                UploadResult = UploadDHATransactionFile(efile);

                                if (Convert.ToInt64(UploadResult[0]) == 1 && Convert.ToInt64(UploadResult[1]) == 4) // Upload or Resubmission Success
                                {
                                    foreach (var ID in eclaimIDs)
                                    {
                                        Obj.ExecuteQuery("Update Eclaims Set Sys_Claim_Status = 4,RAUploadDate = SysDate Where Sys_Claim_ID = " + ID);
                                    }
                                }
                                else
                                {
                                    foreach (var ID in eclaimIDs)
                                    {
                                        Obj.ExecuteQuery("Update Eclaims Set Sys_Claim_Status = 5,RAUploadDate = Null Where Sys_Claim_ID = " + ID);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public EclaimFile GenarateDHAXmlRemittanceFile(string ReceiverID, DataSet ds, out int[] eclaimID, out double PaymentAmount)
        {
            EclaimFile file = new EclaimFile();
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            string path = "remittanceFile" + DateTime.Now.ToString("yyyyMMddHHmmssss") + ".xml";
            string str3 = DateTime.Now.ToString(ConfigurationSettings.AppSettings["UploadDateFormat"]);
            string str = ConfigurationSettings.AppSettings["DispositionFlag"].ToString();
            int num = 0;
            int index = 0;
            DataRow[] source = null;
            eclaimID = new int[0];
            PaymentAmount = 0;
            List<long> ClaimIDs = new List<long>();
            if (ds.Tables.Count > 1)
            {
                source = (ReceiverID != null) ? ds.Tables[0].Select("SENDER_ID='" + ReceiverID + "'") : ds.Tables[0].Select();
                eclaimID = new int[source.Count<DataRow>()];
                if (eclaimID.Length > 0)
                {
                    source[0]["SENDER_ID"].ToString();
                    builder.Clear();
                    builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<Remittance.Advice xmlns:tns='http://www.eclaimlink.ae/DataDictionary/CommonTypes' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' xsi:noNamespaceSchemaLocation='https://www.eclaimlink.ae/DataDictionary/CommonTypes/RemittanceAdvice.xsd'>\n\t");
                    builder.Append("<Header>\n\t\t");
                    builder.Append("<SenderID>" + source[0]["RECEIVER_ID"].ToString() + "</SenderID>\n\t\t");
                    builder.Append("<ReceiverID>" + source[0]["SENDER_ID"].ToString() + "</ReceiverID>\n\t\t");
                    builder.Append("<TransactionDate>" + str3 + "</TransactionDate>\n\t\t");
                    builder.Append("<RecordCount>" + num + "</RecordCount>\n\t\t");
                    builder.Append("<DispositionFlag>" + str + "</DispositionFlag>\n\t");
                    builder.Append("</Header>\n");
                    foreach (DataRow row in source)
                    {
                        try
                        {
                            builder2.Clear();
                            DataRow[] rowArray2 = ds.Tables[1].Select("SYS_CLAIM_ID='" + row["SYS_CLAIM_ID"].ToString() + "'");
                            builder2.Append("\t<Claim>\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["CLAIM_ID"].ToString(), "ID") + "\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["ID_PAYER"].ToString(), "IDPayer") + "\n\t\t");
                            if (row["PROVIDER_ID"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row["PROVIDER_ID"].ToString(), "ProviderID") + "\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["PAYMENT_REFERENCE"].ToString(), "PaymentReference") + "\n");
                            builder2.Append("" + this.DBToXMLDate(row["Date_Settlement"], "DateSettlement") + "\n\t");
                            eclaimID[index] = Convert.ToInt32(row["SYS_CLAIM_ID"]);
                            if (row["ENCOUNTER_FACILITY_ID"].ToString() != string.Empty)
                            {
                                builder2.Append("<Encounter>\n\t\t");
                                builder2.Append("<FacilityID>" + row["ENCOUNTER_FACILITY_ID"].ToString() + "</FacilityID>\n\t");
                                builder2.Append("</Encounter>\n\t");
                            }
                            foreach (DataRow row2 in rowArray2)
                            {
                                builder2.Append("<Activity>\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_ID"].ToString(), "ID") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLDate(row2["ACTIVITY_START"].ToString(), "Start") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_TYPE"].ToString(), "Type") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_CODE"].ToString(), "Code") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["QUANTITY"].ToString(), "Quantity") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_NET"].ToString(), "Net") + "\n\t\t");
                                if (row2["ACTIVITY_LIST"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_LIST"].ToString(), "List") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["CLINICIAN"].ToString(), "Clinician") + "\n\t\t");
                                if (row2["PRIOR_AUTHORIZATION_ID"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["PRIOR_AUTHORIZATION_ID"].ToString(), "PriorAuthorizationID") + "\n\t\t");
                                if (row2["ACTIVITY_GROSS"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_GROSS"].ToString(), "Gross") + "\n\t\t");
                                builder2.Append("<PatientShare>0</PatientShare>\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_PAYMENT_AMOUNT"].ToString(), "PaymentAmount") + "\n\t\t");
                                if (row2["ACTIVITY_PAYMENT_AMOUNT"].ToString() != row2["ACTIVITY_NET"].ToString())
                                {
                                    if (row2["ACTIVITY_DENIAL_CODE"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_DENIAL_CODE"].ToString(), "DenialCode") + "\n");
                                }
                                builder2.Append("</Activity>\n\t");
                            }
                            builder2.Append("</Claim>\n");
                            num++;
                            builder.Append(builder2.ToString());
                            index++;
                        }
                        catch
                        {
                        }
                    }
                    builder.Replace("&", "&amp;");
                    builder.Replace("<RecordCount>0", "<RecordCount>" + num.ToString());
                    builder.Append("</Remittance.Advice>");
                }
            }
            if (num > 0)
            {
                int PaymentType = Convert.ToInt16(source[0]["PAYMENTTYPE"].ToString());
                file.SenderID = source[0]["SENDER_ID"].ToString();
                file.ReceiverID = source[0]["RECEIVER_ID"].ToString();
                file.TransactionDate = DateTime.Now;
                file.RecordCount = num;
                file.FileLocation = path;
                file.FileName = Path.GetFileName(path);
                file.TransactionError = "1";
                file.XmlData = builder.ToString();
                return file;
            }
            file.TransactionError = "0";
            return file;
        }

        public string[] UploadDHATransactionFile(EclaimFile eclaimFile)
        {
            string[] result = new string[3];
            int UploadResult;
            int transactionStatus, claimsStatus;
            string Filename = eclaimFile.FileName;
            string errorFileName = Path.GetFileNameWithoutExtension(eclaimFile.FileName) + "-error.txt";
            string errorPath = Path.GetDirectoryName(eclaimFile.FileLocation) + "\\" + errorFileName;
            string errorMsg = "";
            byte[] filecontent, errorReport;

            filecontent = Encoding.UTF8.GetBytes(eclaimFile.XmlData);

            UploadResult = DHPOSrv.UploadTransaction("ngiuae", "ngi2012", filecontent, Filename, out errorMsg, out errorReport);

            if (UploadResult == 0 || UploadResult == 1)
            {
                transactionStatus = 1;//upload success
                claimsStatus = 4;//resubmisson success
            }
            else
            {
                if (errorReport != null)
                    File.WriteAllBytes(errorPath, errorReport);
                transactionStatus = 2;// upload fiald
                claimsStatus = 5;//resubmisson fiald
            }
            result[0] = transactionStatus.ToString();
            result[1] = claimsStatus.ToString();
            result[2] = errorMsg;
            return result;
        }

        public void UploadHAADClaims()
        {
            int[] eclaimIDs;
            string[] UploadResult;
            double PaymentAmount;
            DataTable Headers = new DataTable();
            DataTable PaymentHeaders = new DataTable();
            List<string> FileLocation = new List<string>();
            DataView view;
            DataView paymentview;
            DAL Obj = new DAL();
            try
            {
                DataSet dsp = Obj.GetDataSet("Select Distinct Payment_Reference From EClaims Where Sys_Claim_Status = 3 And Sys_Claim_Source = 2");
                if (dsp.Tables.Count > 0)
                {
                    paymentview = new DataView(dsp.Tables[0]);
                    PaymentHeaders = paymentview.ToTable(true, "PAYMENT_REFERENCE");
                }
                foreach (DataRow paymentfile in PaymentHeaders.Rows)
                {
                    DataSet ds = Obj.GetClaims(paymentfile["PAYMENT_REFERENCE"].ToString(), 2);
                    if (ds.Tables.Count > 0)
                    {
                        view = new DataView(ds.Tables[0]);
                        Headers = view.ToTable(true, "SENDER_ID");
                    }

                    foreach (DataRow file in Headers.Rows)
                    {
                        EclaimFile efile = new EclaimFile();
                        efile = GenarateHAADXmlRemittanceFile(file["SENDER_ID"].ToString(), ds, out eclaimIDs, out PaymentAmount);
                        if (efile.TransactionError == "1")//1= File Generated Successfully
                        {
                            try
                            {
                                efile.FileLocation = WriteGeneratedXmlFile(efile.ReceiverID, efile.SenderID, efile.XmlData, efile.TransactionDate.ToString("yyyyMMdd"), 1);
                                efile.FileName = Path.GetFileName(efile.FileLocation);
                                efile.FileID = "1"; 
                                //log File To database 
                                Obj.WriteEclaimFilesLogToDB(efile, 2, 1, "", 2);

                                //Upload Transaction File, Log File to db and Relate between File and it's claims
                                UploadResult = UploadDHATransactionFile(efile);

                                if (Convert.ToInt64(UploadResult[0]) == 1 && Convert.ToInt64(UploadResult[1]) == 4) // Upload or Resubmission Success
                                {
                                    foreach (var ID in eclaimIDs)
                                    {
                                        Obj.ExecuteQuery("Update Eclaims Set Sys_Claim_Status = 4,RAUploadDate = SysDate Where Sys_Claim_ID = " + ID);
                                    }
                                }
                                else
                                {
                                    foreach (var ID in eclaimIDs)
                                    {
                                        Obj.ExecuteQuery("Update Eclaims Set Sys_Claim_Status = 5,RAUploadDate = Null Where Sys_Claim_ID = " + ID);
                                    }
                                }
                            }
                            catch
                            {
                            }
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public EclaimFile GenarateHAADXmlRemittanceFile(string ReceiverID, DataSet ds, out int[] eclaimID, out double PaymentAmount)
        {
            EclaimFile file = new EclaimFile();
            StringBuilder builder = new StringBuilder();
            StringBuilder builder2 = new StringBuilder();
            string path = "remittanceFile" + DateTime.Now.ToString("yyyyMMddHHmmssss") + ".xml";
            string str3 = DateTime.Now.ToString(ConfigurationSettings.AppSettings["UploadDateFormat"]);
            string str = ConfigurationSettings.AppSettings["DispositionFlag"].ToString();
            int num = 0;
            int index = 0;
            DataRow[] source = null;
            eclaimID = new int[0];
            PaymentAmount = 0;
            List<long> ClaimIDs = new List<long>();
            if (ds.Tables.Count > 1)
            {
                source = (ReceiverID != null) ? ds.Tables[0].Select("SENDER_ID='" + ReceiverID + "'") : ds.Tables[0].Select();
                eclaimID = new int[source.Count<DataRow>()];
                if (eclaimID.Length > 0)
                {
                    source[0]["SENDER_ID"].ToString();
                    builder.Clear();
                    builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\" ?>\n<Remittance.Advice xmlns:tns=\"http://www.haad.ae/DataDictionary/CommonTypes\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xsi:noNamespaceSchemaLocation=\"http://www.haad.ae/DataDictionary/CommonTypes/RemittanceAdvice.xsd\"> \n");
                    builder.Append("<Header>\n");
                    builder.Append("<SenderID>" + source[0]["RECEIVER_ID"].ToString() + "</SenderID>\n");
                    builder.Append("<ReceiverID>" + source[0]["SENDER_ID"].ToString() + "</ReceiverID>\n");
                    builder.Append("<TransactionDate>" + str3 + "</TransactionDate>\n");
                    builder.Append("<RecordCount>" + num + "</RecordCount>\n");
                    builder.Append("<DispositionFlag>" + str + "</DispositionFlag>\n");
                    builder.Append("</Header>\n");
                    foreach (DataRow row in source)
                    {
                        try
                        {
                            builder2.Clear();
                            DataRow[] rowArray2 = ds.Tables[1].Select("SYS_CLAIM_ID='" + row["SYS_CLAIM_ID"].ToString() + "'");
                            builder2.Append("\t<Claim>\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["CLAIM_ID"].ToString(), "ID") + "\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["ID_PAYER"].ToString(), "IDPayer") + "\n\t\t");
                            if (row["PROVIDER_ID"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row["PROVIDER_ID"].ToString(), "ProviderID") + "\n\t\t");
                            builder2.Append("" + this.DBToXMLString(row["PAYMENT_REFERENCE"].ToString(), "PaymentReference") + "\n");
                            builder2.Append("" + this.DBToXMLDate(row["Date_Settlement"], "DateSettlement") + "\n\t");
                            eclaimID[index] = Convert.ToInt32(row["SYS_CLAIM_ID"]);
                            if (row["ENCOUNTER_FACILITY_ID"].ToString() != string.Empty)
                            {
                                builder2.Append("<Encounter>\n\t\t");
                                builder2.Append("<FacilityID>" + row["ENCOUNTER_FACILITY_ID"].ToString() + "</FacilityID>\n\t");
                                builder2.Append("</Encounter>\n\t");
                            }
                            foreach (DataRow row2 in rowArray2)
                            {
                                builder2.Append("<Activity>\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_ID"].ToString(), "ID") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLDate(row2["ACTIVITY_START"].ToString(), "Start") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_TYPE"].ToString(), "Type") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_CODE"].ToString(), "Code") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["QUANTITY"].ToString(), "Quantity") + "\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_NET"].ToString(), "Net") + "\n\t\t");
                                if (row2["ACTIVITY_LIST"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_LIST"].ToString(), "List") + "\n\t\t");
                                if (row2["ORDERINGCLINICIAN"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ORDERINGCLINICIAN"].ToString(), "OrderingClinician") + "\n");
                                if (row2["CLINICIAN"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["CLINICIAN"].ToString(), "Clinician") + "\n");
                                if (row2["PRIOR_AUTHORIZATION_ID"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["PRIOR_AUTHORIZATION_ID"].ToString(), "PriorAuthorizationID") + "\n\t\t");
                                if (row2["ACTIVITY_NET"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_NET"].ToString(), "Gross") + "\n\t\t");
                                builder2.Append("<PatientShare>0</PatientShare>\n\t\t");
                                builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_PAYMENT_AMOUNT"].ToString(), "PaymentAmount") + "\n\t\t");
                                if (row2["ACTIVITY_PAYMENT_AMOUNT"].ToString() != row2["ACTIVITY_NET"].ToString())
                                {
                                    if (row2["ACTIVITY_DENIAL_CODE"].ToString() != string.Empty) builder2.Append("" + this.DBToXMLString(row2["ACTIVITY_DENIAL_CODE"].ToString(), "DenialCode") + "\n");
                                }
                                builder2.Append("</Activity>\n\t");
                            }
                            builder2.Append("</Claim>\n");
                            num++;
                            builder.Append(builder2.ToString());
                            index++;
                        }
                        catch
                        {
                        }
                    }
                    builder.Replace("&", "&amp;");
                    builder.Replace("<RecordCount>0", "<RecordCount>" + num.ToString());
                    builder.Append("</Remittance.Advice>");
                }
            }
            if (num > 0)
            {
                int PaymentType = Convert.ToInt16(source[0]["PAYMENTTYPE"].ToString());
                file.SenderID = source[0]["SENDER_ID"].ToString();
                file.ReceiverID = source[0]["RECEIVER_ID"].ToString();
                file.TransactionDate = DateTime.Now;
                file.RecordCount = num;
                file.FileLocation = path;
                file.FileName = Path.GetFileName(path);
                file.TransactionError = "1";
                file.XmlData = builder.ToString();
                return file;
            }
            file.TransactionError = "0";
            return file;
        }

        public string[] UploadHAADTransactionFile(EclaimFile eclaimFile)
        {
            string[] result = new string[3];
            int UploadResult;
            int transactionStatus, claimsStatus;
            string Filename = eclaimFile.FileName;
            string errorFileName = Path.GetFileNameWithoutExtension(eclaimFile.FileName) + "-error.txt";
            string errorPath = Path.GetDirectoryName(eclaimFile.FileLocation) + "\\" + errorFileName;
            string errorMsg = "";
            byte[] filecontent, errorReport;

            filecontent = Encoding.UTF8.GetBytes(eclaimFile.XmlData);

            UploadResult = HAADSrv.UploadTransaction("national general", "ngihnt", filecontent, Filename, out errorMsg, out errorReport);

            if (UploadResult == 0 || UploadResult == 1)
            {
                transactionStatus = 1;//upload success
                claimsStatus = 4;//resubmisson success
            }
            else
            {
                if (errorReport != null)
                    File.WriteAllBytes(errorPath, errorReport);
                transactionStatus = 2;// upload fiald
                claimsStatus = 5;//resubmisson fiald
            }
            result[0] = transactionStatus.ToString();
            result[1] = claimsStatus.ToString();
            result[2] = errorMsg;
            return result;
        }

        public string WriteGeneratedXmlFile(string SenderID, string ReceiverID, string Data, string TransactionDate, int Sys_Claim_Source)
        {

            int count = 1;
            bool Existed = false;
            string CurrentDateTime = DateTime.Now.ToString("yyyy/MM/dd");
            CurrentDateTime = CurrentDateTime.Replace("/", "");
            string uploadLocation = "";
            if (Sys_Claim_Source == 1)
            {
                uploadLocation = ConfigurationSettings.AppSettings["DHPORemittance"].ToString();
            }
            if (Sys_Claim_Source == 2)
            {
                uploadLocation = ConfigurationSettings.AppSettings["HAADRemittance"].ToString();
            }
            string path = uploadLocation + "\\" + TransactionDate + "\\" + SenderID;
            string FileName = "\\Remittance-" + ReceiverID + "_" + CurrentDateTime;
            string tmpFileName = path + FileName + ".xml";
            while (File.Exists(tmpFileName))
            {
                Existed = true;
                tmpFileName = path + FileName + "-" + count + ".xml";
                count++;
            }
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            if (!Existed)
                FileName = path + FileName + ".xml";
            else
                FileName = tmpFileName;
            //Wrile the file byte to file system
            File.WriteAllText(FileName, Data, Encoding.UTF8);
            return FileName;
        }

        private string DBToXMLString(object value, string Element)
        {
            if (value != DBNull.Value)
            {
                return ("<" + Element + ">" + value.ToString() + "</" + Element + ">");
            }
            return string.Empty;
        }

        private string DBToXMLDate(object value, string Element)
        {
            if (value != DBNull.Value)
            {
                return ("<" + Element + ">" + Convert.ToDateTime(value).ToString(ConfigurationSettings.AppSettings["UploadDateFormat"]) + "</" + Element + ">");
            }
            return string.Empty;
        }
    }
}
