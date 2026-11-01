namespace WorkflowEngine.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Username { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
}
