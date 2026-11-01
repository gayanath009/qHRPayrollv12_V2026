using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrStfLoanPmt
    {
        public int LnPmtId { get; set; }

        [Display(Name = "Loan Code")]
        public int StfLnId { get; set; }

        [Display(Name = "Payment Date")]
        [Required(ErrorMessage = "Payment date is reqiured.")]
        [DataType(DataType.Date)]
        public DateTime PmtDate { get; set; }

        [Display(Name = "Installment")]
        public int Instl { get; set; }

        [Display(Name = "Payment Amount")]
        [Required(ErrorMessage = "Payment amount is reqiured.")]
        public decimal PmtAmt { get; set; }

        public int? PrcId { get; set; }
        public int? PrcRvsn { get; set; }

        [Display(Name = "Process Year")]
        public int PrcYear { get; set; }

        [Display(Name = "Process Month")]
        public int PrcMonth { get; set; }

        [Display(Name = "Effected Excepmtion")]
        public int? LnPmtExpId { get; set; }

        [Display(Name = "Payment Mode")]
        public short PmtMod { get; set; }

        [Display(Name = "Note")]
        public string Note { get; set; }
        public short RecStat { get; set; }
        public int CrtId { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrPrStfLoanPmtExp LnPmtExp { get; set; }
        public virtual HrPrSlryPrcHdr Prc { get; set; }
        public virtual HrPrStfLoan StfLn { get; set; }
    }
}
