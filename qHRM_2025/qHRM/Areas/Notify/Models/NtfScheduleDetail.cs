using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Notify.Models
{

    public partial class NtfScheduleDetail
    {

        public int AutoId { get; set; }
        public int? SchId { get; set; }
        public int? NtfTypRefId { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfListTypeRef NtfTypRef { get; set; }
        public virtual NtfScheduleHeader Sch { get; set; }
    }
}
