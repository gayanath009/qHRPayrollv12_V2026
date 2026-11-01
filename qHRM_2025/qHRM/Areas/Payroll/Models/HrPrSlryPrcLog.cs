using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrSlryPrcLog
    {
        public HrPrSlryPrcLog()
        {
            HrPrAdHocTrns = new HashSet<HrPrAdHocTrns>();
            HrPrSlryPrcLogDtl = new HashSet<HrPrSlryPrcLogDtl>();
        }

        public int PrcLogId { get; set; }
        public int PrcId { get; set; }
        public int PrcRvsn { get; set; }
        public string PrcTyp { get; set; }
        public string Note { get; set; }
        public short RevStat { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrPrSlryPrcHdr Prc { get; set; }
        public virtual ICollection<HrPrAdHocTrns> HrPrAdHocTrns { get; set; }
        public virtual ICollection<HrPrSlryPrcLogDtl> HrPrSlryPrcLogDtl { get; set; }
    }
}
