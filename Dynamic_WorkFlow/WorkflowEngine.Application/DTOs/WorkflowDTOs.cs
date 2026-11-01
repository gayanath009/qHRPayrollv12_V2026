using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.DTOs;

public record WorkflowDefinitionDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    string? Module,
    string? Category,
    string? AssociatedLeaveTypes,
    string? TargetWorkType,
    bool IsActive
);

public record WorkflowVersionDto(
    Guid Id,
    Guid WorkflowDefinitionId,
    int VersionNumber,
    WorkflowStatus Status,
    DateTime? EffectiveDate,
    DateTime? ExpiryDate,
    int Priority,
    bool IsActive,
    List<WorkflowStepDto> Steps,
    string? WorkflowName = null,
    string? WorkflowCode = null,
    string? AssociatedLeaveTypes = null,
    string? TargetWorkType = null
);

public record WorkflowStepDto(
    Guid Id,
    Guid WorkflowVersionId,
    int StepNumber,
    string Name,
    ApprovalType ApprovalType,
    int Sequence,
    bool IsMandatory,
    bool ParallelApproval,
    int SLAHours,
    bool EmailNotification,
    bool SMSNotification,
    bool PushNotification,
    bool AutoApproval,
    bool RejectAllowed,
    bool ReturnAllowed,
    bool DelegateAllowed,
    bool CommentsMandatory,
    bool AttachmentMandatory,
    bool DigitalSignatureRequired,
    string? TargetWorkType,
    string? TargetRoleId,
    string? TargetUserId,
    string? TargetDepartmentId,
    string? ApiLookupUrl,
    List<WorkflowConditionGroupDto> SkipConditionGroups,
    List<WorkflowTransitionDto> TransitionsFrom
);

public record WorkflowConditionGroupDto(
    Guid Id,
    LogicalOperator LogicalOperator,
    List<WorkflowConditionDto> Conditions
);

public record WorkflowConditionDto(
    Guid Id,
    string FieldName,
    ConditionOperator Operator,
    string Value
);

public record WorkflowTransitionDto(
    Guid Id,
    Guid FromStepId,
    Guid ToStepId,
    string? Description,
    List<WorkflowConditionGroupDto> ConditionGroups
);

public record CreateWorkflowDto(
    string Name,
    string Code,
    string? Description,
    string? Module,
    string? Category,
    string? AssociatedLeaveTypes,
    string? TargetWorkType
);

public record UpdateWorkflowDto(
    string Name,
    string? Description,
    string? Module,
    string? Category,
    string? AssociatedLeaveTypes,
    string? TargetWorkType
);

public class UpdateStepDto
{
    public Guid? Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int StepNumber { get; set; }
    public int ApprovalType { get; set; }
    public int Sequence { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool ParallelApproval { get; set; }
    public int SLAHours { get; set; } = 24;
    public bool EmailNotification { get; set; } = true;
    public bool SMSNotification { get; set; }
    public bool CommentsMandatory { get; set; }
    public bool AttachmentMandatory { get; set; }
    public string? TargetWorkType { get; set; } = "ALL";
    public string? TargetUserId { get; set; }
    public string? TargetRoleId { get; set; }
}

