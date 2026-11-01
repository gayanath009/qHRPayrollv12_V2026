using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Travel
{
    public partial class HrTrvlTktDtl
    {
        public int TrvlTktId { get; set; }
        public int EmpSysId { get; set; }
        public string RefTyp { get; set; }
        public int RefId { get; set; }
        public int? DpndId { get; set; }
        public string RefNo { get; set; }
        public DateTime TktDate { get; set; }
        public bool CmpTkt { get; set; }
        public decimal TktAmt { get; set; }
        public decimal AprdTktAmt { get; set; }
        public string Rmks { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpDependent Dpnd { get; set; }
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
