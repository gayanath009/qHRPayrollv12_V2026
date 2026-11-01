using System.Collections.Generic;
using System;

namespace qHRM.Areas.Reports.Models
{
    public partial class ReCnfgRpt
    {
        public ReCnfgRpt()
        {
            InversePrnt = new HashSet<ReCnfgRpt>();
            ReCnfgRptPrms = new HashSet<ReCnfgRptPrm>();
            ReRptLogs = new HashSet<ReRptLog>();
            ReStgngRpts = new HashSet<ReStgngRpt>();
        }

        public int RptId { get; set; }
        public string RptKy { get; set; }
        public string RptFlNm { get; set; }
        public string RptDsplyNm { get; set; }
        public string SpNm { get; set; }
        public int RptGrpId { get; set; }
        public int? PrntId { get; set; }
        public int? SbIndx { get; set; }
        public int EntId { get; set; }
        public short RcSts { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDt { get; set; }

        public virtual ReCnfgRpt Prnt { get; set; }
        public virtual ICollection<ReCnfgRpt> InversePrnt { get; set; }
        public virtual ICollection<ReCnfgRptPrm> ReCnfgRptPrms { get; set; }
        public virtual ICollection<ReRptLog> ReRptLogs { get; set; }
        public virtual ICollection<ReStgngRpt> ReStgngRpts { get; set; }
    }
}
