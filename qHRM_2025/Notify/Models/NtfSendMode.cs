using System;
using System.Collections.Generic;

namespace Notify.Models
{
    public partial class NtfSendMode
    {
        public NtfSendMode()
        {
            NtfListTypeRef = new HashSet<NtfListTypeRef>();
        }

        public int NsmId { get; set; }
        public string NsmCode { get; set; }
        public string NsmDesc { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual ICollection<NtfListTypeRef> NtfListTypeRef { get; set; }
    }
}
