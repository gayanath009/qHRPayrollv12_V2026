using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrSlryAtnAdj
    {
        public int AtnAdjId { get; set; }
        public string AtnAdjTyp { get; set; }
        public int PrcId { get; set; }
        public int PrcRvsn { get; set; }
        public int EmpSysId { get; set; }
        public decimal AdjAmt { get; set; }
        public decimal Fct { get; set; }
        public string Remarks { get; set; }
        public string RmkExt { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrSlryPrcHdr Prc { get; set; }
    }
}
