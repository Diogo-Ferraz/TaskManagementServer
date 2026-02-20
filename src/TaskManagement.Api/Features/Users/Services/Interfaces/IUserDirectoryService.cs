using TaskManagement.Api.Features.Users.Services.Models;

namespace TaskManagement.Api.Features.Users.Services.Interfaces
{
    public interface IUserDirectoryService
    {
        Task<bool> UserExistsAsync(string userId, CancellationToken cancellationToken);
        Task<string?> GetDisplayNameAsync(string userId, CancellationToken cancellationToken);
        Task<UserDirectorySummary?> GetUserSummaryAsync(string userId, CancellationToken cancellationToken);
    }
}
