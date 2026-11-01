using System;
using System.Collections.Generic;

namespace qHRM.Areas.Leave.Models
{
    public class LeaveApprovalTimelineViewModel
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; }
        public string ApplicantName { get; set; }
        public string ApplicantCode { get; set; }
        public string LeaveType { get; set; }
        public string ReferenceNo { get; set; }
        public string InstanceNo { get; set; }
        public string Period { get; set; }
        public string TotalDuration { get; set; }
        public string OverallStatus { get; set; }
        public string StatusBadgeText { get; set; }
        public string LastUpdatedDate { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string SeatingLocation { get; set; } = string.Empty;
        public bool HasAttachment { get; set; } = false;
        public string AttachmentUrl { get; set; } = string.Empty;
        public string AttachmentFileName { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;
        public List<LeaveTimelineEventViewModel> Events { get; set; } = new List<LeaveTimelineEventViewModel>();
    }

    public class LeaveTimelineEventViewModel
    {
        public string Type { get; set; } // "submission" | "approval"
        public int StepNumber { get; set; }
        public string StepName { get; set; }
        public string RoleName { get; set; } // "Leave Initiator", "HR", "Reporting Manager", etc.
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string ActionDate { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Email { get; set; }
        public string Comment { get; set; }
        public string ActedIn { get; set; }
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
        public bool IsRejected { get; set; }
        public bool IsWaiting { get; set; }
    }
}
