using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfType
    {
        public NtfType()
        {
            NtfListTypeRef = new HashSet<NtfListTypeRef>();
        }

        public int TypId { get; set; }
        public string TypCode { get; set; }
        public string TypName { get; set; }
        public string TypDesc { get; set; }
        public bool? RecStat { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual ICollection<NtfListTypeRef> NtfListTypeRef { get; set; }
    }
}
