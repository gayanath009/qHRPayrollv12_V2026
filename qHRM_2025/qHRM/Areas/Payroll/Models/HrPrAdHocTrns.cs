using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrAdHocTrns
    {
        public int TrnId { get; set; }

        [Display(Name = "Transaction Type")]
        [Required(ErrorMessage = "Select Transaction Type")]
        public int TrnTypId { get; set; }

        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Select an Employee")]        
        public int EmpSysId { get; set; }

        [Display(Name = "Transaction Date")]
        [DataType(DataType.Date)]
        public DateTime TrnDate { get; set; }

        [Display(Name = "Referance Number")]
        [StringLength(20)]
        public string Ref { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description cannot be blank")]
        [StringLength(200)]
        public string Desc { get; set; }

        [Display(Name = "Amount")]
        [Remote(action: "ValidateAmount", controller: "HrPrAdHocTrns", ErrorMessage = "Amount cannot be 0")]
        public decimal TrnAmt { get; set; }

        [Display(Name = "Month")]
        public int EftMonth { get; set; }

        [Display(Name = "Effective Year")]
        public int EftYear { get; set; }
        public int? PrcLogId { get; set; }

        [Display(Name = "Short Remark")]
        [StringLength(35)]
        public string ShtRmk { get; set; }

        [Display(Name = "Status")]
        public short RecStat { get; set; }

        [NotMapped]
        public string ex_RecStatTxt { get { return (RecStat == 0) ? "Cancelled" : (RecStat == 1) ? "Active" : "Processed"; } }

        public int CrtBy { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrSlryPrcLog PrcLog { get; set; }
        public virtual HrPrAdHocTrnType TrnTyp { get; set; }
    }
}
