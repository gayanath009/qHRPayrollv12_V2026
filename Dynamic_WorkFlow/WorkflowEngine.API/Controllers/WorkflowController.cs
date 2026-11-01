using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Domain.Entities;
using WorkflowEngine.Domain.Enums;
using WorkflowEngine.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;

namespace WorkflowEngine.API.Controllers;

[Authorize]
public class WorkflowController : ApiControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;

    public WorkflowController(WorkflowDbContext context, IMapper mapper, IConfiguration configuration)
    {
        _context = context;
        _mapper = mapper;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<WorkflowDefinitionDto>>> GetWorkflows()
    {
        var workflows = await _context.WorkflowDefinitions
            .Where(w => !w.IsDeleted)
            .ToListAsync();
        return Ok(_mapper.Map<IEnumerable<WorkflowDefinitionDto>>(workflows));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<WorkflowDefinitionDto>> GetWorkflow(Guid id)
    {
        var workflow = await _context.WorkflowDefinitions.FindAsync(id);
        if (workflow == null) return NotFound();
        return Ok(_mapper.Map<WorkflowDefinitionDto>(workflow));
    }

    [HttpGet("{id}/versions")]
    public async Task<ActionResult<IEnumerable<WorkflowVersionDto>>> GetVersions(Guid id)
    {
        var versions = await _context.WorkflowVersions
            .Where(v => v.WorkflowDefinitionId == id && !v.IsDeleted)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();
        return Ok(_mapper.Map<IEnumerable<WorkflowVersionDto>>(versions));
    }

    [HttpGet("version/{versionId}")]
    public async Task<ActionResult<WorkflowVersionDto>> GetVersionDetails(Guid versionId)
    {
        var version = await _context.WorkflowVersions
            .Include(v => v.WorkflowDefinition)
            .Include(v => v.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.Sequence))
                .ThenInclude(s => s.SkipConditionGroups.Where(cg => !cg.IsDeleted))
                    .ThenInclude(cg => cg.Conditions.Where(c => !c.IsDeleted))
            .Include(v => v.Steps.Where(s => !s.IsDeleted).OrderBy(s => s.Sequence))
                .ThenInclude(s => s.TransitionsFrom.Where(t => !t.IsDeleted))
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted);

        if (version == null) return NotFound();
        var dto = _mapper.Map<WorkflowVersionDto>(version);
        if (version.WorkflowDefinition != null)
        {
            dto = dto with
            {
                WorkflowName = version.WorkflowDefinition.Name,
                WorkflowCode = version.WorkflowDefinition.Code,
                AssociatedLeaveTypes = version.WorkflowDefinition.AssociatedLeaveTypes,
                TargetWorkType = version.WorkflowDefinition.TargetWorkType
            };
        }
        return Ok(dto);
    }

    [HttpGet("metadata/leave-and-work-types")]
    public async Task<IActionResult> GetLeaveAndWorkTypes()
    {
        var leaveTypes = new List<object>();
        var workTypes = new List<object>();

        try
        {
            var qhrConStr = _configuration?.GetConnectionString("qHRconString")
                            ?? "Server=localhost;Database=qHRMasterV26;Trusted_Connection=True;TrustServerCertificate=True;";

            using var conn = new SqlConnection(qhrConStr);
            await conn.OpenAsync();

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT LvTyp_Id, LvTyp_Code, Name_Eng, [Desc] FROM dbo.HR_LvType WHERE Rec_Stat = 1 ORDER BY LvTyp_Id";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    leaveTypes.Add(new
                    {
                        Id = Convert.ToInt32(reader["LvTyp_Id"]),
                        Code = reader["LvTyp_Code"]?.ToString() ?? "",
                        Name = reader["Name_Eng"]?.ToString() ?? "",
                        Description = reader["Desc"]?.ToString() ?? ""
                    });
                }
            }

            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT Wrktyp_Id, Wrktyp_Code, Name_Eng, [Desc] FROM dbo.ORG_WrkType WHERE Rec_Stat = 1 ORDER BY Wrktyp_Id";
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    workTypes.Add(new
                    {
                        Id = Convert.ToInt32(reader["Wrktyp_Id"]),
                        Code = reader["Wrktyp_Code"]?.ToString() ?? "",
                        Name = reader["Name_Eng"]?.ToString() ?? "",
                        Description = reader["Desc"]?.ToString() ?? ""
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Fallback hardcoded if connection unavailable
            leaveTypes = new List<object>
            {
                new { Id = 1, Code = "LV_ANL", Name = "Annual Leave", Description = "Annual Leave" },
                new { Id = 3, Code = "LV_SICK", Name = "Sick Leave", Description = "Sick Leave" },
                new { Id = 4, Code = "LV_CSL", Name = "Casual Leave", Description = "Casual Leave" },
                new { Id = 12, Code = "LV_MTN", Name = "Maternity Leave", Description = "Maternity Leave" },
                new { Id = 13, Code = "LV_HAJ", Name = "HAJJI Leave", Description = "HAJJI Leave" },
                new { Id = 7, Code = "LV_EMG_PD", Name = "Emergency-Paid", Description = "Emergency Leave - Paid" },
                new { Id = 14, Code = "LV_EMG_UPD", Name = "Emergency-Unpaid", Description = "Emergency Leave - Unpaid" }
            };

            workTypes = new List<object>
            {
                new { Id = 1, Code = "IN_HOUSE", Name = "In-House", Description = "In-House Staff (Head Office)" },
                new { Id = 2, Code = "OUTSRC", Name = "Outsourced", Description = "Outsourced Staff (Sites/Clients)" }
            };
        }

        return Ok(new
        {
            LeaveTypes = leaveTypes,
            WorkTypes = workTypes
        });
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowDefinitionDto>> CreateWorkflow([FromBody] CreateWorkflowDto dto)
    {
        var exists = await _context.WorkflowDefinitions.AnyAsync(w => w.Code == dto.Code && !w.IsDeleted);
        if (exists) return BadRequest("Workflow code already exists.");

        var workflow = new WorkflowDefinition
        {
            Code = dto.Code,
            Name = dto.Name,
            Description = dto.Description,
            Module = dto.Module,
            Category = dto.Category,
            AssociatedLeaveTypes = dto.AssociatedLeaveTypes,
            TargetWorkType = !string.IsNullOrWhiteSpace(dto.TargetWorkType) ? dto.TargetWorkType : "ALL"
        };

        await _context.WorkflowDefinitions.AddAsync(workflow);
        await _context.SaveChangesAsync();

        // Create default draft Version 1
        var version = new WorkflowVersion
        {
            WorkflowDefinitionId = workflow.Id,
            VersionNumber = 1,
            Status = WorkflowStatus.Draft
        };
        await _context.WorkflowVersions.AddAsync(version);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWorkflow), new { id = workflow.Id }, _mapper.Map<WorkflowDefinitionDto>(workflow));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateWorkflow(Guid id, [FromBody] UpdateWorkflowDto dto)
    {
        var workflow = await _context.WorkflowDefinitions.FindAsync(id);
        if (workflow == null) return NotFound();

        workflow.Name = dto.Name;
        workflow.Description = dto.Description;
        workflow.Module = dto.Module;
        workflow.Category = dto.Category;
        workflow.AssociatedLeaveTypes = dto.AssociatedLeaveTypes;
        if (!string.IsNullOrWhiteSpace(dto.TargetWorkType))
        {
            workflow.TargetWorkType = dto.TargetWorkType;
        }

        _context.WorkflowDefinitions.Update(workflow);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("version/{versionId}/publish")]
    public async Task<IActionResult> PublishVersion(Guid versionId)
    {
        var version = await _context.WorkflowVersions.FindAsync(versionId);
        if (version == null) return NotFound();

        // Check if there's already a published version, if so deactive it
        var activeVersions = await _context.WorkflowVersions
            .Where(v => v.WorkflowDefinitionId == version.WorkflowDefinitionId && v.Status == WorkflowStatus.Published)
            .ToListAsync();

        foreach (var v in activeVersions)
        {
            v.Status = WorkflowStatus.Deactivated;
            _context.WorkflowVersions.Update(v);
        }

        version.Status = WorkflowStatus.Published;
        version.EffectiveDate = DateTime.UtcNow;
        _context.WorkflowVersions.Update(version);

        await _context.SaveChangesAsync();
        return Ok(new { Message = "Workflow version published successfully." });
    }

    [HttpPost("version/{versionId}/clone")]
    public async Task<IActionResult> CloneVersion(Guid versionId)
    {
        var sourceVersion = await _context.WorkflowVersions
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(v => v.Id == versionId);

        if (sourceVersion == null) return NotFound();

        var maxVersionNum = await _context.WorkflowVersions
            .Where(v => v.WorkflowDefinitionId == sourceVersion.WorkflowDefinitionId)
            .MaxAsync(v => (int?)v.VersionNumber) ?? 0;

        // Create new Version
        var newVersion = new WorkflowVersion
        {
            WorkflowDefinitionId = sourceVersion.WorkflowDefinitionId,
            VersionNumber = maxVersionNum + 1,
            Status = WorkflowStatus.Draft
        };
        await _context.WorkflowVersions.AddAsync(newVersion);
        await _context.SaveChangesAsync();

        // Duplicate steps
        var steps = await _context.WorkflowSteps
            .Where(s => s.WorkflowVersionId == sourceVersion.Id && !s.IsDeleted)
            .ToListAsync();

        var stepMap = new Dictionary<Guid, Guid>(); // Map old step IDs to new step IDs for transitions

        foreach (var s in steps)
        {
            var newStep = new WorkflowStep
            {
                WorkflowVersionId = newVersion.Id,
                StepNumber = s.StepNumber,
                Name = s.Name,
                ApprovalType = s.ApprovalType,
                Sequence = s.Sequence,
                IsMandatory = s.IsMandatory,
                ParallelApproval = s.ParallelApproval,
                SLAHours = s.SLAHours,
                EmailNotification = s.EmailNotification,
                SMSNotification = s.SMSNotification,
                PushNotification = s.PushNotification,
                AutoApproval = s.AutoApproval,
                RejectAllowed = s.RejectAllowed,
                ReturnAllowed = s.ReturnAllowed,
                DelegateAllowed = s.DelegateAllowed,
                CommentsMandatory = s.CommentsMandatory,
                AttachmentMandatory = s.AttachmentMandatory,
                DigitalSignatureRequired = s.DigitalSignatureRequired,
                TargetRoleId = s.TargetRoleId,
                TargetUserId = s.TargetUserId,
                TargetDepartmentId = s.TargetDepartmentId,
                ApiLookupUrl = s.ApiLookupUrl
            };
            await _context.WorkflowSteps.AddAsync(newStep);
            await _context.SaveChangesAsync();
            stepMap[s.Id] = newStep.Id;
        }

        // Duplicate transitions
        var transitions = await _context.WorkflowTransitions
            .Where(t => t.WorkflowVersionId == sourceVersion.Id && !t.IsDeleted)
            .ToListAsync();

        foreach (var t in transitions)
        {
            if (stepMap.TryGetValue(t.FromStepId, out Guid newFrom) && stepMap.TryGetValue(t.ToStepId, out Guid newTo))
            {
                var newTrans = new WorkflowTransition
                {
                    WorkflowVersionId = newVersion.Id,
                    FromStepId = newFrom,
                    ToStepId = newTo,
                    Description = t.Description
                };
                await _context.WorkflowTransitions.AddAsync(newTrans);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { Message = $"Workflow version cloned to Version {newVersion.VersionNumber}.", NewVersionId = newVersion.Id });
    }

    [HttpPut("version/{versionId}/steps")]
    public async Task<IActionResult> UpdateVersionSteps(Guid versionId, [FromBody] List<UpdateStepDto> stepDtos)
    {
        var version = await _context.WorkflowVersions
            .Include(v => v.Steps)
            .FirstOrDefaultAsync(v => v.Id == versionId && !v.IsDeleted);

        if (version == null) return NotFound(new { message = "Workflow version not found." });

        var incomingIds = stepDtos
            .Where(s => s.Id.HasValue && s.Id.Value != Guid.Empty)
            .Select(s => s.Id.Value)
            .ToList();

        // Soft-delete steps that are no longer present in incoming list
        var toRemove = version.Steps.Where(s => !incomingIds.Contains(s.Id) && !s.IsDeleted).ToList();
        foreach (var r in toRemove)
        {
            r.IsDeleted = true;
            _context.WorkflowSteps.Update(r);
        }

        foreach (var dto in stepDtos)
        {
            var existing = (dto.Id.HasValue && dto.Id.Value != Guid.Empty)
                ? version.Steps.FirstOrDefault(s => s.Id == dto.Id.Value)
                : null;

            if (existing != null)
            {
                existing.Name = dto.Name;
                existing.StepNumber = dto.StepNumber;
                existing.ApprovalType = (ApprovalType)dto.ApprovalType;
                existing.Sequence = dto.Sequence;
                existing.IsMandatory = dto.IsMandatory;
                existing.ParallelApproval = dto.ParallelApproval;
                existing.SLAHours = dto.SLAHours;
                existing.EmailNotification = dto.EmailNotification;
                existing.SMSNotification = dto.SMSNotification;
                existing.CommentsMandatory = dto.CommentsMandatory;
                existing.AttachmentMandatory = dto.AttachmentMandatory;
                existing.TargetWorkType = string.IsNullOrWhiteSpace(dto.TargetWorkType) ? "ALL" : dto.TargetWorkType;
                existing.TargetUserId = string.IsNullOrWhiteSpace(dto.TargetUserId) ? null : dto.TargetUserId;
                existing.TargetRoleId = string.IsNullOrWhiteSpace(dto.TargetRoleId) ? null : dto.TargetRoleId;
                existing.IsDeleted = false;
                _context.WorkflowSteps.Update(existing);
            }
            else
            {
                var newStep = new WorkflowStep
                {
                    WorkflowVersionId = versionId,
                    Name = dto.Name,
                    StepNumber = dto.StepNumber,
                    ApprovalType = (ApprovalType)dto.ApprovalType,
                    Sequence = dto.Sequence,
                    IsMandatory = dto.IsMandatory,
                    ParallelApproval = dto.ParallelApproval,
                    SLAHours = dto.SLAHours,
                    EmailNotification = dto.EmailNotification,
                    SMSNotification = dto.SMSNotification,
                    CommentsMandatory = dto.CommentsMandatory,
                    AttachmentMandatory = dto.AttachmentMandatory,
                    TargetWorkType = string.IsNullOrWhiteSpace(dto.TargetWorkType) ? "ALL" : dto.TargetWorkType,
                    TargetUserId = string.IsNullOrWhiteSpace(dto.TargetUserId) ? null : dto.TargetUserId,
                    TargetRoleId = string.IsNullOrWhiteSpace(dto.TargetRoleId) ? null : dto.TargetRoleId
                };
                await _context.WorkflowSteps.AddAsync(newStep);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Workflow steps and approver assignments updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWorkflow(Guid id)
    {
        var workflow = await _context.WorkflowDefinitions.FindAsync(id);
        if (workflow == null) return NotFound(new { message = "Workflow not found." });

        workflow.IsDeleted = true;
        workflow.IsActive = false;
        _context.WorkflowDefinitions.Update(workflow);
        await _context.SaveChangesAsync();
        return Ok(new { message = "Workflow deleted successfully." });
    }
}

