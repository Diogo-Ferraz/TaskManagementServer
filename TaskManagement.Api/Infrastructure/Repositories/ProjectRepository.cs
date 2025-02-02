using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Infrastructure.Repositories
{
    public class ProjectRepository : Repository<Project>, IProjectRepository
    {
        public ProjectRepository(TaskManagementDbContext context) : base(context)
        {
        }

        public async Task<IReadOnlyList<Project>> GetProjectsByUserIdAsync(string userId)
        {
            return await _context.Projects
                .Where(p => p.UserId == userId)
                .Include(p => p.TaskItems)
                .ToListAsync();
        }

        public async Task<bool> ProjectExistsAsync(Guid id)
        {
            return await _context.Projects.AnyAsync(p => p.Id == id);
        }
    }
}
