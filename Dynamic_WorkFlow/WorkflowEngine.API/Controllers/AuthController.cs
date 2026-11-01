using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using WorkflowEngine.Application.DTOs;
using WorkflowEngine.Application.Interfaces;

namespace WorkflowEngine.API.Controllers;

public class AuthController : ApiControllerBase
{
    private readonly IIdentityService _identityService;

    public AuthController(IIdentityService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await _identityService.LoginAsync(request.Username, request.Password);
        if (!result.Succeeded || result.User == null)
            return Unauthorized(new { Message = "Invalid username or password" });

        var userDto = new UserDto(
            result.User.Id,
            result.User.EmpSysId,
            result.User.Username,
            result.User.Email,
            result.User.FullName,
            result.User.RoleId,
            result.User.Role?.Name,
            result.User.DepartmentId,
            result.User.Department?.Name,
            result.User.DesignationId,
            result.User.Designation?.Name
        );

        return Ok(new LoginResponse(result.Token, result.RefreshToken, userDto));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var result = await _identityService.RefreshTokenAsync(request.Token, request.RefreshToken);
        if (!result.Succeeded)
            return BadRequest(new { Message = "Invalid refresh token request" });

        return Ok(new { Token = result.Token, RefreshToken = result.RefreshToken });
    }
}
