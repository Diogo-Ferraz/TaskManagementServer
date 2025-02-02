using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Common.Interfaces
{
    public interface IProjectRepository : IRepository<Project>
    {
        Task<IReadOnlyList<Project>> GetProjectsByUserIdAsync(string userId);
        Task<bool> ProjectExistsAsync(Guid id);
    }
}
