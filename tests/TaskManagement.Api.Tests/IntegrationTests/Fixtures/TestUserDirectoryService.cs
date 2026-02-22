using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Models;

namespace TaskManagement.Api.Tests.IntegrationTests.Fixtures
{
    public class TestUserDirectoryService : IUserDirectoryService
    {
        public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(userId));
        }

        public Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult<string?>(null);
            }

            return Task.FromResult<string?>($"Test User {userId}");
        }

        public Task<UserDirectorySummary?> GetUserSummaryAsync(string userId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Task.FromResult<UserDirectorySummary?>(null);
            }

            return Task.FromResult<UserDirectorySummary?>(new UserDirectorySummary
            {
                DisplayName = $"Test User {userId}",
                Email = $"{userId}@example.test",
                Roles = ResolveRoles(userId)
            });
        }

        private static IReadOnlyCollection<string> ResolveRoles(string userId)
        {
            if (userId.Contains("pm", StringComparison.OrdinalIgnoreCase)
                || userId.Contains("project-manager", StringComparison.OrdinalIgnoreCase))
            {
                return ["ProjectManager"];
            }

            if (userId.Contains("admin", StringComparison.OrdinalIgnoreCase))
            {
                return ["Administrator"];
            }

            return ["User"];
        }
    }
}
