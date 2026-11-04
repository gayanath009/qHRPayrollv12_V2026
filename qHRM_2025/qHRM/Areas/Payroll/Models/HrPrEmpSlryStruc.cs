using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class HrPrEmpSlryStruc
    {
        public int SlStrcId { get; set; }
        public int EmpSysId { get; set; }
        public decimal Basic { get; set; }
        public decimal HrAlwnc { get; set; }
        public decimal TrnsAlwnc { get; set; }
        public decimal Tra { get; set; }
        public decimal PstnAlwnc { get; set; }
        [Column("Proj_Alwnc")]
        public decimal ProjAlwnc { get; set; }
        public decimal MedAlwnc { get; set; }
        public decimal MobAlwnc { get; set; }
        public decimal SpcAlwnc { get; set; }
        public decimal IncntvPrc { get; set; }
        public decimal PrfmAlwnc { get; set; }
        public decimal OthrAlwnc { get; set; }
        public decimal BonusRate { get; set; }
        public decimal AirTckt_Fee { get; set; }
        public int OtId { get; set; }
        public short RecStat { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrOtrate Ot { get; set; }
    }
}

