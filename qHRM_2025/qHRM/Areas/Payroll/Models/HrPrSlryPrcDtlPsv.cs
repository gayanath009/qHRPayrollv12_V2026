using qHRM.Areas.Employee.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public partial class HrPrSlryPrcDtlPsv
    {
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
        public decimal AtnPmtAmt { get; set; }
        public decimal OthrRcvry { get; set; }
        public decimal AtnRcvryAmt { get; set; }
        public decimal SlryAdv { get; set; }
        public decimal StfLnPmt { get; set; }
        public decimal BonusAmt { get; set; }
        public decimal OtAmt { get; set; }
        public decimal WkDayCnt { get; set; }
        public decimal NpAmt { get; set; }
        public decimal AirTckt_Fee { get; set; }
        public decimal Anl_Lv_Enchmnt { get; set; }
        public short PmtMod { get; set; }
        public string BnkAcct { get; set; }
        public string Rmks { get; set; }
        public short EmpStat { get; set; }
        public short RecStat { get; set; }
        public DateTime CrtDateTime { get; set; }

        public virtual HrEmpGenInfo EmpSys { get; set; }
        public virtual HrPrSlryPrcHdr Prc { get; set; }
    }
}
