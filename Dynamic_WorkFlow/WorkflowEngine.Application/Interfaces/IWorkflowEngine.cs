using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Interfaces;

public interface IWorkflowEngine
{
    Task<WorkflowInstance> StartWorkflowAsync(string workflowCode, Guid requesterId, Dictionary<string, string> inputData);
    Task<bool> ApproveStepAsync(Guid approvalId, Guid userId, string? comment, string? signature);
    Task<bool> RejectStepAsync(Guid approvalId, Guid userId, string? comment);
    Task<bool> ReturnStepAsync(Guid approvalId, Guid userId, string? comment);
    Task<bool> DelegateStepAsync(Guid approvalId, Guid userId, Guid delegateUserId, string? comment);
    Task<bool> EscalateStepAsync(Guid stepInstanceId);
}
