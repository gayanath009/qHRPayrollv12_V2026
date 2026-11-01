using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfRecipients
    {
        public int NtfRcptId { get; set; }
        public int? NtfTypRefId { get; set; }
        public string Recipients { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfListTypeRef NtfTypRef { get; set; }
    }
}
