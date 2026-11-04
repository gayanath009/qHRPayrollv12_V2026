using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using qHRM.Areas.Leave.Models;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrLopTrns
    {
        [Key]
        public int LopId { get; set; }

        [Display(Name = "Leave Type")]
        [Required(ErrorMessage = "Select Leave Type")]
        public int LvTypId { get; set; }

        [Display(Name = "Employee (Outsource)")]
        [Required(ErrorMessage = "Select an Outsourced Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Transaction Date")]
        [DataType(DataType.Date)]
        public DateTime TrnDate { get; set; }

        [Display(Name = "Reference Number")]
        [StringLength(20)]
        public string Ref { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description cannot be blank")]
        [StringLength(200)]
        public string Desc { get; set; }

        [Display(Name = "LOP Days")]
        [Required(ErrorMessage = "LOP Days cannot be blank")]
        [Range(0.5, 31.0, ErrorMessage = "LOP Days must be between 0.5 and 31")]
        public decimal LopDays { get; set; }

        [Display(Name = "LOP Amount (QR.)")]
        [Remote(action: "ValidateAmount", controller: "HrPrLopTrns", ErrorMessage = "LOP Amount cannot be 0")]
        public decimal LopAmt { get; set; }

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
        public virtual HrLvType LvTyp { get; set; }
    }
}
