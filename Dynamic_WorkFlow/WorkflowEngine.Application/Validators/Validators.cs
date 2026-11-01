using FluentValidation;
using WorkflowEngine.Application.DTOs;

namespace WorkflowEngine.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}

public class CreateWorkflowDtoValidator : AbstractValidator<CreateWorkflowDto>
{
    public CreateWorkflowDtoValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Workflow name is required.")
            .MaximumLength(150).WithMessage("Workflow name must not exceed 150 characters.");
        RuleFor(x => x.Code).NotEmpty().WithMessage("Workflow code is required.")
            .MaximumLength(50).WithMessage("Workflow code must not exceed 50 characters.");
    }
}

public class StartWorkflowRequestValidator : AbstractValidator<StartWorkflowRequest>
{
    public StartWorkflowRequestValidator()
    {
        RuleFor(x => x.WorkflowCode).NotEmpty().WithMessage("Workflow Code is required.");
        RuleFor(x => x.InputData).NotNull().WithMessage("Input data payload is required.");
    }
}

public class ApproveRequestValidator : AbstractValidator<ApproveRequest>
{
    public ApproveRequestValidator()
    {
        RuleFor(x => x.ApprovalId).NotEmpty().WithMessage("Approval ID is required.");
    }
}
