using AutoMapper;
using MediatR;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
using TaskManagement.Api.Features.Tasks.Models.DTOs;
using TaskManagement.Api.Features.Tasks.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Tasks.Queries.Handlers
{
    public class GetTasksForProjectQueryHandler : IRequestHandler<GetTasksForProjectQuery, Result<IReadOnlyList<TaskItemDto>>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetTasksForProjectQueryHandler(
            ITaskItemRepository taskItemRepository,
            IProjectRepository projectRepository,
            IUserService userService,
            IMapper mapper)
        {
            _taskItemRepository = taskItemRepository;
            _projectRepository = projectRepository;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyList<TaskItemDto>>> Handle(GetTasksForProjectQuery request, CancellationToken cancellationToken)
        {
            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return Result<IReadOnlyList<TaskItemDto>>.Failure("Project not found");
            }

            // Verify requesting user is authorized
            var requestingUser = await _userService.GetUserByIdAsync(request.RequestingUserId);
            if (requestingUser == null)
            {
                return Result<IReadOnlyList<TaskItemDto>>.Failure("Requesting user not found");
            }

            // Only the project admin can view all tasks in a project
            if (!await _userService.IsInRoleAsync(requestingUser.Id, Roles.ProjectManager) &&
                project.UserId != request.RequestingUserId)
            {
                return Result<IReadOnlyList<TaskItemDto>>.Failure("User is not authorized to view tasks in this project");
            }

            var tasks = await _taskItemRepository.GetTasksByProjectIdAsync(request.ProjectId);
            var taskDtos = _mapper.Map<IReadOnlyList<TaskItemDto>>(tasks);

            return Result<IReadOnlyList<TaskItemDto>>.Success(taskDtos);
        }
    }
}
