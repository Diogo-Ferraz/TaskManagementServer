using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Application.TaskItems.Commands.Handlers
{
    public class UpdateTaskItemCommandHandler : IRequestHandler<UpdateTaskItemCommand, Result<TaskItemDto>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateTaskItemCommand> _validator;

        public UpdateTaskItemCommandHandler(
            ITaskItemRepository taskItemRepository,
            IUserService userService,
            IMapper mapper,
            IValidator<UpdateTaskItemCommand> validator)
        {
            _taskItemRepository = taskItemRepository;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Result<TaskItemDto>> Handle(UpdateTaskItemCommand request, CancellationToken cancellationToken)
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<TaskItemDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            // Get existing task
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

            // Only the assigned user or project admin can update the task
            if (!await _userService.IsInRoleAsync(requestingUser.Id, Roles.ProjectManager) &&
                taskItem.AssignedUserId != request.RequestingUserId)
            {
                return Result<TaskItemDto>.Failure("User is not authorized to update this task");
            }

            // Verify new assigned user exists and is a RegularUser
            var assignedUser = await _userService.GetUserByIdAsync(request.AssignedUserId);
            if (!await _userService.IsInRoleAsync(assignedUser.Id, Roles.RegularUser))
            {
                return Result<TaskItemDto>.Failure("Assigned user must be a regular user");
            }

            // Update task
            _mapper.Map(request, taskItem);
            taskItem.LastModifiedBy = request.RequestingUserId;
            taskItem.LastModifiedAt = DateTime.UtcNow;

            await _taskItemRepository.UpdateAsync(taskItem);

            // Return mapped DTO
            var taskItemDto = _mapper.Map<TaskItemDto>(taskItem);
            return Result<TaskItemDto>.Success(taskItemDto);
        }
    }
}
