using System;
using System.Collections.Generic;

namespace Notify.Models
{
    public partial class NtfScheduleLog
    {
        public int SchId { get; set; }
        public DateTime? SchProcessStart { get; set; }
        public DateTime? SchProcessEnd { get; set; }
        public string SchLogfileRef { get; set; }
        public string SchErrorLogfileRef { get; set; }
        public bool? SchStat { get; set; }

        public virtual NtfScheduleHeader Sch { get; set; }
    }
}
