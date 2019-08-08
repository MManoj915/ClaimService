using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClaimService
{
    class EclaimFile
    {
        public string FileID { get; set; }
        public string FileName { get; set; }
        public string SenderID { get; set; }
        public string ReceiverID { get; set; }
        public DateTime TransactionDate { get; set; }
        public long RecordCount { get; set; }
        public Boolean IsDownloaded { get; set; }
        public string FileLocation { get; set; }
        public string XmlData { get; set; }
        public string TransactionError { get; set; }
    }

    class EclaimBatch
    {
        public long Sys_File_ID { get; set; }
        public string File_Name { get; set; }
        public string File_Location { get; set; }
    }
}
