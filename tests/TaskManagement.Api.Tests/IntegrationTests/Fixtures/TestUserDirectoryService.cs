using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Tests.IntegrationTests.Fixtures
{
    public class TestUserDirectoryService : IUserDirectoryService
    {
        public Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(userId));
        }
    }
}
