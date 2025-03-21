using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class UpdateTaskItemCommandHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<UpdateTaskItemCommand>> _validatorMock;
        private readonly UpdateTaskItemCommandHandler _handler;

        public UpdateTaskItemCommandHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<UpdateTaskItemCommand>>();

            _handler = new UpdateTaskItemCommandHandler(
                _taskItemRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequestFromProjectManager_ShouldReturnSuccessResult()
        {
            var taskId = Guid.NewGuid();
            var command = new UpdateTaskItemCommand
            {
                Id = taskId,
                Title = "Updated Task",
                Description = "Updated Description",
                Status = Api.Features.TaskItems.Models.TaskStatus.InProgress,
                AssignedUserId = "user123",
                RequestingUserId = "manager123",
                DueDate = DateTime.UtcNow.AddDays(10)
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Original Task",
                Description = "Original Description",
                Status = Api.Features.TaskItems.Models.TaskStatus.Todo,
                AssignedUserId = "olduser123",
                ProjectId = Guid.NewGuid()
            };

            var updatedTaskDto = new TaskItemDto
            {
                Id = taskId,
                Title = command.Title,
                Description = command.Description,
                Status = command.Status,
                AssignedUserId = command.AssignedUserId
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.AssignedUserId))
                .ReturnsAsync(new User { Id = command.AssignedUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.AssignedUserId, Roles.RegularUser))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map(command, existingTask))
                .Callback<UpdateTaskItemCommand, TaskItem>((cmd, task) =>
                {
                    task.Title = cmd.Title;
                    task.Description = cmd.Description;
                    task.Status = cmd.Status;
                    task.AssignedUserId = cmd.AssignedUserId;
                    task.DueDate = cmd.DueDate;
                });

            _mapperMock.Setup(x => x.Map<TaskItemDto>(existingTask))
                .Returns(updatedTaskDto);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(updatedTaskDto);

            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Once);
            existingTask.LastModifiedBy.Should().Be(command.RequestingUserId);
        }

        [Fact]
        public async Task Handle_WithValidRequestFromAssignedUser_ShouldReturnSuccessResult()
        {
            var taskId = Guid.NewGuid();
            var userId = "user123";
            var command = new UpdateTaskItemCommand
            {
                Id = taskId,
                Title = "Updated Task",
                Description = "Updated Description",
                Status = Api.Features.TaskItems.Models.TaskStatus.InProgress,
                AssignedUserId = userId,
                RequestingUserId = userId,
                DueDate = DateTime.UtcNow.AddDays(10)
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Original Task",
                Description = "Original Description",
                Status = Api.Features.TaskItems.Models.TaskStatus.Todo,
                AssignedUserId = userId,
                ProjectId = Guid.NewGuid()
            };

            var updatedTaskDto = new TaskItemDto
            {
                Id = taskId,
                Title = command.Title,
                Description = command.Description,
                Status = command.Status,
                AssignedUserId = command.AssignedUserId
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.AssignedUserId))
                .ReturnsAsync(new User { Id = command.AssignedUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.AssignedUserId, Roles.RegularUser))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map(command, existingTask))
                .Callback<UpdateTaskItemCommand, TaskItem>((cmd, task) =>
                {
                    task.Title = cmd.Title;
                    task.Description = cmd.Description;
                    task.Status = cmd.Status;
                    task.AssignedUserId = cmd.AssignedUserId;
                    task.DueDate = cmd.DueDate;
                });

            _mapperMock.Setup(x => x.Map<TaskItemDto>(existingTask))
                .Returns(updatedTaskDto);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(updatedTaskDto);

            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Once);
            existingTask.LastModifiedBy.Should().Be(command.RequestingUserId);
        }

        [Fact]
        public async Task Handle_WithNonExistentTask_ShouldReturnFailure()
        {
            var command = new UpdateTaskItemCommand
            {
                Id = Guid.NewGuid(),
                Title = "Updated Task",
                RequestingUserId = "manager123",
                AssignedUserId = "user123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync((TaskItem?)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailure()
        {
            var taskId = Guid.NewGuid();
            var command = new UpdateTaskItemCommand
            {
                Id = taskId,
                Title = "Updated Task",
                RequestingUserId = "unauthorized123",
                AssignedUserId = "user123"
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Original Task",
                AssignedUserId = "differentUser123",
                ProjectId = Guid.NewGuid()
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
            result.Error.Should().Be("User is not authorized to update this task");
            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonRegularUserAssigned_ShouldReturnFailure()
        {
            var taskId = Guid.NewGuid();
            var command = new UpdateTaskItemCommand
            {
                Id = taskId,
                Title = "Updated Task",
                AssignedUserId = "admin123",
                RequestingUserId = "manager123"
            };

            var existingTask = new TaskItem
            {
                Id = taskId,
                Title = "Original Task",
                AssignedUserId = "user123",
                ProjectId = Guid.NewGuid()
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(command.Id))
                .ReturnsAsync(existingTask);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.AssignedUserId))
                .ReturnsAsync(new User { Id = command.AssignedUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.AssignedUserId, Roles.RegularUser))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Assigned user must be a regular user");
            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidCommand_ShouldReturnValidationError()
        {
            var command = new UpdateTaskItemCommand();
            var validationResult = new ValidationResult(
                new[] { new ValidationFailure("Id", "Task ID is required") }
            );

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task ID is required");
            _taskItemRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<TaskItem>()), Times.Never);
        }
    }
}