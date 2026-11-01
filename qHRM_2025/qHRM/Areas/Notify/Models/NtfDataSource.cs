using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Notify.Models
{
    public partial class NtfDataSource
    {
        
        public int DsId { get; set; }
        public string DsCode { get; set; }
        public string DsDesc { get; set; }
        public int? NtfId { get; set; }
        public string SysTbl { get; set; }
        public string SysField { get; set; }
        public int? PriorDays { get; set; }
        public DateTime? CrtDateTime { get; set; }
        public virtual NtfList Ntf { get; set; }
    }
}
