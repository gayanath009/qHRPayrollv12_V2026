using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrSlryPrcLogDtl
    {
        public int PrcLogDtlId { get; set; }
        public int PrcLogId { get; set; }
        public int EmpSysId { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrSlryPrcLog PrcLog { get; set; }
    }
}
