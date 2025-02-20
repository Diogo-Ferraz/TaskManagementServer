using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.TaskItems.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Application.TaskItems.Commands.Handlers
{
    public class CreateTaskItemCommandHandler : IRequestHandler<CreateTaskItemCommand, Result<TaskItemDto>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateTaskItemCommand> _validator;

        public CreateTaskItemCommandHandler(
            ITaskItemRepository taskItemRepository,
            IProjectRepository projectRepository,
            IUserService userService,
            IMapper mapper,
            IValidator<CreateTaskItemCommand> validator)
        {
            _taskItemRepository = taskItemRepository;
            _projectRepository = projectRepository;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Result<TaskItemDto>> Handle(CreateTaskItemCommand request, CancellationToken cancellationToken)
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<TaskItemDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            // Verify project exists
            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return Result<TaskItemDto>.Failure("Project not found");
            }

            // Verify requesting user is authorized (ProjectAdmin or AssignedUser)
            var requestingUser = await _userService.GetUserByIdAsync(request.RequestingUserId);
            if (requestingUser == null)
            {
                return Result<TaskItemDto>.Failure("Requesting user not found");
            }

            if (!await _userService.IsInRoleAsync(requestingUser.Id, Roles.ProjectManager) &&
                project.UserId != request.RequestingUserId)
            {
                return Result<TaskItemDto>.Failure("User is not authorized to create tasks in this project");
            }

            // Verify assigned user exists and is a RegularUser
            var assignedUser = await _userService.GetUserByIdAsync(request.AssignedUserId);
            if (!await _userService.IsInRoleAsync(assignedUser.Id, Roles.RegularUser))
            {
                return Result<TaskItemDto>.Failure("Assigned user must be a regular user");
            }

            // Create and save task
            var taskItem = _mapper.Map<TaskItem>(request);
            taskItem.CreatedBy = request.RequestingUserId;
            taskItem.LastModifiedBy = request.RequestingUserId;

            await _taskItemRepository.AddAsync(taskItem);

            // Return mapped DTO
            var taskItemDto = _mapper.Map<TaskItemDto>(taskItem);
            taskItemDto.ProjectName = project.Name;
            taskItemDto.AssignedUserName = assignedUser.UserName;

            return Result<TaskItemDto>.Success(taskItemDto);
        }
    }
}
