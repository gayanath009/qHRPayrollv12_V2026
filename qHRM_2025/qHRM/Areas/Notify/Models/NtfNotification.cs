using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfNotification
    {
        [Key]
        public int MsgId { get; set; }
        public int? NtfTypRefId { get; set; }
        public int? ReqId { get; set; }
        public int? EmpSysId { get; set; }
        public string Subject { get; set; }
        public string Message { get; set; }
        public bool? PushReadStat { get; set; }
        public bool? IsActionTaken { get; set; }
        public int? IsActionTakenBy { get; set; }
        public int? IsDelivered { get; set; }
        public int? EftMnth { get; set; }
        public int? EftYr { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfListTypeRef NtfTypRef { get; set; }
    }
}
