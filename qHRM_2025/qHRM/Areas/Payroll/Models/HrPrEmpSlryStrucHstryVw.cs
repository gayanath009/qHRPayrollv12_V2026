using qHRM.Areas.Employee.Models;
using qHRM.Areas.Payroll.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrEmpSlryStrucHstryVw
    {
        [Key]
        public int LogId { get; set; }
        public short LogTyp { get; set; }
        public int SlStrcId { get; set; }
        public int EmpSysId { get; set; }
        public decimal Basic { get; set; }
        public decimal HrAlwnc { get; set; }
        public decimal TrnsAlwnc { get; set; }
        public decimal Tra { get; set; }
        public decimal PstnAlwnc { get; set; }
        public decimal MedAlwnc { get; set; }
        public decimal MobAlwnc { get; set; }
        public decimal ProjAlwnc { get; set; }
        public decimal SpcAlwnc { get; set; }
        public decimal IncntvPrc { get; set; }
        public decimal PrfmAlwnc { get; set; }
        public decimal OthrAlwnc { get; set; }
        public decimal BonusRate { get; set; }
        public decimal AirTckt_Fee { get; set; }
        public int OtId { get; set; }
        public int OldModBy { get; set; }
        public DateTime OldModDateTime { get; set; }
        public int NewModBy { get; set; }
        public DateTime NewModDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrOtrate Ot { get; set; }
        public virtual HrPrEmpSlryStrucVw SlStrc { get; set; }
    }
}
