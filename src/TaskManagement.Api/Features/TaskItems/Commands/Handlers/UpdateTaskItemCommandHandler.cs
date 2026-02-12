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
            var isProjectMember = taskItem.Project.Members.Any(m => m.UserId == currentUserId);
            bool isProjectOwner = taskItem.Project.OwnerUserId == currentUserId;
            bool isAssignee = taskItem.AssignedUserId == currentUserId;

            if (!isAdmin && !isProjectOwner && !isAssignee && !isProjectMember)
            {
                throw new ForbiddenAccessException("User is not authorized to update this task item.");
            }

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
                    TaskTitle = taskItem.Title
                });
            }

            if (!string.Equals(previousAssignedUserId, taskItem.AssignedUserId, StringComparison.Ordinal))
            {
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.TaskAssigneeChanged,
                    ProjectId = taskItem.ProjectId,
                    TaskItemId = taskItem.Id,
                    ProjectName = taskItem.Project.Name,
                    TaskTitle = taskItem.Title
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
                    TaskTitle = taskItem.Title
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

            return _mapper.Map<TaskItemDto>(taskItem);
        }

        private static string? NormalizeAssignedUserId(string? assignedUserId)
        {
            if (string.IsNullOrWhiteSpace(assignedUserId))
            {
                return null;
            }

            return assignedUserId.Trim();
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
    }
}
