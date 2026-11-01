using qHRM.Areas.Entity.Models;
using qHRM.Areas.General.Models;
using System;
using System.Collections.Generic;

namespace qHRM.Areas.SystemLog.Models
{
    public partial class SysTableInfo
    {
        public SysTableInfo()
        {
            LogAtndc = new HashSet<LogAtndc>();
            LogHremp = new HashSet<LogHremp>();
            LogHrlv = new HashSet<LogHrlv>();
            LogHrpr = new HashSet<LogHrpr>();
            LogHrwsf = new HashSet<LogHrwsf>();
            LogOrg = new HashSet<LogOrg>();
        }

        public int TblId { get; set; }
        public string Section { get; set; }
        public string TblName { get; set; }
        public string MdlName { get; set; }
        public bool LogInsert { get; set; }
        public bool LogUpdate { get; set; }
        public bool LogDelete { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<LogAtndc> LogAtndc { get; set; }
        public virtual ICollection<LogHremp> LogHremp { get; set; }
        public virtual ICollection<LogHrlv> LogHrlv { get; set; }
        public virtual ICollection<LogHrpr> LogHrpr { get; set; }
        public virtual ICollection<LogHrwsf> LogHrwsf { get; set; }
        public virtual ICollection<LogOrg> LogOrg { get; set; }
    }
}
