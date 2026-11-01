using System;
using System.Collections.Generic;

namespace Notify.Models
{
    public partial class NtfMsgTmpl
    {
        public NtfMsgTmpl()
        {
            NtfListTypeRef = new HashSet<NtfListTypeRef>();
            NtfMsgTmplParamaters = new HashSet<NtfMsgTmplParamaters>();
        }

        public int TmplId { get; set; }
        public string TmplCode { get; set; }
        public string TmplDesc { get; set; }
        public string MessageSubject { get; set; }
        public string MessageBody { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual ICollection<NtfListTypeRef> NtfListTypeRef { get; set; }
        public virtual ICollection<NtfMsgTmplParamaters> NtfMsgTmplParamaters { get; set; }
    }
}
