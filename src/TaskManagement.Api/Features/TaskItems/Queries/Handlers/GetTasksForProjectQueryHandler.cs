using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Features.TaskItems.Queries.Handlers
{
    public class GetTasksForProjectQueryHandler : IRequestHandler<GetTasksForProjectQuery, IReadOnlyList<TaskItemDto>>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetTasksForProjectQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TaskItemDto>> Handle(GetTasksForProjectQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            bool canViewProject = await _dbContext.Projects
                .AnyAsync(p => p.Id == request.ProjectId &&
                              (p.OwnerUserId == currentUserId || p.Members.Any(m => m.UserId == currentUserId)),
                          cancellationToken);

            if (!canViewProject)
            {
                throw new ForbiddenAccessException($"User is not authorized to view tasks for Project ID {request.ProjectId}.");
            }

            var taskDtos = await _dbContext.TaskItems
                .Where(t => t.ProjectId == request.ProjectId)
                .OrderBy(t => t.CreatedAt)
                .ProjectTo<TaskItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return taskDtos;
        }
    }
}
