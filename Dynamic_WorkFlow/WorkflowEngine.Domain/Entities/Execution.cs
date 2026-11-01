using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Common;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

public class WorkflowInstance : BaseEntity
{
    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion WorkflowVersion { get; set; } = null!;
    
    public string InstanceNumber { get; set; } = string.Empty; // e.g. WRQ-2026-00001
    public WorkflowInstanceStatus Status { get; set; } = WorkflowInstanceStatus.InProgress;
    public Guid RequesterId { get; set; }
    public User Requester { get; set; } = null!;
    
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    
    public ICollection<WorkflowInstanceData> Data { get; set; } = new List<WorkflowInstanceData>();
    public ICollection<WorkflowInstanceStep> Steps { get; set; } = new List<WorkflowInstanceStep>();
    public ICollection<WorkflowHistory> Histories { get; set; } = new List<WorkflowHistory>();
    public ICollection<WorkflowComment> Comments { get; set; } = new List<WorkflowComment>();
    public ICollection<WorkflowAttachment> Attachments { get; set; } = new List<WorkflowAttachment>();
    public ICollection<WorkflowVariable> Variables { get; set; } = new List<WorkflowVariable>();
}

public class WorkflowInstanceData : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; } // Can store scalar values or JSON array for grids
}

public class WorkflowInstanceStep : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public Guid WorkflowStepId { get; set; }
    public WorkflowStep WorkflowStep { get; set; } = null!;
    
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime? EndDate { get; set; }
    public DateTime? DueDate { get; set; }
    
    public ICollection<WorkflowApproval> Approvals { get; set; } = new List<WorkflowApproval>();
}

public class WorkflowApproval : BaseEntity
{
    public Guid WorkflowInstanceStepId { get; set; }
    public WorkflowInstanceStep WorkflowInstanceStep { get; set; } = null!;
    
    public Guid ApproverId { get; set; }
    public User Approver { get; set; } = null!;
    
    public StepStatus Status { get; set; } = StepStatus.Pending;
    public DateTime? ActionDate { get; set; }
    public string? Comment { get; set; }
    public string? DigitalSignature { get; set; }
    
    // In case of delegation
    public Guid? DelegatedFromUserId { get; set; }
    public User? DelegatedFromUser { get; set; }
}

public class WorkflowHistory : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public string StepName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty; // e.g. "Started", "Approved", "Rejected", "Returned", "Delegated", "Escalated", "Cancelled"
    public Guid PerformedById { get; set; }
    public User PerformedBy { get; set; } = null!;
    
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;
    public string? Comment { get; set; }
}

public class WorkflowComment : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public Guid? WorkflowStepId { get; set; }
    public WorkflowStep? WorkflowStep { get; set; }
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string Content { get; set; } = string.Empty;
}

public class WorkflowAttachment : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public Guid? WorkflowStepId { get; set; }
    public WorkflowStep? WorkflowStep { get; set; }
    
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    
    public Guid UploadedById { get; set; }
    public User UploadedBy { get; set; } = null!;
}

public class WorkflowVariable : BaseEntity
{
    public Guid WorkflowInstanceId { get; set; }
    public WorkflowInstance WorkflowInstance { get; set; } = null!;
    
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
