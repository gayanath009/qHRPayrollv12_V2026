using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace qHRM.Areas.Payroll.Models
{
    public class PrSalarySheetView
    {
        public int Index { get; set; }
        public int PrcPsvRecId { get; set; }
        public string Department { get; set; }
        public int Emp_Sys_Id { get; set; }
        public string Emp_Id { get; set; }
        public string Full_Name { get; set; }
        public string Designation { get; set; }
        public decimal BasicSalary { get; set; }

        //Allowances & Additions
        //======================================
        public decimal HouseRent { get; set; }
        public decimal Transport { get; set; }
        public decimal Position { get; set; }
        public decimal ProjectAllowance { get; set; }
        public decimal SpcAllowance { get; set; }
        public decimal Incentives { get; set; }
        public decimal Mobile { get; set; }
        public decimal Overtime { get; set; }
        public decimal MedicalAllowance { get; set; }
        public decimal BonusAmt { get; set; }        
        public decimal OthrAlwnc { get; set; }
        public decimal OthrPmt { get; set; }
        public decimal AirTcktFee { get; set; }
        public decimal AnlLvEnchmnt { get; set; }
        //======================================

        //-------------------------------------
        public decimal GrossSalary { get; set; }
        //-------------------------------------

        //Deductions
        //======================================
        public decimal SalaryAdvance { get; set; }
        public decimal StaffLoans { get; set; }
        public decimal NoPay { get; set; }
        public decimal OthrRcvryAmt { get; set; }
        //======================================

        //-------------------------------------
        public decimal TotalDeductions { get; set; }
        //-------------------------------------

        //Net Salary
        //======================================
        public decimal NtSlry { get; set; }
        public int PmtMod { get; set; }
        public string PmtModTxt { get; set; }
        public decimal BnkAmt { get; set; }
        public decimal CshAmt { get; set; }
        public string BnkAcct { get; set; }
        public int RecStat { get; set; }
        //======================================

        //======================================
        public int PrcId { get; set; } //Process ID 
        public int PrcLogId { get; set; } // Process Log ID
        public int EftMnt { get; set; } 
        public int EftYr { get; set; }
        //======================================

        public string Remarks { get; set; }
        public string lstPaySlip { get; set; }
        public string YrMntTxt { get; set; }
        public string PrntTime { get; set; }       
    }
}
