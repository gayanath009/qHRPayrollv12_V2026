using System;
using System.Collections.Generic;

namespace qHRM.Areas.Leave.Models
{
    public class LeaveApprovalQueueItemViewModel
    {
        public int LvId { get; set; }
        public int EmpSysId { get; set; }
        public string EmpId { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Designation { get; set; } = "";
        public int LvTypId { get; set; }
        public string LeaveType { get; set; } = "";
        public DateTime StDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Days { get; set; }
        public DateTime? AppliedDate { get; set; }
        public string Reason { get; set; } = "";
        public string Status { get; set; } = ""; // "Approved", "Pending", "Rejected", "Cancelled"
        public string PendingWith { get; set; } = "";
        public bool CanDirectApprove { get; set; }
        public bool CanDirectReject { get; set; }
        public int RecStat { get; set; }
        public string FormattedPeriod => $"{StDate:dd-MMM-yyyy} to {EndDate:dd-MMM-yyyy}";
        public string FormattedAppliedDate => AppliedDate.HasValue ? AppliedDate.Value.ToString("dd-MMM-yyyy HH:mm") : "—";
    }

    public class LeaveApprovalQueueViewModel
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string Filter { get; set; } = "all"; // all, pending, approved, rejected, cancelled
        public string SearchQuery { get; set; } = "";
        public int TotalAll { get; set; }
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalCancelled { get; set; }
        public bool UserCanApprove { get; set; }
        public List<LeaveApprovalQueueItemViewModel> Items { get; set; } = new List<LeaveApprovalQueueItemViewModel>();
        public string CycleLabel => $"{FromDate:dd MMM yyyy} — {ToDate:dd MMM yyyy}";
    }

    public class LeaveQueueActionRequest
    {
        public int LvId { get; set; }
        public string Comment { get; set; } = "";
    }
}

