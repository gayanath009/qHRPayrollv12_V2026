using System;
using System.Collections.Generic;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfMsgTmplParamaters
    {
        public int ParamId { get; set; }
        public int? TmplId { get; set; }
        public string ParamValue { get; set; }
        public string ParamType { get; set; }
        public DateTime? CrtDateTime { get; set; }

        public virtual NtfMsgTmpl Tmpl { get; set; }
    }
}
