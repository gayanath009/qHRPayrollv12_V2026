using System;
using System.Collections.Generic;
using System.Linq;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;

namespace WorkflowEngine.Application.Services;

public class ConditionEvaluator
{
    public bool Evaluate(WorkflowConditionGroup group, Dictionary<string, string> inputData)
    {
        if (group == null) return true;
        if (group.Conditions == null || !group.Conditions.Any()) return true;

        bool result = group.LogicalOperator == LogicalOperator.AND;

        foreach (var condition in group.Conditions)
        {
            bool condResult = EvaluateCondition(condition, inputData);

            if (group.LogicalOperator == LogicalOperator.AND)
            {
                result = result && condResult;
                if (!result) break; // Short-circuit AND
            }
            else if (group.LogicalOperator == LogicalOperator.OR)
            {
                result = result || condResult;
                if (result) break; // Short-circuit OR
            }
            else if (group.LogicalOperator == LogicalOperator.NOT)
            {
                result = !condResult;
                break;
            }
        }

        return result;
    }

    private bool EvaluateCondition(WorkflowCondition condition, Dictionary<string, string> inputData)
    {
        if (!inputData.TryGetValue(condition.FieldName, out var fieldValue))
        {
            fieldValue = string.Empty;
        }

        string conditionValue = condition.Value ?? string.Empty;

        // Try numeric conversion first for comparisons
        switch (condition.Operator)
        {
            case ConditionOperator.Equals:
                return string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase);
            
            case ConditionOperator.NotEquals:
                return !string.Equals(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.GreaterThan:
                if (double.TryParse(fieldValue, out double val1) && double.TryParse(conditionValue, out double val2))
                    return val1 > val2;
                return string.Compare(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase) > 0;

            case ConditionOperator.LessThan:
                if (double.TryParse(fieldValue, out double lVal1) && double.TryParse(conditionValue, out double lVal2))
                    return lVal1 < lVal2;
                return string.Compare(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase) < 0;

            case ConditionOperator.GreaterThanOrEqual:
                if (double.TryParse(fieldValue, out double geVal1) && double.TryParse(conditionValue, out double geVal2))
                    return geVal1 >= geVal2;
                return string.Compare(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase) >= 0;

            case ConditionOperator.LessThanOrEqual:
                if (double.TryParse(fieldValue, out double leVal1) && double.TryParse(conditionValue, out double leVal2))
                    return leVal1 <= leVal2;
                return string.Compare(fieldValue, conditionValue, StringComparison.OrdinalIgnoreCase) <= 0;

            case ConditionOperator.Contains:
                return fieldValue.Contains(conditionValue, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.StartsWith:
                return fieldValue.StartsWith(conditionValue, StringComparison.OrdinalIgnoreCase);

            case ConditionOperator.EndsWith:
                return fieldValue.EndsWith(conditionValue, StringComparison.OrdinalIgnoreCase);

            default:
                return false;
        }
    }
}
