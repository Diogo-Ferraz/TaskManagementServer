using AutoMapper;
using FluentAssertions;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
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
    public class GetTasksForProjectQueryHandlerTests
    {
        private readonly Mock<ITaskItemRepository> _taskItemRepositoryMock;
        private readonly Mock<IProjectRepository> _projectRepositoryMock;
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly GetTasksForProjectQueryHandler _handler;

        public GetTasksForProjectQueryHandlerTests()
        {
            _taskItemRepositoryMock = new Mock<ITaskItemRepository>();
            _projectRepositoryMock = new Mock<IProjectRepository>();
            _userServiceMock = new Mock<IUserService>();
            _mapperMock = new Mock<IMapper>();
            _handler = new GetTasksForProjectQueryHandler(
                _taskItemRepositoryMock.Object,
                _projectRepositoryMock.Object,
                _userServiceMock.Object,
                _mapperMock.Object
            );
        }

        [Fact]
        public async Task Handle_WithExistingProjectAndProjectOwner_ShouldReturnSuccessResult()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = userId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = userId
            };

            var taskItems = new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", ProjectId = projectId, AssignedUserId = "user1" },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", ProjectId = projectId, AssignedUserId = "user2" }
        };

            var taskItemDtos = taskItems.Select(t => new TaskItemDto
            {
                Id = t.Id,
                AssignedUserId = t.AssignedUserId,
                Title = t.Title,
                ProjectId = t.ProjectId,
            }).ToList();

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            _taskItemRepositoryMock.Setup(x => x.GetTasksByProjectIdAsync(projectId))
                .ReturnsAsync(taskItems);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<TaskItemDto>>(taskItems))
                .Returns(taskItemDtos);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(taskItemDtos);
        }

        [Fact]
        public async Task Handle_WithExistingProjectAndProjectManager_ShouldReturnSuccessResult()
        {
            var projectId = Guid.NewGuid();
            var ownerId = Guid.NewGuid().ToString();
            var managerId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = managerId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = ownerId
            };

            var taskItems = new List<TaskItem>
        {
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 1", ProjectId = projectId, AssignedUserId = "user1" },
            new TaskItem { Id = Guid.NewGuid(), Title = "Task 2", ProjectId = projectId, AssignedUserId = "user2" }
        };

            var taskItemDtos = taskItems.Select(t => new TaskItemDto
            {
                Id = t.Id,
                AssignedUserId = t.AssignedUserId,
                Title = t.Title,
                ProjectId = t.ProjectId,
            }).ToList();

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(managerId))
                .ReturnsAsync(new User { Id = managerId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(managerId, Roles.ProjectManager))
                .ReturnsAsync(true);

            _taskItemRepositoryMock.Setup(x => x.GetTasksByProjectIdAsync(projectId))
                .ReturnsAsync(taskItems);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<TaskItemDto>>(taskItems))
                .Returns(taskItemDtos);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEquivalentTo(taskItemDtos);
        }

        [Fact]
        public async Task Handle_WithNonExistentProject_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = userId };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync((Project?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Project not found");
        }

        [Fact]
        public async Task Handle_WithNonExistentUser_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = userId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = Guid.NewGuid().ToString()
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync((User?)null);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("Requesting user not found");
        }

        [Fact]
        public async Task Handle_WithUnauthorizedUser_ShouldReturnFailureResult()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var ownerId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = userId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = ownerId
            };

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeFalse();
            result.Error.Should().Be("User is not authorized to view tasks in this project");
        }

        [Fact]
        public async Task Handle_WithEmptyTaskList_ShouldReturnEmptyList()
        {
            var projectId = Guid.NewGuid();
            var userId = Guid.NewGuid().ToString();
            var query = new GetTasksForProjectQuery { ProjectId = projectId, RequestingUserId = userId };

            var project = new Project
            {
                Id = projectId,
                Name = "Test Project",
                UserId = userId
            };

            var taskItems = new List<TaskItem>();
            var taskItemDtos = new List<TaskItemDto>();

            _projectRepositoryMock.Setup(x => x.GetByIdAsync(projectId))
                .ReturnsAsync(project);

            _userServiceMock.Setup(x => x.GetUserByIdAsync(userId))
                .ReturnsAsync(new User { Id = userId });

            _userServiceMock.Setup(x => x.IsInRoleAsync(userId, Roles.ProjectManager))
                .ReturnsAsync(false);

            _taskItemRepositoryMock.Setup(x => x.GetTasksByProjectIdAsync(projectId))
                .ReturnsAsync(taskItems);

            _mapperMock.Setup(x => x.Map<IReadOnlyList<TaskItemDto>>(taskItems))
                .Returns(taskItemDtos);

            var result = await _handler.Handle(query, CancellationToken.None);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeEmpty();
        }
    }
}
