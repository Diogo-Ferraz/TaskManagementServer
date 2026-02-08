using AutoMapper;
using FluentValidation.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Features.TaskItems.Commands.Handlers
{
    public class CreateTaskItemCommandHandler : IRequestHandler<CreateTaskItemCommand, TaskItemDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;
        private readonly IUserDirectoryService _userDirectoryService;

        public CreateTaskItemCommandHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper,
            IUserDirectoryService userDirectoryService)
        {
            _dbContext = dbContext;
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

            var isAuthorized = project.OwnerUserId == currentUserId
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

                EnsureProjectMember(_dbContext, project, assignedUserId);
            }

            taskItem.AssignedUserId = assignedUserId;

            _dbContext.TaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync(cancellationToken);

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
            string assignedUserId)
        {
            if (project.OwnerUserId == assignedUserId || project.Members.Any(m => m.UserId == assignedUserId))
            {
                return;
            }

            var member = new ProjectMember { ProjectId = project.Id, UserId = assignedUserId };
            project.Members.Add(member);
            dbContext.ProjectMembers.Add(member);
        }
    }
}
