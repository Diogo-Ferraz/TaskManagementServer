using AutoMapper;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.TaskItems.Queries.Handlers
{
    public class GetTaskItemQueryHandler : IRequestHandler<GetTaskItemQuery, Result<TaskItemDto>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetTaskItemQueryHandler(
            ITaskItemRepository taskItemRepository,
            IUserService userService,
            IMapper mapper)
        {
            _taskItemRepository = taskItemRepository;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<Result<TaskItemDto>> Handle(GetTaskItemQuery request, CancellationToken cancellationToken)
        {
            var taskItem = await _taskItemRepository.GetByIdAsync(request.Id);
            if (taskItem == null)
            {
                return Result<TaskItemDto>.Failure("Task not found");
            }

            // Verify requesting user is authorized
            var requestingUser = await _userService.GetUserByIdAsync(request.RequestingUserId);
            if (requestingUser == null)
            {
                return Result<TaskItemDto>.Failure("Requesting user not found");
            }

            // Only the assigned user or project admin can view the task
            if (requestingUser.Role != UserRole.ProjectManager &&
                taskItem.AssignedUserId != request.RequestingUserId)
            {
                return Result<TaskItemDto>.Failure("User is not authorized to view this task");
            }

            var taskItemDto = _mapper.Map<TaskItemDto>(taskItem);
            return Result<TaskItemDto>.Success(taskItemDto);
        }
    }
}
