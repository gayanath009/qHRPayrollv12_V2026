using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.General.Models;
using qHRM.Areas.Leave.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrStfLoanType
    {
        public HrPrStfLoanType()
        {
            HrPrStfLoan = new HashSet<HrPrStfLoan>();
        }

        public int LnTypId { get; set; }

        [Display(Name = "Loan Type Code")]
        [Required(ErrorMessage = "Code cannot be blank.")]
        [Remote(action: "IsAlreadyExisting", controller: "HrPrStfLoanType", ErrorMessage = "Code is already existing", AdditionalFields = "ex_LnTypCode")]
        public string LnTypCode { get; set; }

        [NotMapped]
        public string ex_LnTypCode { get; set; }

        [Display(Name = "Description")]
        public string Desc { get; set; }

        [Display(Name = "Description")]
        public string DescArb { get; set; }

        [Display(Name = "Maximum Amount")]
        [Required(ErrorMessage = "Amount cannot be blank.")]
        public decimal MaxAmt { get; set; }

        [Display(Name = "Maximum Installments")]
        [Required(ErrorMessage = "Installments cannot be blank.")]
        public int MaxInstl { get; set; }

        [Display(Name = "Is Salary Advance")]
        public bool SlryAdv { get; set; }
        public int EntId { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual OrgEntity Ent { get; set; }
        public virtual ICollection<HrPrStfLoan> HrPrStfLoan { get; set; }
    }
}
