using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Api.Features.Tasks.Models;
using TaskManagement.Api.Features.Tasks.Models.DTOs;
using TaskManagement.Api.Features.Tasks.Queries;
using TaskManagement.Api.Features.Tasks.Queries.Handlers;
using TaskManagement.Api.Features.Tasks.Repositories.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.TaskItems.Queries
{
    public class GetTasksForUserQueryHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetTasksForUserQueryHandler _handler;

        public GetTasksForUserQueryHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetTasksForUserQueryHandler(
                _taskItemRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithValidRegularUser_ShouldReturnSuccessResult()
        {
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForUserQuery { UserId = userId };

            var taskItems = new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", AssignedUserId = "user1" },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", AssignedUserId = "user2" }
        };

            var taskItemDtos = taskItems.Select(t => new TaskItemDto
            {
                Id = t.Id,
                AssignedUserId = t.AssignedUserId,
                Title = t.Title
            }).ToList();

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.RegularUser))
                .ReturnsAsync(true);

            _taskItemRepositoryMock.Setup(x => x.GetTasksByUserIdAsync(userId))
                .ReturnsAsync(taskItems);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<TaskItemDto>>(taskItems))
                .Returns(taskItemDtos);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(taskItemDtos);

            _taskItemRepositoryMock.Verify(x => x.GetTasksByUserIdAsync(userId), Times.Once);
        }

        [Fact]
        public async Task Handle_WithNonRegularUser_ShouldReturnFailureResult()
        {
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForUserQuery { UserId = userId };

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.RegularUser))
                .ReturnsAsync(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User not found or not authorized");

            _taskItemRepositoryMock.Verify(x => x.GetTasksByUserIdAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WithEmptyTaskList_ShouldReturnEmptyList()
        {
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForUserQuery { UserId = userId };

            var taskItems = new List<TaskItem>();
            var taskItemDtos = new List<TaskItemDto>();

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.RegularUser))
                .ReturnsAsync(true);

            _taskItemRepositoryMock.Setup(x => x.GetTasksByUserIdAsync(userId))
                .ReturnsAsync(taskItems);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<TaskItemDto>>(taskItems))
                .Returns(taskItemDtos);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
    }
}