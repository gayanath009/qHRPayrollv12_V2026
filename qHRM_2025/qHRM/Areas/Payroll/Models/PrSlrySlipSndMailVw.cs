using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrSlrySlipSndMailVw
    {
        [Key]
        public int EmpSysId { get; set; }
        public string EmpId { get; set; }
        public string PstName { get; set; }
        public string IsDlvrd { get; set; }
    }
}
