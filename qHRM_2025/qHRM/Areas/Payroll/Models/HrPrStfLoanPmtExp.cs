using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrStfLoanPmtExp
    {
        public HrPrStfLoanPmtExp()
        {
            HrPrStfLoanPmt = new HashSet<HrPrStfLoanPmt>();
        }

        public int LnPmtExpId { get; set; }
        public int StfLnId { get; set; }

        [Display(Name = "Entry Date")]
        [DataType(DataType.Date)]
        public DateTime EntryDate { get; set; }

        [Display(Name = "Accepted Payment Amount")]
        [Required(ErrorMessage = "Accepted payment amount is reqiured.")]
        public decimal ExpPmtAmt { get; set; }

        [Display(Name = "Effective Year")]
        //[Remote(action: "ValidateEftYM", controller: "HrPrStfLoanPmt", ErrorMessage = "Effective Year & Month should be grater than Salary Processed year & Month", AdditionalFields = "EftMonth")]
        public int EftYear { get; set; }

        [Display(Name = "Effective Month")]
        //[Remote(action: "ValidateEftYM", controller: "HrPrStfLoanPmt", ErrorMessage = "Effective Year & Month should be grater than Salary Processed year & Month", AdditionalFields = "EftYear")]
        public int EftMonth { get; set; }

        [Display(Name = "Excepmtion Installments")]
        //[Remote(action: "ValidateExpInstl", controller: "HrPrStfLoanPmt", ErrorMessage = "Excepmtion installments cannot be 0")]
        public int ExpInstl { get; set; }

        [Display(Name = "Note")]
        [Required(ErrorMessage = "Reason is required.")]
        public string Note { get; set; }

        [Display(Name = "Status")]
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrPrStfLoan StfLn { get; set; }
        public virtual ICollection<HrPrStfLoanPmt> HrPrStfLoanPmt { get; set; }
    }
}