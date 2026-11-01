using System;

namespace qHRM.Areas.Reports.Models
{
    public partial class ReRptLog
    {
        public long RptLogId { get; set; }
        public int SsnId { get; set; }
        public int RptId { get; set; }
        public string Prms { get; set; }
        public bool Exprt { get; set; }
        public bool Prnt { get; set; }
        public short RcSts { get; set; }
        public int PrntdId { get; set; }
        public DateTime PrntdDt { get; set; }

        public virtual ReCnfgRpt Rpt { get; set; }
    }
}
