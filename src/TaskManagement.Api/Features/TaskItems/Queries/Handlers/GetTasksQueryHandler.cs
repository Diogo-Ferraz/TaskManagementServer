using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.TaskItems.Queries.Handlers
{
    public class GetTasksQueryHandler : IRequestHandler<GetTasksQuery, IReadOnlyList<TaskItemDto>>
    {
        private const int MaxLimit = 500;
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetTasksQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TaskItemDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var query = _dbContext.TaskItems.AsNoTracking().AsQueryable();

            if (!isAdmin)
            {
                query = query.Where(t => t.Project.OwnerUserId == currentUserId
                    || t.Project.Members.Any(m => m.UserId == currentUserId));
            }

            if (request.ProjectId.HasValue)
            {
                if (!isAdmin)
                {
                    var canAccessProject = await _dbContext.Projects
                        .AnyAsync(p => p.Id == request.ProjectId.Value
                            && (p.OwnerUserId == currentUserId || p.Members.Any(m => m.UserId == currentUserId)),
                            cancellationToken);
                    if (!canAccessProject)
                    {
                        throw new ForbiddenAccessException("User is not authorized to view tasks for this project.");
                    }
                }

                query = query.Where(t => t.ProjectId == request.ProjectId.Value);
            }

            if (!string.IsNullOrWhiteSpace(request.AssignedUserId))
            {
                var assignedUserId = request.AssignedUserId.Trim();
                query = query.Where(t => t.AssignedUserId == assignedUserId);
            }

            if (request.Status.HasValue)
            {
                query = query.Where(t => t.Status == request.Status.Value);
            }

            if (request.UnassignedOnly.GetValueOrDefault())
            {
                query = query.Where(t => t.AssignedUserId == null);
            }

            var limit = request.Limit <= 0 ? 100 : Math.Min(request.Limit, MaxLimit);
            return await query
                .OrderByDescending(t => t.LastModifiedAt)
                .ThenBy(t => t.Title)
                .Take(limit)
                .ProjectTo<TaskItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
    }
}
