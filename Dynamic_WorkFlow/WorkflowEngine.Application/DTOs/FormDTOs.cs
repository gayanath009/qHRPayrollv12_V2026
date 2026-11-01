using System;
using System.Collections.Generic;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.DTOs;

public record WorkflowFormDto(
    Guid Id,
    Guid WorkflowVersionId,
    string Name,
    string? Description,
    List<WorkflowFieldDto> Fields,
    List<WorkflowRuleDto> Rules
);

public record WorkflowFieldDto(
    Guid Id,
    string Name,
    string Label,
    FieldType Type,
    string? Placeholder,
    string? DefaultValue,
    string? ValidationRegex,
    bool IsRequired,
    bool IsReadOnly,
    int Order,
    string? GridColumns,
    List<WorkflowFieldOptionDto> Options
);

public record WorkflowFieldOptionDto(
    Guid Id,
    string Label,
    string Value
);

public record WorkflowRuleDto(
    Guid Id,
    string Name,
    List<WorkflowRuleConditionDto> Conditions,
    List<WorkflowRuleActionDto> Actions
);

public record WorkflowRuleConditionDto(
    Guid Id,
    string FieldName,
    ConditionOperator Operator,
    string Value
);

public record WorkflowRuleActionDto(
    Guid Id,
    string ActionType,
    string TargetFieldName,
    string? Value
);

public record CreateFormDto(
    Guid WorkflowVersionId,
    string Name,
    string? Description,
    List<WorkflowFieldDto> Fields
);
