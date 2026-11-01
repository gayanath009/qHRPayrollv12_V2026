using System;
using System.Collections.Generic;

namespace qHRM.Areas.Leave.Models
{
    public class EmpLeaveBalanceViewModel
    {
        public int LvTypId { get; set; }
        public string LeaveTypeCode { get; set; }
        public string LeaveTypeName { get; set; }
        public decimal Entitlement { get; set; }
        public decimal InProgress { get; set; }
        public decimal Used { get; set; }
        public decimal Remaining { get; set; }
        public bool IsApplicable { get; set; } = true;
        public string RuleNotice { get; set; }
        public string BadgeClass { get; set; }

        // Accrual & Reset Metadata
        public bool IsAccrualBased { get; set; }
        public decimal AccruedDays { get; set; }
        public decimal AnnualQuota { get; set; }
        public decimal OpeningDays { get; set; }
        public decimal AccrualRatePerMonth { get; set; } = 2.5m;
        public DateTime? JoinedDate { get; set; }
        public DateTime? ServicePeriodStart { get; set; }
        public DateTime? ServicePeriodEnd { get; set; }
        public DateTime? NextResetDate { get; set; }
        public string AccrualFormulaText { get; set; }
        public decimal CurrentCycleRemaining { get; set; }
        public decimal NextCycleRemaining { get; set; }
        public DateTime? CurrentCycleResetDate { get; set; }

        // Service Year Requirement Policy
        public bool HasCompletedOneYear { get; set; } = true;
        public DateTime? OneYearCompletionDate { get; set; }
        public bool IsServiceRestricted { get; set; } = false;
        public string RestrictionReason { get; set; }
    }

    public class EmpLeaveBalanceSummaryViewModel
    {
        public int EmpSysId { get; set; }
        public string EmployeeName { get; set; }
        public string FullName { get; set; }
        public string EmpId { get; set; }
        public string Department { get; set; }
        public string Gender { get; set; }
        public string Religion { get; set; }
        public bool IsFemale { get; set; }
        public bool IsMuslim { get; set; }
        public int Year { get; set; }
        public int LeaveYear { get; set; }
        public decimal TotalEntitlement { get; set; }
        public decimal TotalInProgress { get; set; }
        public decimal TotalUsed { get; set; }
        public decimal TotalRemaining { get; set; }
        public List<EmpLeaveBalanceViewModel> Balances { get; set; } = new List<EmpLeaveBalanceViewModel>();
        public List<EmpLeaveBalanceViewModel> LeaveBalances { get => Balances; set => Balances = value; }
    }

    public class LeaveBalanceTransactionViewModel
    {
        public int TrnId { get; set; }
        public string LeaveTypeName { get; set; }
        public string LeaveTypeCode { get; set; }
        public string TransactionType { get; set; }
        public string TrnType { get => TransactionType; set => TransactionType = value; }
        public int? LvId { get; set; }
        public decimal Days { get; set; }
        public decimal DeltaDays { get => Days; set => Days = value; }
        public decimal BalanceBefore { get; set; }
        public decimal BalanceAfter { get; set; }
        public string Remarks { get; set; }
        public string DateFormatted { get; set; }
        public string TransactionDateDisplay { get => DateFormatted; set => DateFormatted = value; }
    }
}
