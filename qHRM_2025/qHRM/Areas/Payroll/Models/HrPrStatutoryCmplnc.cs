using System;
using System.Collections.Generic;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrStatutoryCmplnc
    {
        public int StutryId { get; set; }
        public string StutryCode { get; set; }
        public string NameEng { get; set; }
        public string NameArb { get; set; }
        public string Desc { get; set; }
        public string DescArb { get; set; }
        public bool IsPrcnt { get; set; }
        public decimal Value { get; set; }
        public decimal MinSlry { get; set; }
        public decimal MaxSlry { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }
    }
}
