using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.API.Controllers;

[Authorize]
public class DashboardController : ApiControllerBase
{
    private readonly WorkflowDbContext _context;

    public DashboardController(WorkflowDbContext context)
    {
        _context = context;
    }

    [HttpGet("metrics")]
    public async Task<ActionResult<DashboardMetricsDto>> GetMetrics()
    {
        // Counts
        int total = await _context.WorkflowInstances.CountAsync(i => !i.IsDeleted);
        int pending = await _context.WorkflowInstances.CountAsync(i => i.Status == WorkflowInstanceStatus.InProgress && !i.IsDeleted);
        int approved = await _context.WorkflowInstances.CountAsync(i => i.Status == WorkflowInstanceStatus.Approved && !i.IsDeleted);
        int rejected = await _context.WorkflowInstances.CountAsync(i => i.Status == WorkflowInstanceStatus.Rejected && !i.IsDeleted);

        // SLA violations
        int slaViolations = await _context.WorkflowInstanceSteps
            .CountAsync(s => s.Status == StepStatus.Pending && s.DueDate.HasValue && s.DueDate.Value < DateTime.UtcNow && !s.IsDeleted);

        // Average approval duration
        var approvedSteps = await _context.WorkflowInstanceSteps
            .Where(s => s.Status == StepStatus.Approved && s.EndDate.HasValue && !s.IsDeleted)
            .ToListAsync();

        double avgHours = 0;
        if (approvedSteps.Any())
        {
            avgHours = approvedSteps.Average(s => (s.EndDate!.Value - s.StartDate).TotalHours);
        }

        // Monthly Stats (Mocking past 4 months for trend charts)
        var monthlyStats = new List<MonthlyStatDto>
        {
            new MonthlyStatDto("May", 14, 10, 4),
            new MonthlyStatDto("June", 25, 20, 3),
            new MonthlyStatDto("July", 35, 28, 5),
            new MonthlyStatDto("August", total, approved, rejected)
        };

        // Workflow breakdowns
        var dbInstances = await _context.WorkflowInstances
            .Include(i => i.WorkflowVersion)
                .ThenInclude(v => v.WorkflowDefinition)
            .Include(i => i.Steps)
            .Where(i => !i.IsDeleted)
            .ToListAsync();

        var wfBreakdown = dbInstances
            .GroupBy(i => i.WorkflowVersion.WorkflowDefinition.Name)
            .Select(g => new WorkflowStatDto(
                g.Key,
                g.Count(),
                g.SelectMany(i => i.Steps)
                 .Where(s => s.Status == StepStatus.Approved && s.EndDate.HasValue)
                 .Select(s => (s.EndDate!.Value - s.StartDate).TotalHours)
                 .DefaultIfEmpty(0)
                 .Average()
            ))
            .ToList();

        // Department breakdowns
        var dbInstancesForDept = await _context.WorkflowInstances
            .Include(i => i.Requester)
                .ThenInclude(r => r.Department)
            .Where(i => !i.IsDeleted)
            .ToListAsync();

        var deptBreakdown = dbInstancesForDept
            .GroupBy(i => i.Requester.Department != null ? i.Requester.Department.Name : "IT")
            .Select(g => new DepartmentStatDto(g.Key, g.Count()))
            .ToList();

        // Top Delay Users
        var delayUsers = new List<TopDelayUserDto>
        {
            new TopDelayUserDto("John Doe", "IT", 4, 36.5),
            new TopDelayUserDto("Sarah Jenkins", "HR", 2, 28.0)
        };

        return Ok(new DashboardMetricsDto(
            total,
            pending,
            approved,
            rejected,
            Math.Round(avgHours, 2),
            slaViolations,
            monthlyStats,
            wfBreakdown,
            deptBreakdown,
            delayUsers
        ));
    }
}
