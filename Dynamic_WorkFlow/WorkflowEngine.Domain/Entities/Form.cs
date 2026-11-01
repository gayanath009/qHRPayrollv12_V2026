using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Common;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Domain.Entities;

public class WorkflowForm : BaseEntity
{
    public Guid WorkflowVersionId { get; set; }
    public WorkflowVersion WorkflowVersion { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    
    public ICollection<WorkflowField> Fields { get; set; } = new List<WorkflowField>();
    public ICollection<WorkflowRule> Rules { get; set; } = new List<WorkflowRule>();
}

public class WorkflowField : BaseEntity
{
    public Guid FormId { get; set; }
    public WorkflowForm Form { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty; // Code name (e.g. "LeaveDays")
    public string Label { get; set; } = string.Empty; // Label displayed (e.g. "Number of Leave Days")
    public FieldType Type { get; set; } = FieldType.TextBox;
    public string? Placeholder { get; set; }
    public string? DefaultValue { get; set; }
    public string? ValidationRegex { get; set; }
    public bool IsRequired { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public int Order { get; set; }
    public string? GridColumns { get; set; } // If dynamic grid, store JSON definition of columns
    
    public ICollection<WorkflowFieldOption> Options { get; set; } = new List<WorkflowFieldOption>();
}

public class WorkflowFieldOption : BaseEntity
{
    public Guid FieldId { get; set; }
    public WorkflowField Field { get; set; } = null!;
    
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public class WorkflowRule : BaseEntity
{
    public Guid FormId { get; set; }
    public WorkflowForm Form { get; set; } = null!;
    
    public string Name { get; set; } = string.Empty;
    
    public ICollection<WorkflowRuleCondition> Conditions { get; set; } = new List<WorkflowRuleCondition>();
    public ICollection<WorkflowRuleAction> Actions { get; set; } = new List<WorkflowRuleAction>();
}

public class WorkflowRuleCondition : BaseEntity
{
    public Guid RuleId { get; set; }
    public WorkflowRule Rule { get; set; } = null!;
    
    public string FieldName { get; set; } = string.Empty;
    public ConditionOperator Operator { get; set; } = ConditionOperator.Equals;
    public string Value { get; set; } = string.Empty;
}

public class WorkflowRuleAction : BaseEntity
{
    public Guid RuleId { get; set; }
    public WorkflowRule Rule { get; set; } = null!;
    
    public string ActionType { get; set; } = string.Empty; // e.g. "Show", "Hide", "Enable", "Disable", "SetValue"
    public string TargetFieldName { get; set; } = string.Empty;
    public string? Value { get; set; }
}
