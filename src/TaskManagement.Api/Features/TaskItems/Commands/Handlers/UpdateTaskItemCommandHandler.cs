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
    public class UpdateTaskItemCommandHandler : IRequestHandler<UpdateTaskItemCommand, TaskItemDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IActivityPublisher _activityPublisher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IUserDirectoryService _userDirectoryService;

        public UpdateTaskItemCommandHandler(
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

        public async Task<TaskItemDto> Handle(UpdateTaskItemCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var taskItem = await _dbContext.TaskItems
                .Include(t => t.Project)
                .ThenInclude(p => p.Members)
                .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

            if (taskItem == null)
            {
                throw new NotFoundException(nameof(TaskItem), request.Id);
            }
            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var isProjectManager = _currentUserService.IsInRole(Roles.ProjectManager);
            var isProjectMember = taskItem.Project.Members.Any(m => m.UserId == currentUserId);
            bool isProjectOwner = taskItem.Project.OwnerUserId == currentUserId;
            bool isAssignee = taskItem.AssignedUserId == currentUserId;
            var canManageAsProjectManager = isProjectManager && (isProjectOwner || isProjectMember);
            var isSelfAssigningUnassignedTaskOnly = IsSelfAssigningUnassignedTaskOnly(
                request,
                taskItem,
                currentUserId,
                isAdmin,
                isProjectManager,
                isProjectMember);

            if (!isAdmin && !canManageAsProjectManager && !isProjectOwner && !isAssignee && !isSelfAssigningUnassignedTaskOnly)
            {
                throw new ForbiddenAccessException("User is not authorized to update this task item.");
            }

            var assignedUserId = NormalizeAssignedUserId(request.AssignedUserId);
            EnsureAssignmentChangeAllowedForCurrentUser(currentUserId, taskItem.AssignedUserId, assignedUserId, isAdmin, isProjectManager);
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
                EnsureProjectMember(_dbContext, taskItem.Project, assignedUserId, currentUserId);
            }

            var previousStatus = taskItem.Status;
            var previousTitle = taskItem.Title;
            var previousAssignedUserId = taskItem.AssignedUserId;
            var previousDueDate = taskItem.DueDate;

            _mapper.Map(request, taskItem);
            taskItem.AssignedUserId = assignedUserId;

            var activityLogs = new List<ActivityLog>();
            if (previousStatus != taskItem.Status)
            {
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.TaskStatusChanged,
                    ProjectId = taskItem.ProjectId,
                    TaskItemId = taskItem.Id,
                    ProjectName = taskItem.Project.Name,
                    TaskTitle = taskItem.Title,
                    OldStatus = previousStatus,
                    NewStatus = taskItem.Status
                });
            }

            if (!string.Equals(previousTitle, taskItem.Title, StringComparison.Ordinal))
            {
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.TaskRenamed,
                    ProjectId = taskItem.ProjectId,
                    TaskItemId = taskItem.Id,
                    ProjectName = taskItem.Project.Name,
                    TaskTitle = taskItem.Title,
                    OldValue = previousTitle,
                    NewValue = taskItem.Title
                });
            }

            if (!string.Equals(previousAssignedUserId, taskItem.AssignedUserId, StringComparison.Ordinal))
            {
                var oldAssigneeDisplayName = await ResolveAssigneeDisplayNameAsync(previousAssignedUserId, cancellationToken);
                var newAssigneeDisplayName = await ResolveAssigneeDisplayNameAsync(taskItem.AssignedUserId, cancellationToken);
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.TaskAssigneeChanged,
                    ProjectId = taskItem.ProjectId,
                    TaskItemId = taskItem.Id,
                    ProjectName = taskItem.Project.Name,
                    TaskTitle = taskItem.Title,
                    OldValue = oldAssigneeDisplayName,
                    NewValue = newAssigneeDisplayName
                });
            }

            if (previousDueDate != taskItem.DueDate)
            {
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.TaskDueDateChanged,
                    ProjectId = taskItem.ProjectId,
                    TaskItemId = taskItem.Id,
                    ProjectName = taskItem.Project.Name,
                    TaskTitle = taskItem.Title,
                    OldValue = previousDueDate?.ToString("O"),
                    NewValue = taskItem.DueDate?.ToString("O")
                });
            }

            if (activityLogs.Count > 0)
            {
                _dbContext.ActivityLogs.AddRange(activityLogs);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            foreach (var activityLog in activityLogs)
            {
                await _activityPublisher.PublishAsync(activityLog, cancellationToken);
            }

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

        private static bool IsSelfAssigningUnassignedTaskOnly(
            UpdateTaskItemCommand request,
            TaskItem taskItem,
            string currentUserId,
            bool isAdmin,
            bool isProjectManager,
            bool isProjectMember)
        {
            if (isAdmin || isProjectManager || !isProjectMember)
            {
                return false;
            }

            if (!string.IsNullOrWhiteSpace(taskItem.AssignedUserId))
            {
                return false;
            }

            var normalizedRequestedAssignee = NormalizeAssignedUserId(request.AssignedUserId);
            if (!string.Equals(normalizedRequestedAssignee, currentUserId, StringComparison.Ordinal))
            {
                return false;
            }

            return string.Equals(request.Title, taskItem.Title, StringComparison.Ordinal)
                   && string.Equals(request.Description, taskItem.Description, StringComparison.Ordinal)
                   && request.Status == taskItem.Status
                   && request.DueDate == taskItem.DueDate;
        }

        private static void EnsureAssignmentChangeAllowedForCurrentUser(
            string currentUserId,
            string? currentAssignedUserId,
            string? newAssignedUserId,
            bool isAdmin,
            bool isProjectManager)
        {
            if (isAdmin || isProjectManager)
            {
                return;
            }

            if (newAssignedUserId == null)
            {
                if (!string.Equals(currentAssignedUserId, currentUserId, StringComparison.Ordinal))
                {
                    throw new ForbiddenAccessException("Users can only unassign tasks currently assigned to themselves.");
                }

                return;
            }

            if (!string.Equals(newAssignedUserId, currentUserId, StringComparison.Ordinal))
            {
                throw new ForbiddenAccessException("Users can only assign tasks to themselves.");
            }
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
