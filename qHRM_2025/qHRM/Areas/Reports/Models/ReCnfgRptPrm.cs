using System.Collections.Generic;
using System;

namespace qHRM.Areas.Reports.Models
{
    public partial class ReCnfgRptPrm
    {
        public ReCnfgRptPrm()
        {
            ReStgngRptPrms = new HashSet<ReStgngRptPrm>();
        }

        public int PrmId { get; set; }
        public int RptId { get; set; }
        public string PrmNm { get; set; }
        public string PrmTyp { get; set; }
        public bool? SpPrm { get; set; }
        public bool? Rqrd { get; set; }
        public short RcSts { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDt { get; set; }

        public virtual ReCnfgRpt Rpt { get; set; }
        public virtual ICollection<ReStgngRptPrm> ReStgngRptPrms { get; set; }
    }
}
