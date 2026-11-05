using AutoMapper;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(d => d.RoleName, opt => opt.MapFrom(s => s.Role != null ? s.Role.Name : null))
            .ForMember(d => d.DepartmentName, opt => opt.MapFrom(s => s.Department != null ? s.Department.Name : null))
            .ForMember(d => d.DesignationName, opt => opt.MapFrom(s => s.Designation != null ? s.Designation.Name : null));

        CreateMap<WorkflowDefinition, WorkflowDefinitionDto>();
        CreateMap<WorkflowVersion, WorkflowVersionDto>()
            .ForMember(d => d.WorkflowName, opt => opt.MapFrom(s => s.WorkflowDefinition != null ? s.WorkflowDefinition.Name : string.Empty))
            .ForMember(d => d.WorkflowCode, opt => opt.MapFrom(s => s.WorkflowDefinition != null ? s.WorkflowDefinition.Code : string.Empty))
            .ForMember(d => d.AssociatedLeaveTypes, opt => opt.MapFrom(s => s.WorkflowDefinition != null ? s.WorkflowDefinition.AssociatedLeaveTypes : null))
            .ForMember(d => d.TargetWorkType, opt => opt.MapFrom(s => s.WorkflowDefinition != null ? s.WorkflowDefinition.TargetWorkType : null))`r`n            .ForMember(d => d.WorkflowType, opt => opt.MapFrom(s => s.WorkflowDefinition != null ? s.WorkflowDefinition.WorkflowType : "Leave Approval"));
        
        CreateMap<WorkflowStep, WorkflowStepDto>()
            .ForMember(d => d.SkipConditionGroups, opt => opt.MapFrom(s => s.SkipConditionGroups))
            .ForMember(d => d.TransitionsFrom, opt => opt.MapFrom(s => s.TransitionsFrom));

        CreateMap<WorkflowConditionGroup, WorkflowConditionGroupDto>();
        CreateMap<WorkflowCondition, WorkflowConditionDto>();
        CreateMap<WorkflowTransition, WorkflowTransitionDto>();

        CreateMap<WorkflowForm, WorkflowFormDto>();
        CreateMap<WorkflowField, WorkflowFieldDto>();
        CreateMap<WorkflowFieldOption, WorkflowFieldOptionDto>();
        CreateMap<WorkflowRule, WorkflowRuleDto>();
        CreateMap<WorkflowRuleCondition, WorkflowRuleConditionDto>();
        CreateMap<WorkflowRuleAction, WorkflowRuleActionDto>();

        CreateMap<WorkflowInstance, WorkflowInstanceDto>()
            .ForMember(d => d.WorkflowCode, opt => opt.MapFrom(s => s.WorkflowVersion != null && s.WorkflowVersion.WorkflowDefinition != null ? s.WorkflowVersion.WorkflowDefinition.Code : string.Empty))
            .ForMember(d => d.WorkflowName, opt => opt.MapFrom(s => s.WorkflowVersion != null && s.WorkflowVersion.WorkflowDefinition != null ? s.WorkflowVersion.WorkflowDefinition.Name : string.Empty))
            .ForMember(d => d.RequesterName, opt => opt.MapFrom(s => s.Requester != null ? s.Requester.FullName : string.Empty));

        CreateMap<WorkflowInstanceData, WorkflowInstanceDataDto>();
        CreateMap<WorkflowInstanceStep, WorkflowInstanceStepDto>();
        
        CreateMap<WorkflowApproval, WorkflowApprovalDto>()
            .ForMember(d => d.ApproverName, opt => opt.MapFrom(s => s.Approver != null ? s.Approver.FullName : string.Empty))
            .ForMember(d => d.DelegatedFromUserName, opt => opt.MapFrom(s => s.DelegatedFromUser != null ? s.DelegatedFromUser.FullName : null));

        CreateMap<WorkflowHistory, WorkflowHistoryDto>()
            .ForMember(d => d.PerformedByName, opt => opt.MapFrom(s => s.PerformedBy != null ? s.PerformedBy.FullName : "System"));

        CreateMap<WorkflowComment, WorkflowCommentDto>()
            .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User != null ? s.User.FullName : string.Empty))
            .ForMember(d => d.StepName, opt => opt.MapFrom(s => s.WorkflowStep != null ? s.WorkflowStep.Name : "General"));

        CreateMap<WorkflowAttachment, WorkflowAttachmentDto>()
            .ForMember(d => d.UploadedByName, opt => opt.MapFrom(s => s.UploadedBy != null ? s.UploadedBy.FullName : string.Empty))
            .ForMember(d => d.StepName, opt => opt.MapFrom(s => s.WorkflowStep != null ? s.WorkflowStep.Name : "General"));
    }
}
