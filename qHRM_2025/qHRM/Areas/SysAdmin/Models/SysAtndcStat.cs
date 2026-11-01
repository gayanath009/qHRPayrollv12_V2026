using qHRM.Areas.Attendance.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.SysAdmin.Models
{
    public partial class SysAtndcStat
    {
        public SysAtndcStat()
        {
            HrEmpAtndcSft = new HashSet<HrEmpAtndcSft>();
        }

        public int AtndcStatId { get; set; }
        public int? PrntId { get; set; }
        public string AtndcStatTxt { get; set; }
        public string Desc { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual ICollection<HrEmpAtndcSft> HrEmpAtndcSft { get; set; }
    }
}
