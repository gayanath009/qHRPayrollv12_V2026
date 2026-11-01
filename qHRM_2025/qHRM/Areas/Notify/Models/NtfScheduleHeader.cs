using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfScheduleHeader
    {
        public NtfScheduleHeader()
        {
            NtfScheduleDetail = new HashSet<NtfScheduleDetail>();
            NtfScheduleLog = new HashSet<NtfScheduleLog>();
        }

        public int SchId { get; set; }
        public int? ProId { get; set; }
        public string SchDesc { get; set; }
        public DateTime? SchStartDate { get; set; }
        public DateTime? SchEndDate { get; set; }
        public bool? SchInfinity { get; set; }
        public TimeSpan? SchStartTime { get; set; }
        public string SchFrequency { get; set; }
        public string SchDaysOfWeek { get; set; }
        public string SchDaysOfMonth { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfProcess Pro { get; set; }
        public virtual ICollection<NtfScheduleDetail> NtfScheduleDetail { get; set; }
        public virtual ICollection<NtfScheduleLog> NtfScheduleLog { get; set; }
    }
}
