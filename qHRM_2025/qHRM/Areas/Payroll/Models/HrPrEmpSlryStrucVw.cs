using qHRM.Areas.Employee.Models;
using qHRM.Areas.Payroll.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrEmpSlryStrucVw
    {
        [Key]
        public int SlStrcId { get; set; }
        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Select Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Basic Salary")]
        [Range(0.00, 99999999.00)]
        [Required(ErrorMessage = "Basic Salary cannot be blank")]
        public decimal? Basic { get; set; }

        [Display(Name = "Accommodation Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? HrAlwnc { get; set; }

        [Display(Name = "Transport Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? TrnsAlwnc { get; set; }

        [Display(Name = "Trade Readjustment")]
        [Range(0.00, 99999999.00)]
        public decimal? Tra { get; set; }

        [Display(Name = "Position Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? PstnAlwnc { get; set; }

        [Display(Name = "Medical Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? MedAlwnc { get; set; }

        [Display(Name = "Mobile Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? MobAlwnc { get; set; }

        [Display(Name = "Project Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? ProjAlwnc { get; set; }

        [Display(Name = "Special Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? SpcAlwnc { get; set; }

        [Display(Name = "Incentive Percentage")]
        [Range(0.00, 10.00)]
        public decimal? IncntvPrc { get; set; }

        [Display(Name = "Performance Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? PrfmAlwnc { get; set; }

        [Display(Name = "Other Allowance")]
        [Range(0.00, 99999999.00)]
        public decimal? OthrAlwnc { get; set; }

        [Display(Name = "Bonus Rate")]
        [Range(0.00, 10.00)]
        public decimal? BonusRate { get; set; }

        [Display(Name = "Air Ticket Fee")]
        [Range(0.00, 99999999.00)]
        public decimal? AirTcktFee { get; set; }

        [Display(Name = "OT Rate Type")]
        public int OtId { get; set; }
        public short RecStat { get; set; }
        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrOtrate Ot { get; set; }
    }
}
