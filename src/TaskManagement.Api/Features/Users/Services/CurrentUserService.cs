using System.IdentityModel.Tokens.Jwt;
using TaskManagement.Api.Features.Users.Services.Interfaces;

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
            .FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Sub)?.Value ?? string.Empty;
    }
}
