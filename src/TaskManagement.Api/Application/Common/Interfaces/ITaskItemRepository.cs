using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Common.Interfaces
{
    public interface ITaskItemRepository : IRepository<TaskItem>
    {
        Task<IReadOnlyList<TaskItem>> GetTasksByUserIdAsync(string userId);
        Task<IReadOnlyList<TaskItem>> GetTasksByProjectIdAsync(Guid projectId);
    }
}
