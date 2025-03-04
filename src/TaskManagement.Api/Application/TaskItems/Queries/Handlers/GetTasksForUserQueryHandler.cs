using AutoMapper;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Application.TaskItems.Queries.Handlers
{
    public class GetTasksForUserQueryHandler : IRequestHandler<GetTasksForUserQuery, Result<IReadOnlyList<TaskItemDto>>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetTasksForUserQueryHandler(
            ITaskItemRepository taskItemRepository,
            IUserService userService,
            IMapper mapper)
        {
            _taskItemRepository = taskItemRepository;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyList<TaskItemDto>>> Handle(GetTasksForUserQuery request, CancellationToken cancellationToken)
        {
            if (!await _userService.IsInRoleAsync(request.UserId, Roles.RegularUser))
            {
                return Result<IReadOnlyList<TaskItemDto>>.Failure("User not found or not authorized");
            }

            var tasks = await _taskItemRepository.GetTasksByUserIdAsync(request.UserId);
            var taskDtos = _mapper.Map<IReadOnlyList<TaskItemDto>>(tasks);

            return Result<IReadOnlyList<TaskItemDto>>.Success(taskDtos);
        }
    }
}
