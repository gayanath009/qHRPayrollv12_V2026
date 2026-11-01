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
using WorkflowEngine.Infrastructure.Persistence;

namespace WorkflowEngine.API.Controllers;

[Authorize]
public class FormController : ApiControllerBase
{
    private readonly WorkflowDbContext _context;
    private readonly IMapper _mapper;

    public FormController(WorkflowDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    [HttpGet("version/{versionId}")]
    public async Task<ActionResult<WorkflowFormDto>> GetFormByVersion(Guid versionId)
    {
        var form = await _context.WorkflowForms
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
            .Include(f => f.Rules)
                .ThenInclude(r => r.Conditions)
            .Include(f => f.Rules)
                .ThenInclude(r => r.Actions)
            .FirstOrDefaultAsync(f => f.WorkflowVersionId == versionId && !f.IsDeleted);

        if (form == null) return NotFound();
        return Ok(_mapper.Map<WorkflowFormDto>(form));
    }

    [HttpPost]
    public async Task<ActionResult<WorkflowFormDto>> SaveForm([FromBody] CreateFormDto dto)
    {
        // Check if form already exists for this version
        var existingForm = await _context.WorkflowForms
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.WorkflowVersionId == dto.WorkflowVersionId && !f.IsDeleted);

        if (existingForm != null)
        {
            // Update instead of creating
            existingForm.Name = dto.Name;
            existingForm.Description = dto.Description;
            _context.WorkflowForms.Update(existingForm);

            // Remove existing fields and option sets to rebuild
            var existingFields = await _context.WorkflowFields.Where(f => f.FormId == existingForm.Id).ToListAsync();
            _context.WorkflowFields.RemoveRange(existingFields);
            await _context.SaveChangesAsync();
        }

        var form = existingForm ?? new WorkflowForm
        {
            WorkflowVersionId = dto.WorkflowVersionId,
            Name = dto.Name,
            Description = dto.Description
        };

        if (existingForm == null)
        {
            await _context.WorkflowForms.AddAsync(form);
            await _context.SaveChangesAsync();
        }

        // Insert new fields
        foreach (var fieldDto in dto.Fields)
        {
            var field = new WorkflowField
            {
                FormId = form.Id,
                Name = fieldDto.Name,
                Label = fieldDto.Label,
                Type = fieldDto.Type,
                Placeholder = fieldDto.Placeholder,
                DefaultValue = fieldDto.DefaultValue,
                ValidationRegex = fieldDto.ValidationRegex,
                IsRequired = fieldDto.IsRequired,
                IsReadOnly = fieldDto.IsReadOnly,
                Order = fieldDto.Order,
                GridColumns = fieldDto.GridColumns
            };

            await _context.WorkflowFields.AddAsync(field);
            await _context.SaveChangesAsync();

            if (fieldDto.Options != null && fieldDto.Options.Any())
            {
                foreach (var opt in fieldDto.Options)
                {
                    await _context.WorkflowFieldOptions.AddAsync(new WorkflowFieldOption
                    {
                        FieldId = field.Id,
                        Label = opt.Label,
                        Value = opt.Value
                    });
                }
            }
        }

        await _context.SaveChangesAsync();

        var reloadedForm = await _context.WorkflowForms
            .Include(f => f.Fields)
                .ThenInclude(f => f.Options)
            .Include(f => f.Rules)
            .FirstOrDefaultAsync(f => f.Id == form.Id);

        return Ok(_mapper.Map<WorkflowFormDto>(reloadedForm));
    }
}
