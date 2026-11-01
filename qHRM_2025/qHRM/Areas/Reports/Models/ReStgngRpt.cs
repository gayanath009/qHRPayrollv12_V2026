using System.Collections.Generic;

namespace qHRM.Areas.Reports.Models
{
    public partial class ReStgngRpt
    {
        public ReStgngRpt()
        {
            ReStgngRptPrms = new HashSet<ReStgngRptPrm>();
        }

        public long SsnRptId { get; set; }
        public long SsnId { get; set; }
        public int RptId { get; set; }

        public virtual ReCnfgRpt Rpt { get; set; }
        public virtual ReStgngSsn Ssn { get; set; }
        public virtual ICollection<ReStgngRptPrm> ReStgngRptPrms { get; set; }
    }
}
