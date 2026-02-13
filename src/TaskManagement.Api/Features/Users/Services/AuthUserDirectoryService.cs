using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Features.Users.Services
{
    public class AuthUserDirectoryService : IUserDirectoryService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthUserDirectoryService(HttpClient httpClient, IHttpContextAccessor httpContextAccessor)
        {
            _httpClient = httpClient;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return false;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/{Uri.EscapeDataString(userId)}");
            var authorizationHeader = _httpContextAccessor.HttpContext?.Request?.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            response.EnsureSuccessStatusCode();
            return true;
        }

        public async Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/users/{Uri.EscapeDataString(userId)}");
            var authorizationHeader = _httpContextAccessor.HttpContext?.Request?.Headers.Authorization.ToString();
            if (!string.IsNullOrWhiteSpace(authorizationHeader))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authorizationHeader);
            }

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            var user = await response.Content.ReadFromJsonAsync<UserSummaryResponse>(cancellationToken: cancellationToken);
            var baseDisplayName = user?.DisplayName ?? user?.UserName ?? user?.Email;
            if (string.IsNullOrWhiteSpace(baseDisplayName))
            {
                return null;
            }

            return user?.IsActive == false
                ? $"{baseDisplayName} (Inactive)"
                : baseDisplayName;
        }

        private sealed class UserSummaryResponse
        {
            public string? DisplayName { get; set; }
            public string? UserName { get; set; }
            public string? Email { get; set; }
            public bool IsActive { get; set; }
        }
    }
}
