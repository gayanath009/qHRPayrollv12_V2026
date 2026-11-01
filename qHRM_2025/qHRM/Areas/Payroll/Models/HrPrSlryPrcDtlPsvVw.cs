using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrSlryPrcDtlPsvVw
    {
        [Key]
        public int PrcPsvRecId { get; set; }
        public int PrcId { get; set; }
        public int PrcRvsn { get; set; }
        public int EmpSysId { get; set; }
        public decimal Basic { get; set; }
        public decimal HrAlwnc { get; set; }
        public decimal TrnsAlwnc { get; set; }
        public decimal Tra { get; set; }
        public decimal PstnAlwnc { get; set; }
        public decimal MedAlwnc { get; set; }
        public decimal MobAlwnc { get; set; }
        public decimal ProjAlwnc { get; set; }
        public decimal SpcAlwnc { get; set; }
        public decimal IncntvAmt { get; set; }
        public decimal PrfmAlwnc { get; set; }
        public decimal OthrAlwnc { get; set; }
        public decimal OthrPmt { get; set; }
        public decimal OtAmt { get; set; }
        public decimal BonusAmt { get; set; }
        public decimal AirTcktFee { get; set; }
        public decimal AnlLvEnchmnt { get; set; }
        public decimal GrsSlry { get; set; }
        public decimal SlryAdv { get; set; }
        public decimal StfLnPmt { get; set; }
        public decimal NoPayAmt { get; set; }
        public decimal OthrRcvry { get; set; }
        public decimal TotDctAmt { get; set; }
        public decimal NtSlry { get; set; }
        public short PmtMod { get; set; }
        public string BnkAcct { get; set; }
        public decimal WkDayCnt { get; set; }
        public decimal AbFct { get; set; }
        public short RecStat { get; set; }
        public string Rmks { get; set; }
        public DateTime CrtDateTime { get; set; }

        [NotMapped]
        public int PrcLogId { get; set; }
       
        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrSlryPrcHdr Prc { get; set; }
    }
}
