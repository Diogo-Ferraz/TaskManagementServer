using AutoMapper;
using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Features.TaskItems.Commands.Handlers
{
    public class CreateTaskItemCommandHandler : IRequestHandler<CreateTaskItemCommand, TaskItemDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;
        private readonly IMapper _mapper;

        public CreateTaskItemCommandHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService,
            IMapper mapper)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
            _mapper = mapper;
        }

        public async Task<TaskItemDto> Handle(CreateTaskItemCommand request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrEmpty(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            bool canCreateInProject = await _dbContext.Projects
                .AnyAsync(p => p.Id == request.ProjectId &&
                              (p.OwnerUserId == currentUserId || p.Members.Any(m => m.UserId == currentUserId)),
                          cancellationToken);

            if (!canCreateInProject)
            {
                var projectExists = await _dbContext.Projects.AnyAsync(p => p.Id == request.ProjectId, cancellationToken);
                if (!projectExists) throw new NotFoundException($"Project with ID {request.ProjectId} not found.");
                throw new ForbiddenAccessException("User is not authorized to add tasks to this project.");
            }

            var taskItem = _mapper.Map<TaskItem>(request);

            _dbContext.TaskItems.Add(taskItem);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return _mapper.Map<TaskItemDto>(taskItem);
        }
    }
}
