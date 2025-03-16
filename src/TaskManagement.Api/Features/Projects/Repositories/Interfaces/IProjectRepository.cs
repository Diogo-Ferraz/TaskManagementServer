using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Infrastructure.Repositories.Interfaces;

namespace TaskManagement.Api.Features.Projects.Repositories.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IReadOnlyList<Project>> GetProjectsByUserIdAsync(string userId);
        Task<bool> ProjectExistsAsync(Guid id);
    }
}
