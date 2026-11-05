using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using qHRM.Areas.Employee.Models;

namespace qHRM.Areas.Payroll.Models
{
    [Table("HR_PrStaffLvTktEncash")]
    public partial class HrPrStaffLvTktEncash
    {
        [Key]
        [Column("Encash_Id")]
        public int EncashId { get; set; }

        [Column("Prc_Year")]
        public int PrcYear { get; set; }

        [Column("Prc_Month")]
        public int PrcMonth { get; set; }

        [Column("Emp_Sys_Id")]
        public int EmpSysId { get; set; }

        [Column("Staff_Category")]
        [StringLength(20)]
        public string StaffCategory { get; set; } // 'INTERNAL' or 'OUTSOURCE'

        [Column("Joined_Date")]
        public DateTime JoinedDate { get; set; }

        [Column("Vacation_Return_Date")]
        public DateTime? VacationReturnDate { get; set; }

        [Column("Next_Reset_Date")]
        public DateTime NextResetDate { get; set; }

        [Column("Applicable_Days")]
        public int ApplicableDays { get; set; }

        [Column("Eligible_Leave_Days", TypeName = "decimal(10, 5)")]
        public decimal EligibleLeaveDays { get; set; }

        [Column("Paid_Leave_Days", TypeName = "decimal(10, 2)")]
        public decimal PaidLeaveDays { get; set; }

        [Column("Net_Leave_Days")]
        public int NetLeaveDays { get; set; }

        [Column("Monthly_Salary", TypeName = "decimal(12, 2)")]
        public decimal MonthlySalary { get; set; }

        [Column("Daily_Salary", TypeName = "decimal(12, 2)")]
        public decimal DailySalary { get; set; }

        [Column("Total_Leave_Salary", TypeName = "decimal(12, 2)")]
        public decimal TotalLeaveSalary { get; set; }

        [Column("Tkt_Count")]
        public int TktCount { get; set; }

        [Column("Tkt_Price", TypeName = "decimal(12, 2)")]
        public decimal TktPrice { get; set; }

        [Column("Air_Ticket_Value", TypeName = "decimal(12, 2)")]
        public decimal AirTicketValue { get; set; }

        [Column("Total_Settlement", TypeName = "decimal(12, 2)")]
        public decimal TotalSettlement { get; set; }

        [Column("Remarks")]
        [StringLength(500)]
        public string Remarks { get; set; }

        [Column("Prc_Stat")]
        public short PrcStat { get; set; } = 1; // 1 = Draft / Calculated, 2 = Confirmed

        [Column("Wf_Stat")]
        [StringLength(30)]
        public string WfStat { get; set; } = "Draft";

        [Column("Wf_Inst_Id")]
        public Guid? WfInstId { get; set; }

        [Column("Crt_By")]
        public int CrtBy { get; set; } = 1;

        [Column("Crt_DateTime")]
        public DateTime CrtDateTime { get; set; } = DateTime.Now;

        [Column("Mod_By")]
        public int? ModBy { get; set; }

        [Column("Mod_DateTime")]
        public DateTime? ModDateTime { get; set; }

        [Column("Rec_Stat")]
        public short RecStat { get; set; } = 1;

        [ForeignKey("EmpSysId")]
        public virtual HrEmpGenInfo EmpSys { get; set; }
    }
}

