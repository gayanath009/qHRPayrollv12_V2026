using System;
using System.Collections.Generic;

namespace qHRM.Areas.Reports.Models
{
    public class DocumentExpiryReportVm
    {
        public string StaffCat { get; set; } = "ALL";
        public int HorizonDays { get; set; } = 90;
        public int ExpiredCount { get; set; }
        public int Expiring30Count { get; set; }
        public int Expiring60Count { get; set; }
        public int Expiring90Count { get; set; }
        public List<DocumentExpiryChartVm> ChartData { get; set; } = new List<DocumentExpiryChartVm>();
        public List<DocumentExpiryItemVm> Items { get; set; } = new List<DocumentExpiryItemVm>();
    }

    public class DocumentExpiryChartVm
    {
        public string DocType { get; set; }
        public int TotalCount { get; set; }
        public string Color { get; set; }
        public int Expired { get; set; }
        public int Days0To30 { get; set; }
        public int Days31To60 { get; set; }
        public int Days61To90 { get; set; }
    }

    public class DocumentExpiryItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public string DocType { get; set; }
        public DateTime ExpiryDate { get; set; }
        public string ExpiryDateStr => ExpiryDate.ToString("dd-MMM-yyyy");
        public int DaysRemaining { get; set; }
        public string Urgency { get; set; }
    }

    public class AttendanceAuditReportVm
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public int TotalRequests { get; set; }
        public int ApprovedCount { get; set; }
        public int PendingCount { get; set; }
        public int RejectedCancelledCount { get; set; }
        public decimal TotalRegularizedHours { get; set; }
        public List<ReasonSummaryVm> ReasonChartData { get; set; } = new List<ReasonSummaryVm>();
        public List<AttendanceAuditItemVm> Items { get; set; } = new List<AttendanceAuditItemVm>();
    }

    public class ReasonSummaryVm
    {
        public string ReasonCategory { get; set; }
        public int Approved { get; set; }
        public int Pending { get; set; }
        public int Rejected { get; set; }
    }

    public class AttendanceAuditItemVm
    {
        public string ReqNo { get; set; }
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public DateTime AttendanceDate { get; set; }
        public string AttendanceDateStr => AttendanceDate.ToString("dd-MMM-yyyy");
        public string ActualInOut { get; set; }
        public string RequestedInOut { get; set; }
        public decimal TotalHours { get; set; }
        public string TotalHoursStr { get; set; }
        public string ReasonCategory { get; set; }
        public string ReasonDescription { get; set; }
        public string WfStatus { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LeaveLiabilityReportVm
    {
        public int Year { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public decimal TotalOpeningDays { get; set; }
        public decimal TotalBookedDays { get; set; }
        public decimal TotalRemainingDays { get; set; }
        public decimal TotalLiabilityAmount { get; set; }
        public List<DeptLeaveSummaryVm> DeptChartData { get; set; } = new List<DeptLeaveSummaryVm>();
        public List<LeaveLiabilityItemVm> Items { get; set; } = new List<LeaveLiabilityItemVm>();
    }

    public class DeptLeaveSummaryVm
    {
        public string Department { get; set; }
        public string Color { get; set; }
        public decimal BookedDays { get; set; }
        public decimal RemainingDays { get; set; }
        public decimal LiabilityAmount { get; set; }
    }

    public class LeaveLiabilityItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public decimal OpeningDays { get; set; }
        public decimal BookedDays { get; set; }
        public decimal RemainingDays { get; set; }
        public int ApprovedTakenInYear { get; set; }
        public decimal MonthlySalary { get; set; }
        public decimal DailyRate { get; set; }
        public decimal EstimatedLiability { get; set; }
    }

    public class AnniversaryProbationReportVm
    {
        public string StaffCat { get; set; } = "ALL";
        public int HorizonDays { get; set; } = 60;
        public int ActiveProbationCount { get; set; }
        public int ProbationEndingSoonCount { get; set; }
        public int UpcomingFirstYearCount { get; set; }
        public int UpcomingMultiYearCount { get; set; }
        public List<MonthlyAnnivSummaryVm> MonthlyChartData { get; set; } = new List<MonthlyAnnivSummaryVm>();
        public List<AnniversaryItemVm> Anniversaries { get; set; } = new List<AnniversaryItemVm>();
        public List<ProbationItemVm> Probations { get; set; } = new List<ProbationItemVm>();
    }

    public class MonthlyAnnivSummaryVm
    {
        public string MonthLabel { get; set; }
        public int FirstYearTicketOnly { get; set; }
        public int MultiYearLeaveAndTicket { get; set; }
    }

    public class AnniversaryItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public DateTime JoinedDate { get; set; }
        public string JoinedDateStr => JoinedDate.ToString("dd-MMM-yyyy");
        public DateTime NextAnniversaryDate { get; set; }
        public string NextAnniversaryDateStr => NextAnniversaryDate.ToString("dd-MMM-yyyy");
        public int CompletedYears { get; set; }
        public int DaysUntil { get; set; }
        public string EncashmentEntitlement { get; set; }
    }

    public class ProbationItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public DateTime StDate { get; set; }
        public string StDateStr => StDate.ToString("dd-MMM-yyyy");
        public DateTime EndDate { get; set; }
        public string EndDateStr => EndDate.ToString("dd-MMM-yyyy");
        public int DaysRemaining { get; set; }
        public bool IsExtended { get; set; }
        public string Note { get; set; }
    }

    public class WorkflowSlaReportVm
    {
        public string Module { get; set; } = "ALL";
        public int TotalPending { get; set; }
        public int PendingOver48h { get; set; }
        public double AvgTurnaroundHours { get; set; }
        public int TotalProcessed { get; set; }
        public List<WorkflowAgingBucketVm> AgingChartData { get; set; } = new List<WorkflowAgingBucketVm>();
        public List<WorkflowSlaItemVm> Items { get; set; } = new List<WorkflowSlaItemVm>();
    }

    public class WorkflowAgingBucketVm
    {
        public string Bucket { get; set; }
        public int LeaveCount { get; set; }
        public int RegularizationCount { get; set; }
    }

    public class WorkflowSlaItemVm
    {
        public string Module { get; set; }
        public string Reference { get; set; }
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string Department { get; set; }
        public string RequestType { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string SubmittedAtStr => SubmittedAt.ToString("dd-MMM-yyyy HH:mm");
        public DateTime? CompletedAt { get; set; }
        public double AgingOrTurnaroundHours { get; set; }
        public string WfStatus { get; set; }
        public string SlaState { get; set; }
    }

    public class LeaveBalanceSummaryReportVm
    {
        public int Year { get; set; }
        public string StaffCat { get; set; } = "ALL";
        public string StatusFilter { get; set; } = "ALL";
        public decimal TotalAnnualOpening { get; set; }
        public decimal TotalAnnualBooked { get; set; }
        public decimal TotalSickBooked { get; set; }
        public decimal TotalCasualBooked { get; set; }
        public List<LeaveBalanceTypeChartVm> TypeChartData { get; set; } = new List<LeaveBalanceTypeChartVm>();
        public List<LeaveBalanceSummaryItemVm> Items { get; set; } = new List<LeaveBalanceSummaryItemVm>();
    }

    public class LeaveBalanceTypeChartVm
    {
        public string LeaveType { get; set; }
        public string Color { get; set; }
        public decimal Booked { get; set; }
        public decimal Balance { get; set; }
    }

    public class LeaveBalanceSummaryItemVm
    {
        public string EmpId { get; set; }
        public string FullName { get; set; }
        public string PhotoName { get; set; }
        public string ExitDateText { get; set; }
        public string Department { get; set; }
        public string StaffCategory { get; set; }
        public decimal OpeningBalance { get; set; }

        // Booked / Allocated display strings
        public string BA_Annual { get; set; }
        public string BA_Casual { get; set; }
        public string BA_Sick { get; set; }
        public string BA_Emergency { get; set; }
        public string BA_Maternity { get; set; }
        public string BA_Hajj { get; set; }

        // Remaining Balances
        public decimal Bal_Annual { get; set; }
        public decimal Bal_Casual { get; set; }
        public decimal Bal_Sick { get; set; }
        public decimal Bal_Emergency { get; set; }
        public decimal Bal_Maternity { get; set; }
        public decimal Bal_Hajj { get; set; }
    }
}
