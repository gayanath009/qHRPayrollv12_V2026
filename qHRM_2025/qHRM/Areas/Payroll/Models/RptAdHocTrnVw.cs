using Microsoft.EntityFrameworkCore.Storage.ValueConversion.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class RptAdHocTrnVw
    {
        public string EmpId { get; set; }
        public string EmpName { get; set; }
        public int DptId { get; set; }
        public string DptName { get; set; }
        public int DsgId { get; set; }
        public string DsgName { get; set; }
        public DateTime TrnDate { get; set; }
        public string Ref { get; set; }
        public string Desc { get; set; }
        public decimal Amt { get; set; }  
    }
}
