using System;
using System.Collections.Generic;

namespace qHRM.Areas.Reports.Models
{
    public class MonthlyCostTrendReportVm
    {
        public decimal LatestNetPayout { get; set; }
        public decimal LatestGross { get; set; }
        public decimal LatestOvertime { get; set; }
        public decimal LatestDeductions { get; set; }
        public decimal NetMomPct { get; set; }
        public decimal GrossMomPct { get; set; }
        public decimal OtMomPct { get; set; }
        public decimal DedMomPct { get; set; }
        public List<MonthlyCostTrendItemVm> Months { get; set; } = new List<MonthlyCostTrendItemVm>();
    }

    public class MonthlyCostTrendItemVm
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string MonthLabel { get; set; }
        public int Headcount { get; set; }
        public decimal Basic { get; set; }
        public decimal Allowances { get; set; }
        public decimal Overtime { get; set; }
        public decimal Gross { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetPayout { get; set; }
        public decimal MomVariancePct { get; set; }
    }

    public class DepartmentCostBreakdownReportVm
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public decimal TotalNetPayroll { get; set; }
        public decimal TotalEncashment { get; set; }
        public decimal TotalProjectAllowance { get; set; }
        public decimal CombinedCost { get; set; }
        public List<DepartmentCostDonutVm> DonutData { get; set; } = new List<DepartmentCostDonutVm>();
        public List<DepartmentCostItemVm> DeptItems { get; set; } = new List<DepartmentCostItemVm>();
        public List<DepartmentEmpCostItemVm> EmpItems { get; set; } = new List<DepartmentEmpCostItemVm>();
    }

    public class DepartmentCostDonutVm
    {
        public string Department { get; set; }
        public decimal TotalCost { get; set; }
        public string Color { get; set; }
    }

    public class DepartmentCostItemVm
    {
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public int EmployeeCount { get; set; }
        public decimal PayrollGross { get; set; }
        public decimal EncashmentCost { get; set; }
        public decimal ProjectAllowance { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class DepartmentEmpCostItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public decimal PayrollNet { get; set; }
        public decimal EncashmentCost { get; set; }
        public decimal ProjectAllowance { get; set; }
        public decimal TotalCost { get; set; }
    }

    public class EncashmentForecastReportVm
    {
        public string StaffCat { get; set; } = "ALL";
        public decimal TotalForecastAmount { get; set; }
        public decimal FirstYearTicketAmount { get; set; }
        public decimal MultiYearLeaveTicketAmount { get; set; }
        public int TotalEligibleEmployees { get; set; }
        public List<EncashmentForecastMonthVm> MonthlyChart { get; set; } = new List<EncashmentForecastMonthVm>();
        public List<EncashmentForecastItemVm> Items { get; set; } = new List<EncashmentForecastItemVm>();
    }

    public class EncashmentForecastMonthVm
    {
        public string MonthLabel { get; set; }
        public decimal FirstYearTicketAmount { get; set; }
        public decimal MultiYearAmount { get; set; }
    }

    public class EncashmentForecastItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public string AnniversaryMonth { get; set; }
        public DateTime AnniversaryDate { get; set; }
        public string AnniversaryDateStr => AnniversaryDate.ToString("dd-MMM-yyyy");
        public int CompletedYears { get; set; }
        public string EntitlementRule { get; set; }
        public decimal EstLeaveSalary { get; set; }
        public decimal EstAirTicket { get; set; }
        public decimal TotalForecast { get; set; }
    }

    public class DepartmentTurnoverReportVm
    {
        public int PeriodMonths { get; set; } = 12;
        public string StaffCat { get; set; } = "ALL";
        public int ActiveHeadcount { get; set; }
        public int ExitsInPeriod { get; set; }
        public int JoinersInPeriod { get; set; }
        public decimal TurnoverRatePct { get; set; }
        public List<DepartmentTurnoverSummaryVm> DeptSummaries { get; set; } = new List<DepartmentTurnoverSummaryVm>();
        public List<EmployeeExitItemVm> ExitDetails { get; set; } = new List<EmployeeExitItemVm>();
    }

    public class DepartmentTurnoverSummaryVm
    {
        public string Department { get; set; }
        public int ActiveCount { get; set; }
        public int JoinersInPeriod { get; set; }
        public int Exits3M { get; set; }
        public int Exits6M { get; set; }
        public int Exits12M { get; set; }
        public int ExitsSelected { get; set; }
        public decimal TurnoverRatePct { get; set; }
        public decimal AvgTenureYears { get; set; }
    }

    public class EmployeeExitItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public DateTime JoinedDate { get; set; }
        public string JoinedDateStr => JoinedDate.ToString("dd-MMM-yyyy");
        public DateTime ExitDate { get; set; }
        public string ExitDateStr => ExitDate.ToString("dd-MMM-yyyy");
        public decimal TenureYears { get; set; }
    }

    public class PayrollProcessHistoryReportVm
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public string ProcessStatus { get; set; } = "Not Processed";
        public int EmployeeCount { get; set; }
        public decimal TotalBasic { get; set; }
        public decimal TotalAllowances { get; set; }
        public decimal TotalOvertime { get; set; }
        public decimal TotalGross { get; set; }
        public decimal TotalDeductions { get; set; }
        public decimal TotalNetPayout { get; set; }
        public List<int> AvailableYears { get; set; } = new List<int>();
        public List<PayrollProcessHistoryItemVm> Items { get; set; } = new List<PayrollProcessHistoryItemVm>();
    }

    public class PayrollProcessHistoryItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public decimal Basic { get; set; }
        public decimal Allowances { get; set; }
        public decimal Overtime { get; set; }
        public decimal GrossSalary { get; set; }
        public decimal Deductions { get; set; }
        public decimal NetSalary { get; set; }
    }

    public class EncashmentHistoryReportVm
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public string WorkflowStatus { get; set; } = "Not Processed";
        public int EmployeeCount { get; set; }
        public decimal TotalLeaveSalary { get; set; }
        public decimal TotalAirTicket { get; set; }
        public decimal TotalSettlement { get; set; }
        public List<int> AvailableYears { get; set; } = new List<int>();
        public List<EncashmentHistoryItemVm> Items { get; set; } = new List<EncashmentHistoryItemVm>();
    }

    public class EncashmentHistoryItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public DateTime JoinedDate { get; set; }
        public string JoinedDateStr => JoinedDate.ToString("dd-MMM-yyyy");
        public DateTime NextResetDate { get; set; }
        public string NextResetDateStr => NextResetDate.ToString("dd-MMM-yyyy");
        public int ApplicableDays { get; set; }
        public decimal EligibleLeaveDays { get; set; }
        public decimal PaidLeaveDays { get; set; }
        public int NetLeaveDays { get; set; }
        public decimal MonthlySalary { get; set; }
        public decimal TotalLeaveSalary { get; set; }
        public int TktCount { get; set; }
        public decimal AirTicketValue { get; set; }
        public decimal TotalSettlement { get; set; }
        public string Remarks { get; set; }
    }
}
