using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.TaskItems.Commands.Handlers
{
    public class DeleteTaskItemCommandHandler : IRequestHandler<DeleteTaskItemCommand>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public DeleteTaskItemCommandHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task Handle(DeleteTaskItemCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var taskItem = await _dbContext.TaskItems
                 .Include(t => t.Project)
                 .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (taskItem == null)
            {
                throw new NotFoundException(nameof(TaskItem), request.Id);
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var isProjectManager = _currentUserService.IsInRole(Roles.ProjectManager);
            var isProjectMember = await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == taskItem.ProjectId && pm.UserId == currentUserId, cancellationToken);
            bool isProjectOwner = taskItem.Project.OwnerUserId == currentUserId;
            var canDeleteAsProjectManager = isProjectManager && (isProjectOwner || isProjectMember);

            // Keep user deletes stricter: owner-only unless elevated role.
            if (!isAdmin && !canDeleteAsProjectManager && !isProjectOwner)
            {
                throw new ForbiddenAccessException("User is not authorized to delete this task item.");
            }

            _dbContext.TaskItems.Remove(taskItem);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
