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
    public class GetTaskItemQueryHandler : IRequestHandler<GetTaskItemQuery, TaskItemDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public GetTaskItemQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<TaskItemDto> Handle(GetTaskItemQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);
            var query = _dbContext.TaskItems.Where(t => t.Id == request.Id);
            if (!isAdmin)
            {
                query = query.Where(t => t.Project.OwnerUserId == currentUserId || t.Project.Members.Any(m => m.UserId == currentUserId));
            }

            var taskItemDto = await query
                .ProjectTo<TaskItemDto>(_mapper.ConfigurationProvider)
                .FirstOrDefaultAsync(cancellationToken);

            if (taskItemDto == null)
            {
                throw new NotFoundException($"TaskItem with ID {request.Id} not found or access denied.");
            }

            return taskItemDto;
        }
    }
}
