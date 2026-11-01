using Microsoft.AspNetCore.Mvc;
using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrStfLoan
    {
        public HrPrStfLoan()
        {
            HrPrStfLoanPmt = new HashSet<HrPrStfLoanPmt>();
            HrPrStfLoanPmtExp = new HashSet<HrPrStfLoanPmtExp>();
            InverseRefStfLn = new HashSet<HrPrStfLoan>();
        }

        public int StfLnId { get; set; }

        [Display(Name = "Employee")]
        [Required(ErrorMessage = "Please specify the Employee")]
        public int EmpSysId { get; set; }

        [Display(Name = "Loan Type")]
        public int LnTypId { get; set; }

        [Display(Name = "Loan Code")]
        //[Required(ErrorMessage = "Code cannot be blank.")]
        [Remote(action: "IsAlreadyExisting", controller: "HrPrStfLoan", ErrorMessage = "Code is already existing", AdditionalFields = "ex_LnCode")]
        public string LnCode { get; set; }

        [NotMapped]
        public string ex_LnCode { get; set; }

        [Display(Name = "Loan Issue Date")]
        [DataType(DataType.Date)]
        public DateTime IssuDate { get; set; }

        [Display(Name = "Full Amount")]
        [Required(ErrorMessage = "Full amount should be grater than zero.")]
        
        public decimal FlAmt { get; set; }

        [Display(Name = "No of Installements")]
        [Required(ErrorMessage = "Installements should be grater than zero.")]
        public int Instl { get; set; }

        [Display(Name = "Installement Amount")]
        [Remote(action: "ChkwithSlry", controller: "HrPrStfLoan", ErrorMessage = "Installment amount can not be greater than the salary amount ", AdditionalFields = "EmpSysId")]
        public decimal InstlAmt { get; set; }

        [Display(Name = "Remaining Amount")]
        public decimal RmAmt { get; set; }

        [Display(Name = "Effective Year")]
        public int EftYear { get; set; }

        [Display(Name = "Month")]
        public int EftMonth { get; set; }

        [Display(Name = "Ref Loan")]
        public int? RefStfLnId { get; set; }

        [Display(Name = "Salary Remark")]
        [StringLength(15)]
        public string ShtRmk { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrStfLoanType LnTyp { get; set; }
        public virtual HrPrStfLoan RefStfLn { get; set; }
        public virtual ICollection<HrPrStfLoanPmt> HrPrStfLoanPmt { get; set; }
        public virtual ICollection<HrPrStfLoanPmtExp> HrPrStfLoanPmtExp { get; set; }
        public virtual ICollection<HrPrStfLoan> InverseRefStfLn { get; set; }
    }
}
