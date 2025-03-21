using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class DeleteTaskItemCommandHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IValidator<DeleteTaskItemCommand>> _validatorMock;
        private readonly DeleteTaskItemCommandHandler _handler;

        public DeleteTaskItemCommandHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _userServiceMock = new Mock<IUserService>();
            _validatorMock = new Mock<IValidator<DeleteTaskItemCommand>>();

            _handler = new DeleteTaskItemCommandHandler(
                _taskItemRepositoryMock.Object,
                _userServiceMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequestFromProjectManager_ShouldReturnSuccessResult()
        {
            var taskId = Guid.NewGuid();
            var command = new DeleteTaskItemCommand
            {
                Id = taskId,
                RequestingUserId = "manager123"
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Task to delete",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "regularuser123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeTrue();
            _taskItemRepositoryMock.Verify(x => x.DeleteAsync(existingTask), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentTask_ShouldReturnFailure()
        {
            var command = new DeleteTaskItemCommand
            {
                Id = Guid.NewGuid(),
                RequestingUserId = "manager123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync((TaskItem?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
            _taskItemRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonProjectManagerUser_ShouldReturnFailure()
        {
            var taskId = Guid.NewGuid();
            var command = new DeleteTaskItemCommand
            {
                Id = taskId,
                RequestingUserId = "regularuser123"
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Task to delete",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "regularuser123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to delete tasks");
            _taskItemRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldReturnFailure()
        {
            var taskId = Guid.NewGuid();
            var command = new DeleteTaskItemCommand
            {
                Id = taskId,
                RequestingUserId = "nonexistentuser"
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Task to delete",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "regularuser123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync((User?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Requesting user not found");
            _taskItemRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidCommand_ShouldReturnValidationError()
        {
            var command = new DeleteTaskItemCommand();
            var validationResult = new ValidationResult(
                new[] { new ValidationFailure("Id", "Task ID is required") }
            );

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task ID is required");
            _taskItemRepositoryMock.Verify(x => x.DeleteAsync(It.IsAny<TaskItem>()), Times.Never);
        }
    }
}