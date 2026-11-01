using Microsoft.AspNetCore.Mvc;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using qHRM.Areas.Employee.CustomValidation;


namespace qHRM.Areas.Employee.Models
{
    public partial class HrEmpBankAcct
    {
        public int AcctId { get; set; }
        public int EmpSysId { get; set; }

        [Display(Name = "Acount Number")]
        [Required(ErrorMessage = "Please specify Account Number")]
        [Remote(action: "IsExistAcctNo", controller: "HrEmpBankAcct", ErrorMessage = "Account Number is already existing", AdditionalFields = "ex_AcctNo")]
        [StringLength(35)]
        public string AcctNo { get; set; }

        [NotMapped]
        public string ex_AcctNo { get; set; }

        [Required(ErrorMessage = "Please specify Account Name")]
        [Display(Name = "Acount Name")]
        [StringLength(100)]
        public string AcctName { get; set; }

        [Required(ErrorMessage = "Please specify Account Type")]
        [Display(Name = "Acount Type")]
        public string AcctType { get; set; }

        //[Required(ErrorMessage = "Please specify IBAN")]
        [StringLength(maximumLength: 30, MinimumLength = 25, ErrorMessage = "IBAN length should bb 25 to 30")]
        [Display(Name = "IBAN")]
        public string Iban { get; set; }

        [Required(ErrorMessage = "Please specify Bank Name")]
        [Display(Name = "Bank Name")]
        [StringLength(50)]
        public string BankName { get; set; }

        //[Required(ErrorMessage = "Please specify Branch")]
        [Display(Name = "Branch")]
        public string BranchName { get; set; }

        //[Required(ErrorMessage = "Please specify Account Open Date")]
        [Display(Name = "Account Open Date")]
        [DataType(DataType.Date)]
        [Remote(action: "ValidateBankAccountOpenDate", controller: "HrEmpBankAcct", ErrorMessage = "Invalid Account Open Date")]
        public DateTime? OpndDate { get; set; }

        [Display(Name = "Salary Account")]
        [Remote(action: "IsSetSalaryAcct", controller: "HrEmpBankAcct", ErrorMessage = "Salary Account has already set", AdditionalFields = "ex_AcctNo")]
        public bool SlryAcct { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        [Display(Name = "Bank Short Name")]
        [StringLength(10)]
        public string ShtBankName { get; set; }

        [JsonIgnore]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}
