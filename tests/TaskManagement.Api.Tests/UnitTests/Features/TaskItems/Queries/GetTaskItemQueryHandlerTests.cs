using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Queries;
using TaskManagement.Api.Features.TaskItems.Queries.Handlers;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Queries
{
    public class GetTaskItemQueryHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetTaskItemQueryHandler _handler;

        public GetTaskItemQueryHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetTaskItemQueryHandler(
                _taskItemRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithExistingTaskAndAuthorizedUser_ShouldReturnSuccessResult()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTaskItemQuery { Id = taskId, RequestingUserId = userId };

            var taskItem = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = userId
            };

            var taskItemDto = new TaskItemDto
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = userId
            };

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync(taskItem);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            _mapperMock.Setup(x => x.Map<TaskItemDto>(taskItem))
                .Returns(taskItemDto);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(taskItemDto);
        }

        [Fact]
        public async Task Handle_WithNonExistentTask_ShouldReturnFailureResult()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTaskItemQuery { Id = taskId, RequestingUserId = userId };

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync((TaskItem?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Task not found");
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldReturnFailureResult()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTaskItemQuery { Id = taskId, RequestingUserId = userId };

            var taskItem = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = "differentUserId"
            };

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync(taskItem);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync((User?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Requesting user not found");
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailureResult()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var differentUserId = Guid.NewGuid().ToString();
            var query = new GetTaskItemQuery { Id = taskId, RequestingUserId = userId };

            var taskItem = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = differentUserId
            };

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync(taskItem);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to view this task");
        }

        [Fact]
        public async Task Handle_WithProjectManagerUser_ShouldReturnSuccessResult()
        {
            var taskId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var differentUserId = Guid.NewGuid().ToString();
            var query = new GetTaskItemQuery { Id = taskId, RequestingUserId = userId };

            var taskItem = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = differentUserId
            };

            var taskItemDto = new TaskItemDto
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                AssignedUserId = differentUserId
            };

            _taskItemRepositoryMock.Setup(x => x.GetByIdAsync(taskId))
                .ReturnsAsync(taskItem);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _mapperMock.Setup(x => x.Map<TaskItemDto>(taskItem))
                .Returns(taskItemDto);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(taskItemDto);
        }
    }
}