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
public class InboxController : ApiControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUserService;

    public InboxController(WorkflowDbContext context, IMapper mapper, ICurrentUserService currentUserService)
    {
        _context = context;
        _mapper = mapper;
        _currentUserService = currentUserService;
    }

    [HttpGet("pending")]
    public async Task<ActionResult<IEnumerable<PendingTaskDto>>> GetPendingTasks()
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var pendingApprovals = await _context.WorkflowApprovals
            .Include(a => a.WorkflowInstanceStep)
                .ThenInclude(s => s.WorkflowInstance)
                    .ThenInclude(i => i.WorkflowVersion)
                        .ThenInclude(v => v.WorkflowDefinition)
            .Include(a => a.WorkflowInstanceStep)
                .ThenInclude(s => s.WorkflowInstance)
                    .ThenInclude(i => i.Requester)
            .Where(a => a.ApproverId == userId && a.Status == StepStatus.Pending && !a.IsDeleted)
            .ToListAsync();

        var dtos = pendingApprovals.Select(a => new PendingTaskDto(
            a.Id,
            a.WorkflowInstanceStep.WorkflowInstanceId,
            a.WorkflowInstanceStep.WorkflowInstance.InstanceNumber,
            a.WorkflowInstanceStep.WorkflowInstance.WorkflowVersion.WorkflowDefinition.Name,
            a.WorkflowInstanceStep.Name,
            a.WorkflowInstanceStep.WorkflowInstance.Requester.FullName,
            a.WorkflowInstanceStep.WorkflowInstance.StartDate,
            a.WorkflowInstanceStep.DueDate ?? DateTime.UtcNow.AddDays(1)
        ));

        return Ok(dtos);
    }

    [HttpGet("completed")]
    public async Task<ActionResult<IEnumerable<PendingTaskDto>>> GetCompletedTasks()
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var completedApprovals = await _context.WorkflowApprovals
            .Include(a => a.WorkflowInstanceStep)
                .ThenInclude(s => s.WorkflowInstance)
                    .ThenInclude(i => i.WorkflowVersion)
                        .ThenInclude(v => v.WorkflowDefinition)
            .Include(a => a.WorkflowInstanceStep)
                .ThenInclude(s => s.WorkflowInstance)
                    .ThenInclude(i => i.Requester)
            .Where(a => a.ApproverId == userId && a.Status != StepStatus.Pending && !a.IsDeleted)
            .OrderByDescending(a => a.ActionDate)
            .ToListAsync();

        var dtos = completedApprovals.Select(a => new PendingTaskDto(
            a.Id,
            a.WorkflowInstanceStep.WorkflowInstanceId,
            a.WorkflowInstanceStep.WorkflowInstance.InstanceNumber,
            a.WorkflowInstanceStep.WorkflowInstance.WorkflowVersion.WorkflowDefinition.Name,
            a.WorkflowInstanceStep.Name,
            a.WorkflowInstanceStep.WorkflowInstance.Requester.FullName,
            a.WorkflowInstanceStep.WorkflowInstance.StartDate,
            a.ActionDate ?? DateTime.UtcNow
        ));

        return Ok(dtos);
    }

    [HttpGet("requests")]
    public async Task<ActionResult<IEnumerable<WorkflowInstanceDto>>> GetMyRequests()
    {
        var userIdStr = _currentUserService.UserId;
        if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out Guid userId))
            return Unauthorized();

        var instances = await _context.WorkflowInstances
            .Include(i => i.WorkflowVersion)
                .ThenInclude(v => v.WorkflowDefinition)
            .Include(i => i.Requester)
            .Where(i => i.RequesterId == userId && !i.IsDeleted)
            .OrderByDescending(i => i.StartDate)
            .ToListAsync();

        return Ok(_mapper.Map<IEnumerable<WorkflowInstanceDto>>(instances));
    }
}
