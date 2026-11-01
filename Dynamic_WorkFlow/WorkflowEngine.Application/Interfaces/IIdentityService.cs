using System.Threading.Tasks;
using WorkflowEngine.Domain.Entities;

namespace WorkflowEngine.Application.Interfaces;

public interface IIdentityService
{
    Task<(bool Succeeded, string Token, string RefreshToken, User? User)> LoginAsync(string username, string password);
    Task<(bool Succeeded, string Token, string RefreshToken)> RefreshTokenAsync(string token, string refreshToken);
}
