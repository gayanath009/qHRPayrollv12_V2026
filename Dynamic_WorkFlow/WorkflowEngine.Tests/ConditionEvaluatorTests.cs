using System;
using System.Collections.Generic;
using WorkflowEngine.Application.Services;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using Xunit;

namespace WorkflowEngine.Tests;

public class ConditionEvaluatorTests
{
    private readonly ConditionEvaluator _evaluator;

    public ConditionEvaluatorTests()
    {
        _evaluator = new ConditionEvaluator();
    }

    [Fact]
    public void Evaluate_ShouldReturnTrue_WhenGroupIsNull()
    {
        // Arrange
        var inputData = new Dictionary<string, string>();

        // Act
        var result = _evaluator.Evaluate(null!, inputData);

        // Assert
        Assert.True(result);
    }



    [Fact]
    public void Evaluate_ShouldReturnTrue_WhenAmountGreaterThanLimit()
    {
        // Arrange
        var conditionGroup = new WorkflowConditionGroup
        {
            LogicalOperator = LogicalOperator.AND
        };

        conditionGroup.Conditions.Add(new WorkflowCondition
        {
            FieldName = "Amount",
            Operator = ConditionOperator.GreaterThan,
            Value = "10000"
        });

        var inputData = new Dictionary<string, string>
        {
            { "Amount", "12500" }
        };

        // Act
        var result = _evaluator.Evaluate(conditionGroup, inputData);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Evaluate_ShouldReturnFalse_WhenAmountLessThanLimit()
    {
        // Arrange
        var conditionGroup = new WorkflowConditionGroup
        {
            LogicalOperator = LogicalOperator.AND
        };

        conditionGroup.Conditions.Add(new WorkflowCondition
        {
            FieldName = "Amount",
            Operator = ConditionOperator.GreaterThan,
            Value = "10000"
        });

        var inputData = new Dictionary<string, string>
        {
            { "Amount", "8500" }
        };

        // Act
        var result = _evaluator.Evaluate(conditionGroup, inputData);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void Evaluate_ShouldEvaluateMultipleAndConditionsCorrectly()
    {
        // Arrange
        var conditionGroup = new WorkflowConditionGroup
        {
            LogicalOperator = LogicalOperator.AND
        };

        conditionGroup.Conditions.Add(new WorkflowCondition
        {
            FieldName = "LeaveType",
            Operator = ConditionOperator.Equals,
            Value = "Annual"
        });

        conditionGroup.Conditions.Add(new WorkflowCondition
        {
            FieldName = "LeaveDays",
            Operator = ConditionOperator.LessThanOrEqual,
            Value = "5"
        });

        var inputDataSuccess = new Dictionary<string, string>
        {
            { "LeaveType", "Annual" },
            { "LeaveDays", "3" }
        };

        var inputDataFail = new Dictionary<string, string>
        {
            { "LeaveType", "Sick" },
            { "LeaveDays", "3" }
        };

        // Act
        var successResult = _evaluator.Evaluate(conditionGroup, inputDataSuccess);
        var failResult = _evaluator.Evaluate(conditionGroup, inputDataFail);

        // Assert
        Assert.True(successResult);
        Assert.False(failResult);
    }
}
