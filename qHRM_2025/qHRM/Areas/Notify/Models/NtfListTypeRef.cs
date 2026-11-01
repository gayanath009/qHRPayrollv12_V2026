using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfListTypeRef
    {
        public NtfListTypeRef()
        {
            NtfNotification = new HashSet<NtfNotification>();
            NtfRecipients = new HashSet<NtfRecipients>();
            NtfScheduleDetail = new HashSet<NtfScheduleDetail>();
        }

        public int NtfTypRefId { get; set; }
        public int? NtfId { get; set; }
        public int? TypId { get; set; }
        public int? NsmId { get; set; }
        public int? TmplId { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfSendMode Nsm { get; set; }
        public virtual NtfList Ntf { get; set; }
        public virtual NtfMsgTmpl Tmpl { get; set; }
        public virtual NtfType Typ { get; set; }
        public virtual ICollection<NtfNotification> NtfNotification { get; set; }
        public virtual ICollection<NtfRecipients> NtfRecipients { get; set; }
        public virtual ICollection<NtfScheduleDetail> NtfScheduleDetail { get; set; }
    }
}
