using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Application.Interfaces;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.API.Controllers;

[Authorize]
public class WorkflowExecutionController : ApiControllerBase
{
    private readonly IWorkflowEngine _workflowEngine;
    private readonly WorkflowDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public WorkflowExecutionController(
        IWorkflowEngine workflowEngine,
        WorkflowDbContext context,
        IMapper mapper,
        ICurrentUserService currentUserService)
    {
        _workflowEngine = workflowEngine;
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartWorkflow([FromBody] StartWorkflowRequest request)
    {
        Guid requesterId = Guid.Empty;

        // 1. Explicit requester ID in request
        if (request.RequesterId.HasValue && request.RequesterId.Value != Guid.Empty)
        {
            requesterId = request.RequesterId.Value;
        }
        // 2. Or from InputData EmpSysId
        else if (request.InputData != null && request.InputData.TryGetValue("EmpSysId", out var empSysIdStr) && int.TryParse(empSysIdStr, out int empSysId))
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.EmpSysId == empSysId && !u.IsDeleted);
            if (user != null)
                requesterId = user.Id;
        }

        // 3. Fallback to JWT context
        if (requesterId == Guid.Empty)
        {
            var requesterIdStr = _currentUserService.UserId;
            if (string.IsNullOrEmpty(requesterIdStr) || !Guid.TryParse(requesterIdStr, out requesterId))
                return Unauthorized("User ID not resolved in JWT context.");
        }

        var instance = await _workflowEngine.StartWorkflowAsync(request.WorkflowCode, requesterId, request.InputData);
        
        return Ok(new { 
            Message = "Workflow instance started successfully.", 
            InstanceId = instance.Id, 
            InstanceNumber = instance.InstanceNumber 
        });
    }

    [HttpPost("approve")]
    public async Task<IActionResult> Approve([FromBody] ApproveRequest request)
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var approval = await _context.WorkflowApprovals.FirstOrDefaultAsync(a => a.Id == request.ApprovalId && !a.IsDeleted);
        if (approval == null)
            return BadRequest("Approval record not found.");

        var callingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Guid effectiveUserId = (callingUser != null && (callingUser.Username == "admin" || callingUser.Username == "system"))
            ? approval.ApproverId
            : userId;

        var success = await _workflowEngine.ApproveStepAsync(request.ApprovalId, effectiveUserId, request.Comment, request.DigitalSignature);
        if (!success) return BadRequest("Unable to approve task. Ensure it is pending and assigned to you.");

        var instanceStep = await _context.WorkflowInstanceSteps
            .Include(s => s.WorkflowInstance)
            .FirstOrDefaultAsync(s => s.Id == approval.WorkflowInstanceStepId);

        var instanceStatus = instanceStep?.WorkflowInstance?.Status.ToString() ?? "InProgress";
        var instanceId = instanceStep?.WorkflowInstanceId;

        return Ok(new { 
            Message = "Task approved successfully.",
            InstanceId = instanceId,
            Status = instanceStatus
        });
    }

    [HttpPost("reject")]
    public async Task<IActionResult> Reject([FromBody] RejectRequest request)
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var approval = await _context.WorkflowApprovals.FirstOrDefaultAsync(a => a.Id == request.ApprovalId && !a.IsDeleted);
        if (approval == null)
            return BadRequest("Approval record not found.");

        var callingUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        Guid effectiveUserId = (callingUser != null && (callingUser.Username == "admin" || callingUser.Username == "system"))
            ? approval.ApproverId
            : userId;

        var success = await _workflowEngine.RejectStepAsync(request.ApprovalId, effectiveUserId, request.Comment);
        if (!success) return BadRequest("Unable to reject task.");

        var instanceStep = await _context.WorkflowInstanceSteps
            .Include(s => s.WorkflowInstance)
            .FirstOrDefaultAsync(s => s.Id == approval.WorkflowInstanceStepId);

        var instanceStatus = instanceStep?.WorkflowInstance?.Status.ToString() ?? "Rejected";
        var instanceId = instanceStep?.WorkflowInstanceId;

        return Ok(new { 
            Message = "Task rejected successfully.",
            InstanceId = instanceId,
            Status = instanceStatus
        });
    }

    [HttpPost("return")]
    public async Task<IActionResult> Return([FromBody] ReturnRequest request)
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var success = await _workflowEngine.ReturnStepAsync(request.ApprovalId, userId, request.Comment);
        if (!success) return BadRequest("Unable to return task.");

        return Ok(new { Message = "Task returned successfully." });
    }

    [HttpPost("delegate")]
    public async Task<IActionResult> Delegate([FromBody] DelegateRequest request)
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var success = await _workflowEngine.DelegateStepAsync(request.ApprovalId, userId, request.DelegateUserId, request.Comment);
        if (!success) return BadRequest("Unable to delegate task.");

        return Ok(new { Message = "Task delegated successfully." });
    }

    [HttpGet("instance/{id}")]
    public async Task<ActionResult<WorkflowInstanceDto>> GetInstance(Guid id)
    {
        var instance = await _context.WorkflowInstances
            .Include(i => i.WorkflowVersion)
                .ThenInclude(v => v.WorkflowDefinition)
            .Include(i => i.Requester)
            .Include(i => i.Data)
            .Include(i => i.Steps)
                .ThenInclude(s => s.Approvals)
                    .ThenInclude(a => a.Approver)
            .Include(i => i.Steps)
                .ThenInclude(s => s.Approvals)
                    .ThenInclude(a => a.DelegatedFromUser)
            .Include(i => i.Histories)
                .ThenInclude(h => h.PerformedBy)
            .Include(i => i.Comments)
                .ThenInclude(c => c.User)
            .Include(i => i.Attachments)
                .ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

        if (instance == null) return NotFound();

        var dto = _mapper.Map<WorkflowInstanceDto>(instance);

        if (instance.WorkflowVersionId != Guid.Empty)
        {
            var versionSteps = await _context.WorkflowSteps
                .Where(s => s.WorkflowVersionId == instance.WorkflowVersionId && !s.IsDeleted && s.IsActive)
                .OrderBy(s => s.Sequence)
                .ThenBy(s => s.StepNumber)
                .ToListAsync();

            var targetUserIds = versionSteps
                .Where(s => !string.IsNullOrEmpty(s.TargetUserId) && Guid.TryParse(s.TargetUserId, out _))
                .Select(s => Guid.Parse(s.TargetUserId!))
                .Distinct()
                .ToList();

            var usersDict = targetUserIds.Any()
                ? await _context.Users.Where(u => targetUserIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.FullName)
                : new Dictionary<Guid, string>();

            foreach (var vStep in versionSteps)
            {
                if (!dto.Steps.Any(s => s.WorkflowStepId == vStep.Id || s.StepNumber == vStep.StepNumber))
                {
                    string approverName = "";
                    Guid targetGuid = Guid.Empty;
                    if (!string.IsNullOrEmpty(vStep.TargetUserId) && Guid.TryParse(vStep.TargetUserId, out targetGuid) && usersDict.TryGetValue(targetGuid, out var uName))
                    {
                        approverName = uName;
                    }
                    else if (vStep.ApprovalType == ApprovalType.DepartmentHead)
                    {
                        approverName = "Department Head";
                    }
                    else if (vStep.ApprovalType == ApprovalType.ManagerHierarchy)
                    {
                        Guid? prevUserId = null;
                        bool isDirectRM = vStep.Name.Contains("Reporting Manager", StringComparison.OrdinalIgnoreCase);
                        var prevStep = versionSteps.Where(s => s.Sequence < vStep.Sequence).OrderByDescending(s => s.Sequence).FirstOrDefault();
                        if (!isDirectRM && prevStep != null && prevStep.ApprovalType == ApprovalType.ManagerHierarchy && !string.IsNullOrEmpty(prevStep.TargetUserId) && Guid.TryParse(prevStep.TargetUserId, out var pId))
                        {
                            prevUserId = pId;
                        }
                        else
                        {
                            prevUserId = instance.RequesterId;
                        }

                        if (prevUserId.HasValue)
                        {
                            var prevUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == prevUserId.Value);
                            if (prevUser?.ManagerId != null)
                            {
                                var mgr = await _context.Users.FirstOrDefaultAsync(u => u.Id == prevUser.ManagerId.Value);
                                if (mgr != null)
                                {
                                    approverName = mgr.FullName;
                                    targetGuid = mgr.Id;
                                }
                            }
                        }

                        if (string.IsNullOrEmpty(approverName))
                        {
                            approverName = "Reporting Manager";
                        }
                    }
                    else
                    {
                        approverName = "Approver";
                    }

                    dto.Steps.Add(new WorkflowInstanceStepDto
                    {
                        Id = Guid.Empty,
                        WorkflowStepId = vStep.Id,
                        StepNumber = vStep.StepNumber,
                        Name = vStep.Name,
                        Status = StepStatus.Pending,
                        StartDate = DateTime.UtcNow,
                        Approvals = new List<WorkflowApprovalDto>
                        {
                            new WorkflowApprovalDto
                            {
                                Id = Guid.Empty,
                                ApproverId = targetGuid,
                                ApproverName = approverName,
                                Status = StepStatus.Pending
                            }
                        }
                    });
                }
            }
            dto.Steps = dto.Steps.OrderBy(s => s.StepNumber).ToList();
        }

        return Ok(dto);
    }

    [HttpGet("history/{instanceId}")]
    public async Task<ActionResult<IEnumerable<WorkflowHistoryDto>>> GetHistory(Guid instanceId)
    {
        var history = await _context.WorkflowHistories
            .Include(h => h.PerformedBy)
            .Where(h => h.WorkflowInstanceId == instanceId && !h.IsDeleted)
            .OrderBy(h => h.ActionDate)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<WorkflowHistoryDto>>(history));
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingApprovals([FromQuery] Guid? userId, [FromQuery] int? empSysId)
    {
        try
        {
            User? user = null;
            if (empSysId.HasValue && empSysId.Value > 0)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.EmpSysId == empSysId.Value && !u.IsDeleted);
            }

            if (user == null && userId.HasValue)
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId.Value && !u.IsDeleted);
            }

            if (user == null)
            {
                var userIdStr = _currentUserService.UserId;
                if (Guid.TryParse(userIdStr, out Guid uId))
                    user = await _context.Users.FirstOrDefaultAsync(u => u.Id == uId && !u.IsDeleted);
            }

            if (user == null) return Ok(new List<object>());

            var targetUserId = user.Id;

            var pendingApprovals = await _context.WorkflowApprovals
                .Include(a => a.WorkflowInstanceStep)
                .Where(a => (a.ApproverId == targetUserId || a.DelegatedFromUserId == targetUserId)
                         && a.Status == StepStatus.Pending
                         && !a.IsDeleted)
                .ToListAsync();

            var list = pendingApprovals
                .Where(a => a.WorkflowInstanceStep != null)
                .Select(a => new
                {
                    ApprovalId = a.Id,
                    InstanceId = a.WorkflowInstanceStep.WorkflowInstanceId,
                    Status = "Pending"
                });

            return Ok(list);
        }
        catch (Exception ex)
        {
            // Log and return empty list rather than 500
            return Ok(new List<object>());
        }
    }
}

