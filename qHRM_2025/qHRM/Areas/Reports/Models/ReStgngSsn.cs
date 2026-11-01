using System.Collections.Generic;
using System;

namespace qHRM.Areas.Reports.Models
{
    public partial class ReStgngSsn
    {
        public ReStgngSsn()
        {
            ReStgngRpts = new HashSet<ReStgngRpt>();
        }

        public long SsnId { get; set; }
        public bool? Exprt { get; set; }
        public bool? Prnt { get; set; }
        public short RcSts { get; set; }
        public int PrntdId { get; set; }
        public DateTime PrntdDt { get; set; }

        public virtual ICollection<ReStgngRpt> ReStgngRpts { get; set; }
    }
}
