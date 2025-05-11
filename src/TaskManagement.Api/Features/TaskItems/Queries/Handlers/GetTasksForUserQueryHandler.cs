using AutoMapper;
using AutoMapper.QueryableExtensions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Features.TaskItems.Queries.Handlers
{
    public class GetTasksForUserQueryHandler : IRequestHandler<GetTasksForUserQuery, IReadOnlyList<TaskItemDto>>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetTasksForUserQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<IReadOnlyList<TaskItemDto>> Handle(GetTasksForUserQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var taskDtos = await _dbContext.TaskItems
                .Where(t => t.AssignedUserId == currentUserId)
                .OrderByDescending(t => t.DueDate)
                .ThenBy(t => t.Title)
                .ProjectTo<TaskItemDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);

            return taskDtos;
        }
    }
}
