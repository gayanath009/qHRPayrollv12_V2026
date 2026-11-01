using System;
using System.Collections.Generic;

namespace Notify.Models
{
    public partial class NtfList
    {
        public NtfList()
        {
            NtfDataSource = new HashSet<NtfDataSource>();
            NtfListTypeRef = new HashSet<NtfListTypeRef>();
        }

        public int NtfId { get; set; }
        public int? ModId { get; set; }
        public string NtfCode { get; set; }
        public string NtfName { get; set; }
        public string NtfDesc { get; set; }
        public DateTime? CrtDateTime { get; set; }
        public virtual NtfModule Mod { get; set; }
        public virtual ICollection<NtfDataSource> NtfDataSource { get; set; }
        public virtual ICollection<NtfListTypeRef> NtfListTypeRef { get; set; }
    }
}
