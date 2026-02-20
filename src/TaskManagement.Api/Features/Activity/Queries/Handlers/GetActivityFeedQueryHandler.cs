using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Models.DTOs;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Activity.Queries.Handlers
{
    public class GetActivityFeedQueryHandler : IRequestHandler<GetActivityFeedQuery, IReadOnlyList<ActivityLogDto>>
    {
        private const int MaxPageSize = 200;
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectMembershipService _projectMembershipService;
        private readonly IMapper _mapper;

        public GetActivityFeedQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IProjectMembershipService projectMembershipService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _projectMembershipService = projectMembershipService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<ActivityLogDto>> Handle(GetActivityFeedQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var page = request.Page <= 0 ? 1 : request.Page;
            var pageSize = request.PageSize <= 0 ? 50 : Math.Min(request.PageSize, MaxPageSize);
            if (request.Limit.HasValue && request.Limit.Value > 0)
            {
                page = 1;
                pageSize = Math.Min(request.Limit.Value, MaxPageSize);
            }

            var query = _dbContext.ActivityLogs.AsNoTracking();
            var isAdministrator = _currentUserService.IsInRole(Roles.Administrator);

            if (request.ProjectId.HasValue)
            {
                if (!isAdministrator)
                {
                    var isMember = await _projectMembershipService.IsMemberAsync(request.ProjectId.Value, currentUserId, cancellationToken);
                    if (!isMember)
                    {
                        // Project may be deleted; allow actor to still read their own delete event history.
                        var canAccessDeletedProjectActivity = await _dbContext.ActivityLogs
                            .AsNoTracking()
                            .AnyAsync(
                                a => a.ProjectId == request.ProjectId.Value
                                    && a.Type == ActivityType.ProjectDeleted
                                    && a.CreatedByUserId == currentUserId,
                                cancellationToken);
                        if (!canAccessDeletedProjectActivity)
                        {
                            throw new ForbiddenAccessException("User is not authorized to view activity for this project.");
                        }
                    }
                }

                query = query.Where(activity => activity.ProjectId == request.ProjectId.Value);
            }
            else
            {
                if (isAdministrator)
                {
                    // Administrators can see activity across all projects.
                    query = query.Where(activity => activity.ProjectId != null);
                }
                else
                {
                    var projectIds = await _projectMembershipService.GetProjectIdsForUserAsync(currentUserId, cancellationToken);
                    query = query.Where(activity =>
                        (activity.ProjectId != null && projectIds.Contains(activity.ProjectId.Value))
                        || (activity.Type == ActivityType.ProjectDeleted && activity.CreatedByUserId == currentUserId));
                }
            }

            if (request.MineOnly)
            {
                query = query.Where(activity => activity.CreatedByUserId == currentUserId);
            }

            var activityLogs = await query
                .OrderByDescending(activity => activity.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return _mapper.Map<IReadOnlyList<ActivityLogDto>>(activityLogs);
        }
    }
}
