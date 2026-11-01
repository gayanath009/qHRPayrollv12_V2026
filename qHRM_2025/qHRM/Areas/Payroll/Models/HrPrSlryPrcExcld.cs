using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrSlryPrcExcld
    {
        public int ExcldId { get; set; }

        [Display(Name="Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Starting Year")]
        public int StYear { get; set; }

        [Display(Name = "Month")]
        public int StMonth { get; set; }

        [Display(Name = "Period (Months)")]
        public int NoMnths { get; set; }

        [Display(Name = "Special Notes")]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
