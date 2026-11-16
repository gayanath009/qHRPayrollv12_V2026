using System;
using System.Collections.Generic;

namespace qHRM.Areas.Attendance.Models
{
    public class RegularizationApprovalQueueViewModel
    {
        public DateTime CycleStart { get; set; }
        public DateTime CycleEnd { get; set; }
        public string ActiveFilter { get; set; } = "all";
        public string SearchQuery { get; set; } = "";

        public int TotalCount { get; set; }
        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CancelledCount { get; set; }

        public bool CanApprove { get; set; }
        public int UserEmpSysId { get; set; }
        public int UserGrpId { get; set; }

        public List<RegularizationQueueItemViewModel> Items { get; set; } = new List<RegularizationQueueItemViewModel>();
    }

    public class RegularizationQueueItemViewModel
    {
        public int AtnExpId { get; set; }
        public int EmpSysId { get; set; }
        public string EmpId { get; set; } = "";
        public string EmpName { get; set; } = "";
        public string Department { get; set; } = "";
        public string Designation { get; set; } = "";

        public DateTime AttendanceDate { get; set; }
        public string ReqNo { get; set; } = "";

        public TimeSpan? ReqInTime { get; set; }
        public TimeSpan? ReqOutTime { get; set; }
        public TimeSpan? ActualInTime { get; set; }
        public TimeSpan? ActualOutTime { get; set; }
        public decimal TotalHours { get; set; }

        public string ReasonCategory { get; set; } = "";
        public string ReasonDescription { get; set; } = "";

        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Cancelled
        public string PendingWith { get; set; } = "";
        public string ApproverName { get; set; } = "";
        public DateTime? ApprovalDateTime { get; set; }
        public string ApprovalRemarks { get; set; } = "";
        public DateTime CreatedDateTime { get; set; }

        public bool CanAction { get; set; }
    }

    public class RegQueueActionRequest
    {
        public int AtnExpId { get; set; }
        public string Comment { get; set; } = "";
    }
}

