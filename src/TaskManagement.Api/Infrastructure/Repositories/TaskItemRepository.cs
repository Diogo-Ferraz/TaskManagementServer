using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Infrastructure.Repositories
{
    public class TaskItemRepository : Repository<TaskItem>, ITaskItemRepository
    {
        public TaskItemRepository(TaskManagementDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<TaskItem>> GetTasksByUserIdAsync(string userId)
        {
            return await _context.TaskItems
                .Where(t => t.AssignedUserId == userId)
                .Include(t => t.Project)
                .ToListAsync();
        }

        public async Task<IReadOnlyList<TaskItem>> GetTasksByProjectIdAsync(Guid projectId)
        {
            return await _context.TaskItems
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.AssignedUser)
                .ToListAsync();
        }
    }
}
