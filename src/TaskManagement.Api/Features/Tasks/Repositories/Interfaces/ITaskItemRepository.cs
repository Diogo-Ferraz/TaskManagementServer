using TaskManagement.Api.Features.Tasks.Models;
using TaskManagement.Api.Infrastructure.Repositories.Interfaces;

namespace TaskManagement.Api.Features.Tasks.Repositories.Interfaces
{
    public interface ITaskItemRepository : IRepository<TaskItem>
    {
        Task<IReadOnlyList<TaskItem>> GetTasksByUserIdAsync(string userId);
        Task<IReadOnlyList<TaskItem>> GetTasksByProjectIdAsync(Guid projectId);
    }
}
