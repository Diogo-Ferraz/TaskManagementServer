using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Repositories;

namespace TaskManagement.Api.Features.TaskItems.Repositories
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
