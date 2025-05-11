using System.Security.Claims;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskManagement.Api.Features.Users.Services
{
    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CurrentUserService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public string Id => _httpContextAccessor.HttpContext?.User?.Claims
            .FirstOrDefault(c => c.Type == Claims.Subject)?.Value ?? string.Empty;

        public string? UserName => _httpContextAccessor.HttpContext?.User?
                                .FindFirstValue(Claims.Name);

        public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

        public IEnumerable<string> Roles => _httpContextAccessor.HttpContext?.User?
                                            .FindAll(Claims.Role)
                                            .Select(c => c.Value) ?? Enumerable.Empty<string>();

        public bool IsInRole(string roleName) => _httpContextAccessor.HttpContext?.User?.IsInRole(roleName) ?? false;

        public Claim? FindClaim(string claimType) => _httpContextAccessor.HttpContext?.User?.FindFirst(claimType);
    }
}
