using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfModule
    {
        public NtfModule()
        {
            NtfList = new HashSet<NtfList>();
        }

        public int ModId { get; set; }
        public string ModCode { get; set; }
        public string ModDesc { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual ICollection<NtfList> NtfList { get; set; }
    }
}
