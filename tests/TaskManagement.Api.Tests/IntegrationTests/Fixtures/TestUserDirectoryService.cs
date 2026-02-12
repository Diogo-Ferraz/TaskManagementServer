using TaskManagement.Api.Features.Users.Services.Interfaces;

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
    }
}
