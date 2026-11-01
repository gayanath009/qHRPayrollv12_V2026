using System;
using System.Collections.Generic;

namespace qHRM.Areas.Leave.Models
{
    public class LeaveIncidentReviewViewModel
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;

        public int LvId { get; set; }
        public Guid? WfInstanceId { get; set; }
        public string InstanceNo { get; set; } = string.Empty;
        public string WfStatus { get; set; } = string.Empty;
        public string WfStatusBadge { get; set; } = "badge-info";

        // Employee Info
        public int EmpSysId { get; set; }
        public string EmpId { get; set; } = string.Empty;
        public string EmployeeName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;

        // Leave Details
        public int LvTypId { get; set; }
        public string LeaveType { get; set; } = string.Empty;
        public DateTime? StDate { get; set; }
        public DateTime? EndDate { get; set; }
        public decimal Days { get; set; }
        public string Reason { get; set; } = string.Empty;
        public string SeatingLocation { get; set; } = string.Empty;
        public DateTime? SubmittedOn { get; set; }
        public string SubmittedBy { get; set; } = string.Empty;

        // Attachment Info
        public bool HasAttachment { get; set; } = false;
        public string AttachmentUrl { get; set; } = string.Empty;
        public string AttachmentFileName { get; set; } = string.Empty;
        public string AttachmentType { get; set; } = string.Empty;

        // Approver Action Context for current logged-in user
        public bool CanApprove { get; set; } = false;
        public Guid? ApprovalId { get; set; }
        public string CurrentStepName { get; set; } = string.Empty;
        public int CurrentStepNumber { get; set; }

        // Timeline & History
        public LeaveApprovalTimelineViewModel Timeline { get; set; } = new LeaveApprovalTimelineViewModel();
    }
}
