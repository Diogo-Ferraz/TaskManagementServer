using System.Security.Claims;

namespace TaskManagement.Api.Features.Users.Services.Interfaces
{
    public interface ICurrentUserService
    {
        string? Id { get; }
        string? UserName { get; }
        bool IsAuthenticated { get; }
        IEnumerable<string> Roles { get; }
        bool IsInRole(string roleName);
        Claim? FindClaim(string claimType);
    }
}
