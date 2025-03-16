using AutoMapper;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
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
    public class CreateTaskItemCommandHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IValidator<CreateTaskItemCommand>> _validatorMock;
        private readonly CreateTaskItemCommandHandler _handler;

        public CreateTaskItemCommandHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _validatorMock = new Mock<IValidator<CreateTaskItemCommand>>();

            _handler = new CreateTaskItemCommandHandler(
                _taskItemRepositoryMock.Object,
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object,
                _validatorMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRequestFromProjectManager_ShouldReturnSuccessResult()
        {
            var command = new CreateTaskItemCommand
            {
                Title = "Test Task",
                Description = "Test Description",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "user123",
                RequestingUserId = "manager123",
                Status = Api.Features.TaskItems.Models.TaskStatus.Todo,
                DueDate = DateTime.UtcNow.AddDays(7)
            };

            var project = new Project
            {
                Id = command.ProjectId,
                Name = "Test Project",
                UserId = "differentUser"
            };

            var taskItem = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = command.Title,
                Description = command.Description,
                ProjectId = command.ProjectId,
                AssignedUserId = command.AssignedUserId
            };

            var taskItemDto = new TaskItemDto
            {
                Id = taskItem.Id,
                Title = taskItem.Title,
                Description = taskItem.Description,
                ProjectId = taskItem.ProjectId,
                ProjectName = project.Name,
                AssignedUserId = taskItem.AssignedUserId,
                AssignedUserName = "testuser"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.ProjectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.AssignedUserId))
                .ReturnsAsync(new User { Id = command.AssignedUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.AssignedUserId, Roles.RegularUser))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map<TaskItem>(command))
                .Returns(taskItem);

            _mapperMock.Setup(x => x.Map<TaskItemDto>(taskItem))
                .Returns(taskItemDto);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().NotBeNull();
            result.Value.Should().BeEquivalentTo(taskItemDto);

            _taskItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonExistentProject_ShouldReturnFailure()
        {
            var command = new CreateTaskItemCommand
            {
                Title = "Test Task",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "user123",
                RequestingUserId = "manager123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.ProjectId))
                .ReturnsAsync((Project)null);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project not found");
            _taskItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailure()
        {
            var command = new CreateTaskItemCommand
            {
                Title = "Test Task",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "user123",
                RequestingUserId = "unauthorized123"
            };

            var project = new Project
            {
                Id = command.ProjectId,
                Name = "Project123",
                UserId = "differentUser"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.ProjectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(command.RequestingUserId))
                .ReturnsAsync(new User { Id = command.RequestingUserId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(command.RequestingUserId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to create tasks in this project");
            _taskItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithNonRegularUserAssigned_ShouldReturnFailure()
        {
            var command = new CreateTaskItemCommand
            {
                Title = "Test Task",
                ProjectId = Guid.NewGuid(),
                AssignedUserId = "manager123",
                RequestingUserId = "manager123"
            };

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(command.ProjectId))
                .ReturnsAsync(new Project { Name = "Project123", UserId = "manager123" });

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
            _taskItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithInvalidCommand_ShouldReturnValidationError()
        {
            var command = new CreateTaskItemCommand();
            var validationResult = new ValidationResult(
                new[] { new ValidationFailure("Title", "Title is required") }
            );

            _validatorMock.Setup(x => x.ValidateAsync(command, It.IsAny<CancellationToken>()))
                .ReturnsAsync(validationResult);

            var result = await _handler.Handle(command, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Title is required");
            _taskItemRepositoryMock.Verify(x => x.AddAsync(It.IsAny<TaskItem>()), Times.Never);
        }
    }
}
