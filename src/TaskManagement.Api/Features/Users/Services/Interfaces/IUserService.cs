using TaskManagement.Api.Features.Users.Models;

namespace TaskManagement.Api.Features.Users.Services.Interfaces
{
    public interface IUserService
    {
        Task<User?> GetUserByIdAsync(string id);
        Task<bool> IsInRoleAsync(string userId, string role);
    }
}
