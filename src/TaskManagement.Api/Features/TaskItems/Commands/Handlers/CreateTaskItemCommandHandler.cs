using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.TaskItems.Commands.Handlers
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
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<TaskItemDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var project = await _projectRepository.GetByIdAsync(request.ProjectId);
            if (project == null)
            {
                return Result<TaskItemDto>.Failure("Project not found");
            }

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

            var assignedUser = await _userService.GetUserByIdAsync(request.AssignedUserId);
            if (assignedUser == null)
            {
                return Result<TaskItemDto>.Failure("Assigned user not found");
            }

            if (!await _userService.IsInRoleAsync(assignedUser.Id, Roles.RegularUser))
            {
                return Result<TaskItemDto>.Failure("Assigned user must be a regular user");
            }

            var taskItem = _mapper.Map<TaskItem>(request);
            taskItem.CreatedBy = request.RequestingUserId;
            taskItem.LastModifiedBy = request.RequestingUserId;

            await _taskItemRepository.AddAsync(taskItem);

            var taskItemDto = _mapper.Map<TaskItemDto>(taskItem);
            taskItemDto.ProjectName = project.Name;
            taskItemDto.AssignedUserName = assignedUser.UserName;

            return Result<TaskItemDto>.Success(taskItemDto);
        }
    }
}
