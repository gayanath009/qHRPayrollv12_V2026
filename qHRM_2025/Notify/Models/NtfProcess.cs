using System;
using System.Collections.Generic;

namespace Notify.Models
{
    public partial class NtfProcess
    {
        public NtfProcess()
        {
            NtfScheduleHeader = new HashSet<NtfScheduleHeader>();
        }

        public int ProId { get; set; }
        public string ProName { get; set; }
        public string ProDesc { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual ICollection<NtfScheduleHeader> NtfScheduleHeader { get; set; }
    }
}
