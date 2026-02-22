using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Projects.Commands.Handlers
{
    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IActivityPublisher _activityPublisher;
        private readonly ICurrentUserService _currentUserService;

        public DeleteProjectCommandHandler(
            TaskManagementDbContext dbContext,
            IActivityPublisher activityPublisher,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _activityPublisher = activityPublisher;
            _currentUserService = currentUserService;
        }

        public async Task Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId)) throw new UnauthorizedAccessException();

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (project == null)
            {
                throw new NotFoundException(nameof(Project), request.Id);
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var isProjectManager = _currentUserService.IsInRole(Roles.ProjectManager);

            var canDeleteAsProjectManagerOwner =
                isProjectManager && string.Equals(project.OwnerUserId, currentUserId, StringComparison.Ordinal);

            if (!isAdmin && !canDeleteAsProjectManagerOwner)
            {
                throw new ForbiddenAccessException("User is not authorized to delete this project.");
            }

            var activityLog = new ActivityLog
            {
                Type = ActivityType.ProjectDeleted,
                ProjectId = project.Id,
                ProjectName = project.Name
            };
            _dbContext.ActivityLogs.Add(activityLog);
            _dbContext.Projects.Remove(project);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _activityPublisher.PublishAsync(activityLog, cancellationToken);
        }
    }
}
