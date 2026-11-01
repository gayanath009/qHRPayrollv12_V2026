using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class PrSlryShtVw
    {
        public int Index { get; set; }
        public int EmpSysId { get; set; }
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string DesigName { get; set; }
        public string DeptName { get; set; }
        public decimal Basic { get; set; }
        public bool InHouse { get; set; }

        //Allowances
        //======================================
        public decimal HrAlwnc { get; set; }
        public decimal TrnsAlwnc { get; set; }
        public decimal PstnAlwnc { get; set; }
        public decimal ProjAlwnc { get; set; }
        public decimal MedAlwnc { get; set; }
        public decimal MobAlwnc { get; set; }
        public decimal PrfmAlwnc { get; set; }
        public decimal SpcAlwnc { get; set; }  
        public decimal OthrAlwnc { get; set; }
        public decimal IncntvAmt { get; set; }
        public decimal Mobile { get; set; }
        public decimal Overtime { get; set; }
        public decimal BonusAmt { get; set; }      
        public decimal OthrPmt { get; set; }
        public decimal OtAmt { get; set; }
        //======================================

        //-------------------------------------
        public decimal GrsSlry { get; set; }
        public decimal WkDayCnt { get; set; }
        public decimal AbFct { get; set; }
        //-------------------------------------

        //Deductions
        //======================================
        public decimal SlryAdv { get; set; }
        public decimal StfLnPmt { get; set; }
        public decimal NoPayAmt { get; set; }
        public decimal OthrRcvryAmt { get; set; }
        //======================================

        //-------------------------------------
        public decimal TotDctAmt { get; set; }
        //-------------------------------------

        //Net Salary
        //======================================
        public decimal NtSlry { get; set; }
        public int PmtMod { get; set; }
        public string PmtModTxt { get; set; }
        public decimal BnkAmt { get; set; }
        public decimal CshAmt { get; set; }
        public string BnkAcct { get; set; }
        public string Rmks { get; set; }
        public int RecStat { get; set; }
        //======================================

        //======================================    
        public int PrcId { get; set; }
        public int PrcLogId { get; set; }
        public int EftMnt { get; set; } 
        public int EftYr { get; set; }
        //======================================
    }
}
