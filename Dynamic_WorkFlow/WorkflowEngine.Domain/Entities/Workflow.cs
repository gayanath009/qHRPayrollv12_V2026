using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Common;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

public class WorkflowDefinition : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Module { get; set; }
    public string? Category { get; set; }
    public string? AssociatedLeaveTypes { get; set; } // Comma-separated or JSON list of leave codes e.g. "LV_ANL,LV_EMG_PD,LV_EMG_UPD"
    public string? TargetWorkType { get; set; } = "ALL"; // ALL, IN_HOUSE, OUTSOURCED
    public string? WorkflowType { get; set; } = "Leave Approval"; // Leave Approval, Salary Process, Leave Salary & Air Ticket Encashment
    
    public ICollection<WorkflowVersion> Versions { get; set; } = new List<WorkflowVersion>();
}

public class WorkflowVersion : BaseEntity
{
    public Guid WorkflowDefinitionId { get; set; }
    public WorkflowDefinition WorkflowDefinition { get; set; } = null!;
    
    public int VersionNumber { get; set; }
    public WorkflowStatus Status { get; set; } = WorkflowStatus.Draft;
    public DateTime? EffectiveDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int Priority { get; set; } = 1;
    
    public ICollection<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    public ICollection<WorkflowSetting> Settings { get; set; } = new List<WorkflowSetting>();
}

public class WorkflowStep : BaseEntity
{
    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion WorkflowVersion { get; set; } = null!;
    
    public int StepNumber { get; set; }
    public string Name { get; set; } = string.Empty;
    public ApprovalType ApprovalType { get; set; } = ApprovalType.SpecificUser;
    public int Sequence { get; set; }
    public bool IsMandatory { get; set; } = true;
    public bool ParallelApproval { get; set; } = false;
    public int SLAHours { get; set; } = 24;
    public bool EmailNotification { get; set; } = true;
    public bool SMSNotification { get; set; } = false;
    public bool PushNotification { get; set; } = false;
    public bool AutoApproval { get; set; } = false;
    public bool RejectAllowed { get; set; } = true;
    public bool ReturnAllowed { get; set; } = true;
    public bool DelegateAllowed { get; set; } = true;
    public bool CommentsMandatory { get; set; } = false;
    public bool AttachmentMandatory { get; set; } = false;
    public bool DigitalSignatureRequired { get; set; } = false;
    
    // Work Type Routing Filter ("ALL", "IN_HOUSE", "OUTSOURCED", "1", "2")
    public string? TargetWorkType { get; set; } = "ALL";

    // Config values for Role/User/API lookup
    public string? TargetRoleId { get; set; } // If RoleBased
    public string? TargetUserId { get; set; } // If SpecificUser
    public string? TargetDepartmentId { get; set; } // If DeptHead
    public string? ApiLookupUrl { get; set; } // If ApiLookup
    
    public ICollection<WorkflowConditionGroup> SkipConditionGroups { get; set; } = new List<WorkflowConditionGroup>();
    public ICollection<WorkflowApprover> Approvers { get; set; } = new List<WorkflowApprover>();
    public ICollection<WorkflowEscalation> Escalations { get; set; } = new List<WorkflowEscalation>();
    public ICollection<WorkflowTransition> TransitionsFrom { get; set; } = new List<WorkflowTransition>();
    public ICollection<WorkflowTransition> TransitionsTo { get; set; } = new List<WorkflowTransition>();
}

public class WorkflowConditionGroup : BaseEntity
{
    public Guid? WorkflowStepId { get; set; }
    public WorkflowStep? WorkflowStep { get; set; }
    
    public Guid? WorkflowTransitionId { get; set; }
    public WorkflowTransition? WorkflowTransition { get; set; }
    
    public LogicalOperator LogicalOperator { get; set; } = LogicalOperator.AND;
    
    public ICollection<WorkflowCondition> Conditions { get; set; } = new List<WorkflowCondition>();
}

public class WorkflowCondition : BaseEntity
{
    public Guid ConditionGroupId { get; set; }
    public WorkflowConditionGroup ConditionGroup { get; set; } = null!;
    
    public string FieldName { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;
    public string Value { get; set; } = string.Empty;
}

public class WorkflowAction : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // e.g. "Webhook", "Email", "DatabaseUpdate"
    public string Configuration { get; set; } = string.Empty; // JSON settings
}

public class WorkflowTransition : BaseEntity
{
    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion WorkflowVersion { get; set; } = null!;
    
    public Guid FromStepId { get; set; }
    public WorkflowStep FromStep { get; set; } = null!;
    
    public Guid ToStepId { get; set; }
    public WorkflowStep ToStep { get; set; } = null!;
    
    public string? Description { get; set; }
    
    public ICollection<WorkflowConditionGroup> ConditionGroups { get; set; } = new List<WorkflowConditionGroup>();
}

public class WorkflowRole : BaseEntity
{
    public string RoleName { get; set; } = string.Empty;
    public string RoleCode { get; set; } = string.Empty;
}

public class WorkflowApprover : BaseEntity
{
    public Guid WorkflowStepId { get; set; }
    public WorkflowStep WorkflowStep { get; set; } = null!;
    
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
}

public class WorkflowEscalation : BaseEntity
{
    public Guid WorkflowStepId { get; set; }
    public WorkflowStep WorkflowStep { get; set; } = null!;
    
    public int TimeoutHours { get; set; }
    public EscalationAction Action { get; set; } = EscalationAction.Reminder;
    public Guid? EscalateToUserId { get; set; }
    public User? EscalateToUser { get; set; }
    public bool IsProcessed { get; set; } = false;
}

public class WorkflowDelegation : BaseEntity
{
    public Guid DelegatorId { get; set; }
    public User Delegator { get; set; } = null!;
    
    public Guid DelegateeId { get; set; }
    public User Delegatee { get; set; } = null!;
    
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public bool IsPermanent { get; set; } = false;
}

public class WorkflowSetting : BaseEntity
{
    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion WorkflowVersion { get; set; } = null!;
    
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class WorkflowPermission : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class WorkflowUserPermission : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public WorkflowPermission Permission { get; set; } = null!;
}

public class WorkflowRolePermission : BaseEntity
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    
    public Guid PermissionId { get; set; }
    public WorkflowPermission Permission { get; set; } = null!;
}
