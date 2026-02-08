using AutoMapper;
using MediatR;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Features.Projects.Commands.Handlers
{
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, ProjectDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IActivityPublisher _activityPublisher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public CreateProjectCommandHandler(
            TaskManagementDbContext dbContext,
            IActivityPublisher activityPublisher,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _activityPublisher = activityPublisher;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<ProjectDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var project = _mapper.Map<Project>(request);
            project.OwnerUserId = currentUserId;

            _dbContext.Projects.Add(project);
            _dbContext.ProjectMembers.Add(new ProjectMember
            {
                Project = project,
                UserId = currentUserId,
                JoinedAt = DateTime.UtcNow,
                AddedByUserId = currentUserId
            });

            var activityLog = new ActivityLog
            {
                Type = ActivityType.ProjectCreated,
                ProjectId = project.Id,
                ProjectName = project.Name
            };
            _dbContext.ActivityLogs.Add(activityLog);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await _activityPublisher.PublishAsync(activityLog, cancellationToken);

            return _mapper.Map<ProjectDto>(project);
        }
    }
}
