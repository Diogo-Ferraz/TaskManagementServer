using AutoMapper;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.TaskItems.Commands.Handlers
{
    public class CreateTaskItemCommandHandler : IRequestHandler<CreateTaskItemCommand, TaskItemDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IActivityPublisher _activityPublisher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IUserDirectoryService _userDirectoryService;

        public CreateTaskItemCommandHandler(
            TaskManagementDbContext dbContext,
            IActivityPublisher activityPublisher,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IUserDirectoryService userDirectoryService)
        {
            _dbContext = dbContext;
            _activityPublisher = activityPublisher;
            _currentUserService = currentUserService;
            _mapper = mapper;
            _userDirectoryService = userDirectoryService;
        }

        public async Task<TaskItemDto> Handle(CreateTaskItemCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var project = await _dbContext.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken);

            if (project is null)
            {
                throw new NotFoundException($"Project with ID {request.ProjectId} not found.");
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var isAuthorized = isAdmin
                               || project.OwnerUserId == currentUserId
                               || project.Members.Any(m => m.UserId == currentUserId);

            if (!isAuthorized)
            {
                throw new ForbiddenAccessException("User is not authorized to add tasks to this project.");
            }

            var taskItem = _mapper.Map<TaskItem>(request);
            var assignedUserId = NormalizeAssignedUserId(request.AssignedUserId);
            if (assignedUserId != null)
            {
                var userExists = await _userDirectoryService.UserExistsAsync(assignedUserId, cancellationToken);
                if (!userExists)
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(nameof(request.AssignedUserId),
                            "AssignedUserId must reference an existing user.")
                    });
                }

                await EnsureAssignableUserRoleAsync(assignedUserId, nameof(request.AssignedUserId), cancellationToken);
                EnsureProjectMember(_dbContext, project, assignedUserId, currentUserId);
            }

            taskItem.AssignedUserId = assignedUserId;

            _dbContext.TaskItems.Add(taskItem);
            var activityLog = new ActivityLog
            {
                Type = ActivityType.TaskCreated,
                ProjectId = taskItem.ProjectId,
                TaskItemId = taskItem.Id,
                ProjectName = project.Name,
                TaskTitle = taskItem.Title
            };
            _dbContext.ActivityLogs.Add(activityLog);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await _activityPublisher.PublishAsync(activityLog, cancellationToken);

            var result = _mapper.Map<TaskItemDto>(taskItem);
            result.AssignedUserName = await ResolveAssigneeDisplayNameAsync(taskItem.AssignedUserId, cancellationToken);
            return result;
        }

        private static string? NormalizeAssignedUserId(string? assignedUserId)
        {
            if (string.IsNullOrWhiteSpace(assignedUserId))
            {
                return null;
            }

            return assignedUserId.Trim();
        }

        private async Task EnsureAssignableUserRoleAsync(string assignedUserId, string propertyName, CancellationToken cancellationToken)
        {
            var userSummary = await _userDirectoryService.GetUserSummaryAsync(assignedUserId, cancellationToken);
            var roles = userSummary?.Roles ?? Array.Empty<string>();

            if (roles.Contains(Roles.ProjectManager))
            {
                throw new ValidationException(new[]
                {
                    new ValidationFailure(propertyName, "Assigned user cannot have ProjectManager role.")
                });
            }
        }

        private static void EnsureProjectMember(
            TaskManagementDbContext dbContext,
            Projects.Models.Project project,
            string assignedUserId,
            string addedByUserId)
        {
            if (project.OwnerUserId == assignedUserId || project.Members.Any(m => m.UserId == assignedUserId))
            {
                return;
            }

            var member = new ProjectMember
            {
                ProjectId = project.Id,
                UserId = assignedUserId,
                JoinedAt = DateTime.UtcNow,
                AddedByUserId = addedByUserId
            };
            project.Members.Add(member);
            dbContext.ProjectMembers.Add(member);
        }

        private async Task<string> ResolveAssigneeDisplayNameAsync(string? assignedUserId, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(assignedUserId))
            {
                return "Unassigned";
            }

            var displayName = await _userDirectoryService.GetDisplayNameAsync(assignedUserId, cancellationToken);
            return string.IsNullOrWhiteSpace(displayName) ? assignedUserId : displayName;
        }
    }
}
