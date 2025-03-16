using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Infrastructure.Repositories.Interfaces;

namespace TaskManagement.Api.Features.TaskItems.Repositories.Interfaces
{
    public interface ITaskItemRepository : IRepository<TaskItem>
    {
        Task<IReadOnlyList<TaskItem>> GetTasksByUserIdAsync(string userId);
        Task<IReadOnlyList<TaskItem>> GetTasksByProjectIdAsync(Guid projectId);
    }
}
