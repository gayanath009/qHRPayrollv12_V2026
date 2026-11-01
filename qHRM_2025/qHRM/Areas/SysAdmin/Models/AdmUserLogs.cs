using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public class AdmUserLogs
    {
     
        [Key]
        public long LogId { get; set; }
        public int UsrId { get; set; }
        public string SessionId { get; set; }
        public DateTime? InTime { get; set; }
        public DateTime? OutTime { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [JsonIgnore]
        public virtual AdmUser Usr { get; set; }
    }

    public partial class AdmUserLogsView
    {
        public string EmpName { get; set; }
        public string InTime { get; set; }
        public string OutTime { get; set; }
        public string LogDate { get; set; }
    }
}
