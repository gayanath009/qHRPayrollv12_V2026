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
    public partial class HrPrProjAlwnc
    {
        public int ProjAlwncId { get; set; }

        [Display(Name = "Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Referance")]
        [Required(ErrorMessage = "Referance cannot be blank")]
        public string Ref { get; set; }


        [Display(Name = "Amount")]
        [Remote(action: "ValidateAmount", controller: "HrPrProjAlwnc", ErrorMessage = "Amount cannot be 0")]
        public decimal ProjAlwncAmt { get; set; }

        [Display(Name = "Effective Year")]
        public int EftYear { get; set; }

        [Display(Name = "Month")]
        public int EftMonth { get; set; }

        [Display(Name = "Period in Months")]
        [Remote(action: "ValidatePeriod", controller: "HrPrProjAlwnc", ErrorMessage = "Period cannot be 0")]
        public int Prds { get; set; }
        
        public int EndYear { get; set; }
        public int EndMonth { get; set; }

        [Display(Name = "Short Remark")]
        [StringLength(15)]
        public string ShtRmk { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }

        [NotMapped]
        public string ex_RecStatTxt { get { return (RecStat == 0) ? "Cancelled" : (RecStat == 1) ? "Active" : "Processed"; } }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
