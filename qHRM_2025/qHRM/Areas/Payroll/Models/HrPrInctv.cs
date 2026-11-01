using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrInctv
    {
        public int InctvId { get; set; }

        [Display(Name = "Employee")]
        //[Remote(action: "ValidateEmployee", controller: "HrPrInctv", ErrorMessage = "Select Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Incentive Type")]
        [Required(ErrorMessage = "Select Incentive Type")]
        public int InctvTyp { get; set; }

        [Display(Name = "Incentive Mode")]
        [Required(ErrorMessage = "Select Incentive Mode")]
        public int InctvMod { get; set; }

        [Display(Name = "Entry Date")]
        [DataType(DataType.Date)]
        public DateTime InctvDate { get; set; }

        [Display(Name = "Referance Number")]
        [Required(ErrorMessage = "Referance Number cannot be blank")]
        [StringLength(20)]
        public string Ref { get; set; }

        [Display(Name = "Description")]
        [Required(ErrorMessage = "Description cannot be blank")]
        [StringLength(200)]
        public string Desc { get; set; }

        [Display(Name = "Amount")]
        [Remote(action: "ValidateAmount", controller: "HrPrInctv", ErrorMessage = "Amount cannot be 0")]
        public decimal InctvAmt { get; set; }

        [Display(Name = "Month")]
        public int EftMonth { get; set; }

        [Display(Name = "Effective Year")]
        public int EftYear { get; set; }

        [Display(Name = "Short Remark")]
        [StringLength(35)]
        public string ShtRmk { get; set; }

        [Display(Name = "Status")]
        public short RecStat { get; set; }

        [NotMapped]
        public string ex_RecStatTxt { get { return (RecStat == 0) ? "Cancelled" : (RecStat == 1) ? "Active" : "Processed"; } }

        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
