using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.DTOs;

public record StartWorkflowRequest(
    string WorkflowCode,
    Dictionary<string, string> InputData,
    Guid? RequesterId = null
);

public record ApproveRequest(
    Guid ApprovalId,
    string? Comment,
    string? DigitalSignature
);

public record RejectRequest(
    Guid ApprovalId,
    string? Comment
);

public record ReturnRequest(
    Guid ApprovalId,
    string? Comment
);

public record DelegateRequest(
    Guid ApprovalId,
    Guid DelegateUserId,
    string? Comment
);

public class WorkflowInstanceDto
{
    public Guid Id { get; set; }
    public Guid WorkflowVersionId { get; set; }
    public string WorkflowCode { get; set; } = string.Empty;
    public string WorkflowName { get; set; } = string.Empty;
    public string InstanceNumber { get; set; } = string.Empty;
    public WorkflowInstanceStatus Status { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public List<WorkflowInstanceDataDto> Data { get; set; } = new();
    public List<WorkflowInstanceStepDto> Steps { get; set; } = new();
    public List<WorkflowHistoryDto> History { get; set; } = new();
    public List<WorkflowCommentDto> Comments { get; set; } = new();
    public List<WorkflowAttachmentDto> Attachments { get; set; } = new();
}

public class WorkflowInstanceDataDto
{
    public string FieldName { get; set; } = string.Empty;
    public string? FieldValue { get; set; }
}

public class WorkflowInstanceStepDto
{
    public Guid Id { get; set; }
    public Guid WorkflowStepId { get; set; }
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? DueDate { get; set; }
    public List<WorkflowApprovalDto> Approvals { get; set; } = new();
}

public class WorkflowApprovalDto
{
    public Guid Id { get; set; }
    public Guid ApproverId { get; set; }
    public string ApproverName { get; set; } = string.Empty;
    public StepStatus Status { get; set; }
    public DateTime? ActionDate { get; set; }
    public string? Comment { get; set; }
    public string? DigitalSignature { get; set; }
    public Guid? DelegatedFromUserId { get; set; }
    public string? DelegatedFromUserName { get; set; }
}

public class WorkflowHistoryDto
{
    public Guid Id { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Guid PerformedById { get; set; }
    public string PerformedByName { get; set; } = string.Empty;
    public DateTime ActionDate { get; set; }
    public string? Comment { get; set; }
}

public class WorkflowCommentDto
{
    public Guid Id { get; set; }
    public Guid? StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}

public class WorkflowAttachmentDto
{
    public Guid Id { get; set; }
    public Guid? StepId { get; set; }
    public string StepName { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public Guid UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
