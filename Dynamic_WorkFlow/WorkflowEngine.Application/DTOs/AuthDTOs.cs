using System;

namespace WorkflowEngine.Application.DTOs;

public record LoginRequest(string Username, string Password);

public record LoginResponse(string Token, string RefreshToken, UserDto User);

public record RefreshRequest(string Token, string RefreshToken);

public record UserDto(
    Guid Id,
    int? EmpSysId,
    string Username,
    string Email,
    string FullName,
    Guid? RoleId,
    string? RoleName,
    Guid? DepartmentId,
    string? DepartmentName,
    Guid? DesignationId,
    string? DesignationName
);

public record CreateUserRequest(string Username, string Email, string? Password, string FullName, int? EmpSysId);


