using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.TaskItems.Commands.Handlers
{
    public class DeleteTaskItemCommandHandler : IRequestHandler<DeleteTaskItemCommand, Result<bool>>
    {
        private readonly ITaskItemRepository _taskItemRepository;
        private readonly IUserService _userService;
        private readonly IValidator<DeleteTaskItemCommand> _validator;

        public DeleteTaskItemCommandHandler(
            ITaskItemRepository taskItemRepository,
            IUserService userService,
            IValidator<DeleteTaskItemCommand> validator)
        {
            _taskItemRepository = taskItemRepository;
            _userService = userService;
            _validator = validator;
        }

        public async Task<Result<bool>> Handle(DeleteTaskItemCommand request, CancellationToken cancellationToken)
        {
            // Validate request
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            // Get task
            var taskItem = await _taskItemRepository.GetByIdAsync(request.Id);
            if (taskItem == null)
            {
                return Result<bool>.Failure("Task not found");
            }

            // Verify requesting user is authorized
            var requestingUser = await _userService.GetUserByIdAsync(request.RequestingUserId);
            if (requestingUser == null)
            {
                return Result<bool>.Failure("Requesting user not found");
            }

            // Only the project admin can delete tasks
            if (requestingUser.Role != UserRole.ProjectManager)
            {
                return Result<bool>.Failure("User is not authorized to delete tasks");
            }

            await _taskItemRepository.DeleteAsync(taskItem);
            return Result<bool>.Success(true);
        }
    }
}
