using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Projects.Commands.Handlers
{
    public class PatchProjectCommandHandler : IRequestHandler<PatchProjectCommand, ProjectDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IActivityPublisher _activityPublisher;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public PatchProjectCommandHandler(
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

        public async Task<ProjectDto> Handle(PatchProjectCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var project = await _dbContext.Projects
                .FirstOrDefaultAsync(p => p.Id == request.Id, cancellationToken);

            if (project == null)
            {
                throw new NotFoundException(nameof(Project), request.Id);
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var isProjectManager = _currentUserService.IsInRole(Roles.ProjectManager);
            if (!isAdmin && !isProjectManager && project.OwnerUserId != currentUserId)
            {
                throw new ForbiddenAccessException("User is not authorized to update this project.");
            }

            var previousName = project.Name;
            _mapper.Map(request, project);

            ActivityLog? activityLog = null;
            if (!string.Equals(previousName, project.Name, StringComparison.Ordinal))
            {
                activityLog = new ActivityLog
                {
                    Type = ActivityType.ProjectRenamed,
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    OldValue = previousName,
                    NewValue = project.Name
                };
                _dbContext.ActivityLogs.Add(activityLog);
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            if (activityLog != null)
            {
                await _activityPublisher.PublishAsync(activityLog, cancellationToken);
            }

            return _mapper.Map<ProjectDto>(project);
        }
    }
}
