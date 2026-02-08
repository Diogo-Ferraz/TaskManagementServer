using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Features.Projects.Services
{
    public class ProjectMembershipService : IProjectMembershipService
    {
        private readonly TaskManagementDbContext _dbContext;

        public ProjectMembershipService(TaskManagementDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IReadOnlyList<Guid>> GetProjectIdsForUserAsync(string userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Projects
                .AsNoTracking()
                .Where(project => project.OwnerUserId == userId || project.Members.Any(member => member.UserId == userId))
                .Select(project => project.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task<bool> IsMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken)
        {
            return await _dbContext.Projects
                .AsNoTracking()
                .AnyAsync(project => project.Id == projectId
                    && (project.OwnerUserId == userId || project.Members.Any(member => member.UserId == userId)),
                    cancellationToken);
        }
    }
}
