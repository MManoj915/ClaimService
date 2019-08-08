using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ClaimService.DHPO;
using ClaimService.HAAD;

namespace ClaimService
{
    class DownloadClaims
    {
        ValidateTransactions DHPOSrv = new ValidateTransactions();
        Webservices HAADSrv = new Webservices();

        public void DownloadDHAClaims(string SDate, string EDate, int TransactionStatus)
        {
            DAL Obj = new DAL();
            string Files, ErrorMessage;
            int result = DHPOSrv.SearchTransactions("ngiuae", "ngi2012", 2, "", "", 2, TransactionStatus, "", SDate, EDate, -1, -1, out Files, out ErrorMessage);
            if (Files != "" && Files != null)
            {
                Files = Files.Replace("&", @"&amp;");

                // Read File Information
                DataSet ds = new DataSet();
                StringReader reader = new StringReader(Files.Replace("&", "&amp;"));
                ds.ReadXml(reader);


                int i = 0;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow file in ds.Tables[0].Rows)
                    {
                        try
                        {
                            EclaimFile eFile = new EclaimFile();
                            eFile.FileID = file["FileID"].ToString();
                            eFile.FileName = file["FileName"].ToString();
                            eFile.SenderID = file["SenderID"].ToString();
                            eFile.ReceiverID = file["ReceiverID"].ToString();
                            eFile.TransactionDate = ValueHandler.ToClaimDate(file["TransactionDate"]);
                            eFile.RecordCount = Convert.ToInt32(file["RecordCount"]);
                            eFile.IsDownloaded = Convert.ToBoolean(file["IsDownloaded"]);
                            if (!Obj.CheckIfFileExist(eFile.FileID, 1))
                            {
                                string FilePath = DownloadClaimSubmissionFile(eFile);
                                eFile.FileLocation = FilePath;
                                Obj.WriteEclaimFilesLogToDB(eFile, 1, 1, "", 1);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                        i++;
                    }
                }
            }
        }

        public string DownloadClaimSubmissionFile(EclaimFile eFile)
        {
            byte[] filedata;
            string filename, error;
            int result = DHPOSrv.DownloadTransactionFile("ngiuae", "ngi2012", eFile.FileID, out filename, out filedata, out error);
            string FilePath = ConfigurationSettings.AppSettings["DHPOClaims"].ToString() + "\\" + eFile.TransactionDate.ToString("yyyyMMdd") + "\\" + eFile.SenderID;

            if (!Directory.Exists(FilePath))
                Directory.CreateDirectory(FilePath);
            bool Existed = false;
            int count = 1;
            string FileName = FilePath + "\\" + Path.GetFileNameWithoutExtension(filename);
            string tmpFileName = FileName + ".xml";
            while (File.Exists(tmpFileName))
            {
                Existed = true;
                tmpFileName = FileName + "-" + count + ".xml";
                count++;
            }
            if (!Existed)
                FileName = FileName + ".xml";
            else
                FileName = tmpFileName;
            //Write the file byte to file system
            File.WriteAllBytes(FileName, filedata);
            return FileName;
        }

        public void DownloadDHAAuthorization(string SDate, string EDate, int TransactionStatus)
        {
            DAL Obj = new DAL();
            string Files, ErrorMessage;
            int result = DHPOSrv.SearchTransactions("ngiuae", "ngi2012", 1, "", "", 32, TransactionStatus, "", SDate, EDate, -1, -1, out Files, out ErrorMessage);
            if (Files != "" && Files != null)
            {
                Files = Files.Replace("&", @"&amp;");

                // Read File Information
                DataSet ds = new DataSet();
                StringReader reader = new StringReader(Files.Replace("&", "&amp;"));
                ds.ReadXml(reader);


                int i = 0;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow file in ds.Tables[0].Rows)
                    {
                        try
                        {
                            EclaimFile eFile = new EclaimFile();
                            eFile.FileID = file["FileID"].ToString();
                            eFile.FileName = file["FileName"].ToString();
                            eFile.SenderID = file["SenderID"].ToString();
                            eFile.ReceiverID = file["ReceiverID"].ToString();
                            eFile.TransactionDate = ValueHandler.ToClaimDate(file["TransactionDate"]);
                            eFile.RecordCount = Convert.ToInt32(file["RecordCount"]);
                            eFile.IsDownloaded = Convert.ToBoolean(file["IsDownloaded"]);

                            string FilePath = DownloadAuthSubmissionFile(eFile);
                            eFile.FileLocation = FilePath;
                            Obj.WritePriorAuthorizationRequestFilesLogToDB(eFile, 1, 1, "", 1);
                        }
                        catch
                        {
                            continue;
                        }
                        i++;
                    }
                }
            }
        }

        public string DownloadAuthSubmissionFile(EclaimFile eFile)
        {
            byte[] filedata;
            string filename, error;
            int result = DHPOSrv.DownloadTransactionFile("ngiuae", "ngi2012", eFile.FileID, out filename, out filedata, out error);
            string FilePath = ConfigurationSettings.AppSettings["DHPOAuth"].ToString() + "\\" + eFile.TransactionDate.ToString("yyyyMMdd") + "\\" + eFile.SenderID;

            if (!Directory.Exists(FilePath))
                Directory.CreateDirectory(FilePath);
            bool Existed = false;
            int count = 1;
            string FileName = FilePath + "\\" + Path.GetFileNameWithoutExtension(filename);
            string tmpFileName = FileName + ".xml";
            while (File.Exists(tmpFileName))
            {
                Existed = true;
                tmpFileName = FileName + "-" + count + ".xml";
                count++;
            }
            if (!Existed)
                FileName = FileName + ".xml";
            else
                FileName = tmpFileName;
            //Write the file byte to file system
            File.WriteAllBytes(FileName, filedata);
            return FileName;
        }

        public void DownloadHAADClaims(string SDate, string EDate, int TransactionStatus)
        {
            DAL Obj = new DAL();
            string Files, ErrorMessage;
            int result = HAADSrv.SearchTransactions("national general", "ngihnt", 2, "", "", 2, TransactionStatus, "", SDate, EDate, -1, -1, out Files, out ErrorMessage);
            if (Files != "" && Files != null)
            {
                Files = Files.Replace("&", @"&amp;");

                // Read File Information
                DataSet ds = new DataSet();
                StringReader reader = new StringReader(Files.Replace("&", "&amp;"));
                ds.ReadXml(reader);


                int i = 0;

                if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    foreach (DataRow file in ds.Tables[0].Rows)
                    {
                        try
                        {
                            EclaimFile eFile = new EclaimFile();
                            eFile.FileID = file["FileID"].ToString();
                            eFile.FileName = file["FileName"].ToString();
                            eFile.SenderID = file["SenderID"].ToString();
                            eFile.ReceiverID = file["ReceiverID"].ToString();
                            eFile.TransactionDate = ValueHandler.ToClaimDate(file["TransactionDate"]);
                            eFile.RecordCount = Convert.ToInt32(file["RecordCount"]);
                            eFile.IsDownloaded = Convert.ToBoolean(file["IsDownloaded"]);
                            if (!Obj.CheckIfFileExist(eFile.FileID, 2))
                            {
                                string FilePath = DownloadHAADClaimSubmissionFile(eFile);
                                eFile.FileLocation = FilePath;
                                Obj.WriteEclaimFilesLogToDB(eFile, 1, 1, "", 2);
                            }
                        }
                        catch
                        {
                            continue;
                        }
                        i++;
                    }
                }
            }
        }

        public string DownloadHAADClaimSubmissionFile(EclaimFile eFile)
        {
            byte[] filedata;
            string filename, error;
            int result = HAADSrv.DownloadTransactionFile("national general", "ngihnt", eFile.FileID, out filename, out filedata, out error);
            string FilePath = ConfigurationSettings.AppSettings["HAADClaims"].ToString() + "\\" + eFile.TransactionDate.ToString("yyyyMMdd") + "\\" + eFile.SenderID;

            if (!Directory.Exists(FilePath))
                Directory.CreateDirectory(FilePath);
            bool Existed = false;
            int count = 1;
            string FileName = FilePath + "\\" + Path.GetFileNameWithoutExtension(filename);
            string tmpFileName = FileName + ".xml";
            while (File.Exists(tmpFileName))
            {
                Existed = true;
                tmpFileName = FileName + "-" + count + ".xml";
                count++;
            }
            if (!Existed)
                FileName = FileName + ".xml";
            else
                FileName = tmpFileName;
            //Write the file byte to file system
            File.WriteAllBytes(FileName, filedata);
            return FileName;
        }

        public void InsertClaims(int Sys_File_Source)
        {
            DAL Obj = new DAL();
            List<EclaimBatch> BatchFile = Obj.GetEclaimBatchFiles(Sys_File_Source);
            foreach (EclaimBatch Det in BatchFile)
            {
                try
                {
                    string ClaimID, ClaimIDPayer, ClaimMemberID, ClaimPayerID, ClaimProviderID, ClaimEmiratesIDNumber;
                    double ClaimGross, ClaimPatientShare, ClaimNet;
                    string EncounterFacilityID = "", EncounterType = "", EncounterPatientID = "", EncounterStartType = "", EncounterEndType = "", EncounterTransferSource = "", EncounterTransferDestination = "";
                    DateTime EncounterStart = DateTime.Now, EncounterEnd = DateTime.Now;
                    string Message = "";
                    long Errorcount = 0;

                    DataSet ds = new DataSet();
                    DAL _da = new DAL();
                    string xmlString = System.IO.File.ReadAllText(Det.File_Location + "\\" + Det.File_Name);
                    xmlString = EscapeXMLValue(xmlString);
                    TextReader txtReader = new StringReader(xmlString);
                    XmlReader reader = new XmlTextReader(txtReader);
                    ds.ReadXml(reader);

                    DataTable header = ds.Tables["Header"];

                    #region Header              
                    string SenderID = header.Rows[0]["SenderID"].ToString();
                    string ReceiverID = header.Rows[0]["ReceiverID"].ToString();
                    DateTime TransactionDate = ValueHandler.ToClaimDate(header.Rows[0]["TransactionDate"]);
                    long RecordCount = Convert.ToInt32(header.Rows[0]["RecordCount"]);
                    string DispositionFlag = header.Rows[0]["DispositionFlag"].ToString();
                    #endregion

                    long Sys_Claim_ID = _da.ExecuteScalar("Select nvl(max(SYS_CLAIM_ID),0)+1 from ECLAIMS");

                    #region Claims              
                    foreach (DataRow claim in ds.Tables["Claim"].Rows)
                    {
                        try
                        {
                            ClaimID = claim["ID"].ToString();
                            try
                            {
                                ClaimIDPayer = claim["IDPayer"].ToString();
                            }
                            catch
                            {
                                ClaimIDPayer = claim["PayerID"].ToString();
                            }
                            ClaimMemberID = claim["MemberID"].ToString();
                            ClaimPayerID = claim["PayerID"].ToString();
                            ClaimProviderID = claim["PayerID"].ToString();
                            ClaimEmiratesIDNumber = claim["EmiratesIDNumber"].ToString();
                            ClaimGross = Convert.ToDouble(claim["Gross"].ToString());
                            ClaimPatientShare = Convert.ToDouble(claim["PatientShare"].ToString());
                            ClaimNet = Convert.ToDouble(claim["Net"].ToString());

                            #region Encounter
                            foreach (DataRow Encounter in claim.GetChildRows("Claim_Encounter"))
                            {
                                EncounterFacilityID = Encounter["FacilityID"].ToString();
                                EncounterType = Encounter["Type"].ToString();
                                EncounterPatientID = Encounter["PatientID"].ToString();
                                EncounterStart = ValueHandler.ToClaimDate(Encounter["Start"].ToString());
                                try
                                {
                                    EncounterEnd = ValueHandler.ToClaimDate(Encounter["End"].ToString());
                                }
                                catch
                                {
                                }
                                try
                                {
                                    EncounterStartType = Encounter["StartType"].ToString();
                                }
                                catch
                                {
                                }
                                try
                                {
                                    EncounterEndType = Encounter["EndType"].ToString();
                                }
                                catch
                                {
                                }
                                try
                                {
                                    EncounterTransferSource = Encounter["TransferSource"].ToString();
                                }
                                catch
                                {
                                }
                                try
                                {
                                    EncounterTransferDestination = Encounter["TransferDestination"].ToString();
                                }
                                catch
                                {
                                }
                            }
                            #endregion

                            string ProviderTypeFK = @"SELECT NVL(PROVIDERTYPE,2)  FROM  TABLE(SF_GETCLAIMPROVIDER_FNC('" + SenderID + "'))";
                            int ClaimProviderType = Convert.ToInt32(_da.ExecuteScalar(ProviderTypeFK));

                            string cmd1 = " INSERT INTO ECLAIMS (SYS_CLAIM_ID,SENDER_ID, RECEIVER_ID, TRANSACTION_DATE, DISPOSITION_FLAG, CLAIM_ID, ID_PAYER, MEMBER_ID, " +
                            " PAYER_ID, PROVIDER_ID, EMIRATES_ID_NUMBER,CLAIM_GROSS, CLAIM_PATIENT_SHARE, CLAIM_NET, ENCOUNTER_FACILITY_ID, ENCOUNTER_FACILITY_TYPE, " +
                            " ENCOUNTER_PATIENT_ID, ENCOUNTER_START, ENCOUNTER_END, ENCOUNTER_START_TYPE, ENCOUNTER_END_TYPE, ENCOUNTER_TRANSFER_SOURCE, " +
                            " ENCOUNTER_TRANSFER_DESTINATION, SYS_CLAIM_STATUS, SYS_CLAIM_SOURCE, PROVIDERTYPE, CEEDSTATUS,Sys_file_ID) " +
                            " VALUES (" + Sys_Claim_ID + ",'" + SenderID + "','" + ReceiverID + "',to_date('" + TransactionDate.ToShortDateString() + "','mm-dd-yyyy'),'" + DispositionFlag + "','" + ClaimID + "','" + ClaimIDPayer + "','" + ClaimMemberID + "', " +
                            " '" + ClaimPayerID + "','" + ClaimProviderID + "','" + ClaimEmiratesIDNumber + "'," + ClaimGross + "," + ClaimPatientShare + "," + ClaimNet + ",'" + EncounterFacilityID + "', '" + EncounterType + "', " +
                            " '" + EncounterPatientID + "',to_date('" + EncounterStart.ToShortDateString() + " " + EncounterStart.ToLongTimeString() + "','mm-dd-yyyy HH24:MI:SS'),to_date('" + EncounterEnd.ToShortDateString() + " " + EncounterEnd.ToLongTimeString() + "','mm-dd-yyyy HH24:MI:SS'),'" + EncounterStartType + "','" + EncounterEndType + "','" + EncounterTransferSource + "', " +
                            " '" + EncounterTransferDestination + "',277," + Sys_File_Source + "," + ClaimProviderType + ",0," + Det.Sys_File_ID + ")";
                            string Result = _da.ExecuteQuery(cmd1);

                            if (Result != "S")
                            {
                                Errorcount++; 
                            }
                                


                            #region Diagnosis 
                            foreach (DataRow diag in claim.GetChildRows("Claim_Diagnosis"))
                            {
                                long Sys_Diag_ID = _da.ExecuteScalar("Select nvl(max(SYS_Diagnosis_ID),0)+1 from Eclaim_Diagnosis");
                                string DiagnosisType, DiagnosisCode, DxInfoType = "", DxInfoCode = "";
                                DiagnosisType = diag["Type"].ToString();
                                DiagnosisCode = diag["Code"].ToString();
                                string cmd2 = "Insert Into Eclaim_Diagnosis (SYS_Diagnosis_ID,Sys_Claim_ID,Diagnosis_Type,Diagnosis_Code) Values " +
                                    "(" + Sys_Diag_ID + "," + Sys_Claim_ID + ",'" + DiagnosisType + "','" + DiagnosisCode + "')";
                                Result = _da.ExecuteQuery(cmd2);
                                if (Result != "S")
                                {
                                    Errorcount++;
                                }
                            }
                            #endregion

                            #region Activity 
                            foreach (DataRow act in claim.GetChildRows("Claim_Activity"))
                            {
                                long Sys_Act_ID = _da.ExecuteScalar("Select nvl(max(SYS_Activity_ID),0)+1 from Eclaim_Activities");
                                string ActivityID, ActivityType, ActivityCode, ActivityClinician = "", ActivityPriorAuthorizationID = "", ActivityOrderingClinician = "";
                                double ActivityQty, ActivityNet;
                                DateTime ActivityStart;
                                ActivityID = act["ID"].ToString();
                                ActivityStart = ValueHandler.ToClaimDate(act["Start"].ToString());
                                ActivityType = act["Type"].ToString();
                                ActivityCode = act["Code"].ToString();
                                ActivityQty = (Convert.ToDouble(act["Quantity"].ToString()));
                                ActivityNet = Convert.ToDouble(act["Net"].ToString());
                                try
                                {
                                    ActivityClinician = act["Clinician"].ToString();
                                }
                                catch
                                {
                                }
                                try
                                {
                                    ActivityPriorAuthorizationID = act["PriorAuthorizationID"].ToString();
                                }
                                catch
                                {
                                }
                                try
                                {
                                    ActivityOrderingClinician = (act["OrderingClinician"].ToString());
                                }
                                catch
                                {
                                }
                                string cmd3 = "Insert Into Eclaim_Activities (Sys_Activity_Id,Sys_Claim_ID,Activity_ID,Activity_Start,Activity_Type,Activity_Code, " +
                                    " Quantity,Activity_Net,Clinician,Prior_Authorization_Id,OrderingClinician) Values " +
                                    "(" + Sys_Act_ID + "," + Sys_Claim_ID + ",'" + ActivityID + "',to_date('" + ActivityStart.ToShortDateString() + " " + ActivityStart.ToLongTimeString() + "','mm-dd-yyyy HH24:MI:SS') , " +
                                    " '" + ActivityType + "','" + ActivityCode + "'," + ActivityQty + "," + ActivityNet + ",'" + ActivityClinician + "','" + ActivityPriorAuthorizationID + "','" + ActivityOrderingClinician + "')";
                                Result = _da.ExecuteQuery(cmd3);
                                if (Result != "S")
                                {
                                    Errorcount++;
                                }

                                #region Activity Observation 
                                foreach (DataRow observ in act.GetChildRows("Activity_Observation"))
                                {
                                    long Sys_Obs_ID = _da.ExecuteScalar("select NVL(Max(Sys_Observation_ID),0)+1 from ACTIVITY_OBSERVATIONS");
                                    string ObsType, ObsCode = "", ObsValue = "", ObsValueType;
                                    ObsType = observ["Type"].ToString();
                                    ObsValueType = observ["ValueType"].ToString();
                                    if (string.IsNullOrEmpty(ObsType))
                                    {
                                        ObsType = ObsType.Replace("'", "");
                                    }
                                    if (string.IsNullOrEmpty(observ["Code"].ToString()))
                                    {
                                        ObsCode = observ["Code"].ToString().Replace("'", "");
                                    }
                                    if (string.IsNullOrEmpty(ObsValueType))
                                    {
                                        ObsValueType = ObsValueType.Replace("'", "");
                                    }
                                    if (ObsType == "File")
                                    {
                                        string filename = ConfigurationSettings.AppSettings["ClaimObservation"].ToString() + "\\" + Guid.NewGuid().ToString() + "." + ObsValueType;
                                        WriteObservationAttachment(filename, observ["Value"].ToString());
                                        ObsValue = filename;
                                    }
                                    string cmd4 = "Insert Into ACTIVITY_OBSERVATIONS (Sys_Observation_ID,Sys_Activity_ID,Observation_Type,Observation_Code, " +
                                        " Value_Type,Value) Values " +
                                        " (" + Sys_Obs_ID + "," + Sys_Act_ID + ",'" + ObsType + "','" + ObsCode + "','" + ObsValueType + "','" + ObsValue + "')";
                                    Result = _da.ExecuteQuery(cmd4);
                                    if (Result != "S")
                                    {
                                        Errorcount++;
                                    }
                                }
                                #endregion

                            }
                            #endregion

                            #region Resubmission 
                            foreach (DataRow resub in claim.GetChildRows("Claim_Resubmission"))
                            {
                                string ResubType, ResubComment;
                                ResubType = resub["Type"].ToString();
                                ResubComment = resub["Comment"].ToString();
                                Result = _da.ExecuteQuery("Update Eclaims Set Resubmission_type = '" + ResubType + "',Resubmission_Comment = '" + ResubComment + "',ISResubmission = 1 Where Sys_Claim_ID = " + Sys_Claim_ID);
                                try
                                {
                                    if (resub["Attachment"].ToString() != string.Empty)
                                    {
                                        string PDFFilePath = ConfigurationSettings.AppSettings["ResubmissionClaims"].ToString();
                                        if (!Directory.Exists(PDFFilePath))
                                        {
                                            Directory.CreateDirectory(PDFFilePath);
                                        }
                                        try
                                        {
                                            string AttachmentPath = PDFFilePath + "\\" + claim["ID"].ToString() + ".pdf";
                                            byte[] toEncodeAsBytes = System.Convert.FromBase64String(resub["Attachment"].ToString());
                                            File.WriteAllBytes(AttachmentPath, toEncodeAsBytes);
                                        }

                                        catch
                                        {
                                        }

                                    }
                                }
                                catch
                                {
                                }
                            }
                            #endregion 
                        }
                        catch
                        {
                            Errorcount++; 
                        }
                    }
                    #endregion

                    if(Errorcount == 0)
                        Message = Obj.ExecuteQuery("Update Eclaim_Batch_Files Set STATUS = 31 Where Sys_ID = " + Det.Sys_File_ID);
                    else
                        Message = Obj.ExecuteQuery("Update Eclaim_Batch_Files Set STATUS = -1 Where Sys_ID = " + Det.Sys_File_ID);

                }
                catch
                {
                    string Message = Obj.ExecuteQuery("Update Eclaim_Batch_Files Set STATUS = -1 Where Sys_ID = " + Det.Sys_File_ID);
                }

            }
        }

        public void InsertDHACopyClaims(string FilePath, long Sys_File_Source)
        {
            string ClaimID, ClaimIDPayer, ClaimMemberID, ClaimPayerID, ClaimProviderID, ClaimEmiratesIDNumber;
            double ClaimGross, ClaimPatientShare, ClaimNet;
            string EncounterFacilityID = "", EncounterType = "", EncounterPatientID = "", EncounterStartType = "", EncounterEndType = "", EncounterTransferSource = "", EncounterTransferDestination = "";
            DateTime EncounterStart = DateTime.Now, EncounterEnd = DateTime.Now;

            DataSet ds = new DataSet();
            DAL _da = new DAL();
            string xmlString = System.IO.File.ReadAllText(FilePath);
            xmlString = EscapeXMLValue(xmlString);
            TextReader txtReader = new StringReader(xmlString);
            XmlReader reader = new XmlTextReader(txtReader);
            ds.ReadXml(reader);

            DataTable header = ds.Tables["Header"];

            #region Header              
            string SenderID = header.Rows[0]["SenderID"].ToString();
            string ReceiverID = header.Rows[0]["ReceiverID"].ToString();
            DateTime TransactionDate = ValueHandler.ToClaimDate(header.Rows[0]["TransactionDate"]);
            long RecordCount = Convert.ToInt32(header.Rows[0]["RecordCount"]);
            string DispositionFlag = header.Rows[0]["DispositionFlag"].ToString();
            #endregion

            long Sys_Claim_ID = _da.ExecuteScalar("Select nvl(max(SYS_CLAIM_ID),0)+1 from ECLAIMS");

            #region Claims              
            foreach (DataRow claim in ds.Tables["Claim"].Rows)
            {
                try
                {
                    ClaimID = claim["ID"].ToString();
                    try
                    {
                        ClaimIDPayer = claim["IDPayer"].ToString();
                    }
                    catch
                    {
                        ClaimIDPayer = claim["PayerID"].ToString();
                    }
                    ClaimMemberID = claim["MemberID"].ToString();
                    ClaimPayerID = claim["PayerID"].ToString();
                    ClaimProviderID = claim["PayerID"].ToString();
                    ClaimEmiratesIDNumber = claim["EmiratesIDNumber"].ToString();
                    ClaimGross = Convert.ToDouble(claim["Gross"].ToString());
                    ClaimPatientShare = Convert.ToDouble(claim["PatientShare"].ToString());
                    ClaimNet = Convert.ToDouble(claim["Net"].ToString());

                    #region Encounter
                    foreach (DataRow Encounter in claim.GetChildRows("Claim_Encounter"))
                    {
                        EncounterFacilityID = Encounter["FacilityID"].ToString();
                        EncounterType = Encounter["Type"].ToString();
                        EncounterPatientID = Encounter["PatientID"].ToString();
                        EncounterStart = ValueHandler.ToClaimDate(claim["Start"].ToString());
                        try
                        {
                            EncounterEnd = ValueHandler.ToClaimDate(claim["End"].ToString());
                        }
                        catch
                        {
                        }
                        try
                        {
                            EncounterStartType = Encounter["StartType"].ToString();
                        }
                        catch
                        {
                        }
                        try
                        {
                            EncounterEndType = Encounter["EndType"].ToString();
                        }
                        catch
                        {
                        }
                        try
                        {
                            EncounterTransferSource = Encounter["TransferSource"].ToString();
                        }
                        catch
                        {
                        }
                        try
                        {
                            EncounterTransferDestination = Encounter["TransferDestination"].ToString();
                        }
                        catch
                        {
                        }
                    }
                    #endregion

                    string ProviderTypeFK = @"SELECT NVL(PROVIDERTYPE,2)  FROM  TABLE(SF_GETCLAIMPROVIDER_FNC('" + SenderID + "'))";
                    int ClaimProviderType = Convert.ToInt32(_da.ExecuteScalar(ProviderTypeFK));

                    string cmd1 = " INSERT INTO ECLAIMS (SYS_CLAIM_ID,SENDER_ID, RECEIVER_ID, TRANSACTION_DATE, DISPOSITION_FLAG, CLAIM_ID, ID_PAYER, MEMBER_ID, " +
                    " PAYER_ID, PROVIDER_ID, EMIRATES_ID_NUMBER,CLAIM_GROSS, CLAIM_PATIENT_SHARE, CLAIM_NET, ENCOUNTER_FACILITY_ID, ENCOUNTER_FACILITY_TYPE, " +
                    " ENCOUNTER_PATIENT_ID, ENCOUNTER_START, ENCOUNTER_END, ENCOUNTER_START_TYPE, ENCOUNTER_END_TYPE, ENCOUNTER_TRANSFER_SOURCE, " +
                    " ENCOUNTER_TRANSFER_DESTINATION, SYS_CLAIM_STATUS, SYS_CLAIM_SOURCE, PROVIDERTYPE, CEEDSTATUS) " +
                    " VALUES (" + Sys_Claim_ID + ",'" + SenderID + "','" + ReceiverID + "',to_date('" + TransactionDate.ToShortDateString() + "','mm-dd-yyyy'),'" + DispositionFlag + "','" + ClaimID + "','" + ClaimIDPayer + "','" + ClaimMemberID + "', " +
                    " '" + ClaimPayerID + "','" + ClaimProviderID + "','" + ClaimEmiratesIDNumber + "'," + ClaimGross + "," + ClaimPatientShare + "," + ClaimNet + ",'" + EncounterFacilityID + "', '" + EncounterType + "' " +
                    " '" + EncounterPatientID + "',to_date('" + EncounterStart.ToShortDateString() + " " + EncounterStart.ToLongTimeString() + "','mm-dd-yyyy HH:MI:SS AM'),to_date('" + EncounterEnd.ToShortDateString() + " " + EncounterEnd.ToLongTimeString() + "','mm-dd-yyyy HH:MI:SS AM'),'" + EncounterStartType + "','" + EncounterEndType + "','" + EncounterTransferSource + "', " +
                    " '" + EncounterTransferDestination + "',277," + Sys_File_Source + "," + ClaimProviderType + ",0)";
                    string Result = _da.ExecuteQuery(cmd1);

                    #region Diagnosis 
                    foreach (DataRow diag in claim.GetChildRows("Claim_Diagnosis"))
                    {
                        long Sys_Diag_ID = _da.ExecuteScalar("Select nvl(max(SYS_Diagnosis_ID),0)+1 from Eclaim_Diagnosis");
                        string DiagnosisType, DiagnosisCode, DxInfoType = "", DxInfoCode = "";
                        DiagnosisType = diag["Type"].ToString();
                        DiagnosisCode = diag["Code"].ToString();
                        string cmd2 = "Insert Into Eclaim_Diagnosis (SYS_Diagnosis_ID,Sys_Claim_ID,Diagnosis_Type,Diagnosis_Code) Values " +
                            "(" + Sys_Diag_ID + "," + Sys_Claim_ID + ",'" + DiagnosisType + "','" + DiagnosisCode + "')";
                        Result = _da.ExecuteQuery(cmd2);
                    }
                    #endregion

                    #region Activity 
                    foreach (DataRow act in claim.GetChildRows("Claim_Activity"))
                    {
                        long Sys_Act_ID = _da.ExecuteScalar("Select nvl(max(SYS_Activity_ID),0)+1 from Eclaim_Activities");
                        string ActivityID, ActivityType, ActivityCode, ActivityClinician = "", ActivityPriorAuthorizationID = "", ActivityOrderingClinician = "";
                        double ActivityQty, ActivityNet;
                        DateTime ActivityStart;
                        ActivityID = act["ID"].ToString();
                        ActivityStart = ValueHandler.ToClaimDate(claim["Start"].ToString());
                        ActivityType = act["Type"].ToString();
                        ActivityCode = act["Code"].ToString();
                        ActivityQty = (Convert.ToDouble(act["Quantity"].ToString()));
                        ActivityNet = Convert.ToDouble(act["Net"].ToString());
                        try
                        {
                            ActivityClinician = act["Clinician"].ToString();
                        }
                        catch
                        {
                        }
                        try
                        {
                            ActivityPriorAuthorizationID = act["PriorAuthorizationID"].ToString();
                        }
                        catch
                        {
                        }
                        try
                        {
                            ActivityOrderingClinician = (act["OrderingClinician"].ToString());
                        }
                        catch
                        {
                        }
                        string cmd3 = "Insert Into Eclaim_Activities (Sys_Activity_Id,Sys_Claim_ID,Activity_ID,Activity_Start,Activity_Type,Activity_Code, " +
                            " Quantity,Activity_Net,Clinician,Prior_Authorization_Id,OrderingClinician) Values " +
                            "(" + Sys_Act_ID + "," + Sys_Claim_ID + ",'" + ActivityID + "',to_date('" + ActivityStart.ToShortDateString() + " " + ActivityStart.ToLongTimeString() + "','mm-dd-yyyy HH:MI:SS AM') , " +
                            " '" + ActivityType + "','" + ActivityCode + "'," + ActivityQty + "," + ActivityNet + ",'" + ActivityClinician + "','" + ActivityPriorAuthorizationID + "','" + ActivityOrderingClinician + "')";
                        Result = _da.ExecuteQuery(cmd3);

                        #region Activity Observation 
                        foreach (DataRow observ in act.GetChildRows("Activity_Observation"))
                        {
                            long Sys_Obs_ID = _da.ExecuteScalar("select NVL(Max(Sys_Observation_ID),0) from ACTIVITY_OBSERVATIONS");
                            string ObsType, ObsCode = "", ObsValue = "", ObsValueType;
                            ObsType = observ["Type"].ToString();
                            ObsValueType = observ["ValueType"].ToString();
                            if (string.IsNullOrEmpty(ObsType))
                            {
                                ObsType = ObsType.Replace("'", "");
                            }
                            if (string.IsNullOrEmpty(observ["Code"].ToString()))
                            {
                                ObsCode = observ["Code"].ToString().Replace("'", "");
                            }
                            if (string.IsNullOrEmpty(ObsValueType))
                            {
                                ObsValueType = ObsValueType.Replace("'", "");
                            }
                            if (ObsType == "File")
                            {
                                string filename = ConfigurationSettings.AppSettings["ClaimObservation"].ToString() + "\\" + Guid.NewGuid().ToString() + "." + ObsValueType;
                                WriteObservationAttachment(filename, observ["Value"].ToString());
                                ObsValue = filename;
                            }
                            string cmd4 = "Insert Into ACTIVITY_OBSERVATIONS (Sys_Observation_ID,Sys_Activity_ID,Observation_Type,Observation_Code, " +
                                " ValueType,Value) Values " +
                                " (" + Sys_Obs_ID + "," + Sys_Act_ID + ",'" + ObsType + "','" + ObsCode + "','" + ObsValueType + "','" + ObsValue + "')";
                            Result = _da.ExecuteQuery(cmd4);
                        }
                        #endregion

                    }
                    #endregion

                    #region Resubmission 
                    foreach (DataRow resub in claim.GetChildRows("Claim_Resubmission"))
                    {
                        string ResubType, ResubComment;
                        ResubType = resub["Type"].ToString();
                        ResubComment = resub["Comment"].ToString();
                        Result = _da.ExecuteQuery("Update Eclaims Set Resubmission_type = '" + ResubType + "',Resubmission_Comment = '" + ResubComment + "',ISResubmission = 1 Where Sys_Claim_ID = " + Sys_Claim_ID);
                        try
                        {
                            if (resub["Attachment"].ToString() != string.Empty)
                            {
                                string PDFFilePath = ConfigurationSettings.AppSettings["ResubmissionClaims"].ToString();
                                if (!Directory.Exists(PDFFilePath))
                                {
                                    Directory.CreateDirectory(PDFFilePath);
                                }
                                try
                                {
                                    string AttachmentPath = PDFFilePath + "\\" + claim["ID"].ToString() + ".pdf";
                                    byte[] toEncodeAsBytes = System.Convert.FromBase64String(resub["Attachment"].ToString());
                                    File.WriteAllBytes(AttachmentPath, toEncodeAsBytes);
                                }

                                catch
                                {
                                }

                            }
                        }
                        catch
                        {
                        }
                    }
                    #endregion
                }
                catch
                {
                }
            }
            #endregion
        }

        public static string EscapeXMLValue(string xmlString)
        {
            if (xmlString == null)
                throw new ArgumentNullException("xmlString");
            xmlString = xmlString.Replace("'", "");
            xmlString = xmlString.Replace(",", "");
            return xmlString.Replace("&", "&amp;");
        }

        private void WriteObservationAttachment(string filename, string fileData)
        {
            try
            {
                byte[] bytes = Convert.FromBase64String(fileData);
                File.WriteAllBytes(filename, bytes);
            }
            catch (Exception)
            {
            }
        }
    }
}
