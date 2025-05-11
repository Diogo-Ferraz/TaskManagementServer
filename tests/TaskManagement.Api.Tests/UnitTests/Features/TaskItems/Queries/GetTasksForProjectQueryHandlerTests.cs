using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Queries;
using TaskManagement.Api.Features.TaskItems.Queries.Handlers;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Queries
{
    public class GetTasksForProjectQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetTasksForProjectQueryHandler _handler;

        private readonly Guid _projectIdWithTasks = Guid.NewGuid();
        private readonly Guid _projectWithoutAccess = Guid.NewGuid();
        private readonly Guid _nonExistentProjectId = Guid.NewGuid();
        private readonly string _projectOwnerId = "project-owner-task-123";
        private readonly string _projectMemberId = "project-member-task-456";
        private readonly string _unrelatedUserId = "unrelated-user-task-789";
        private readonly Guid _task1Id = Guid.NewGuid();
        private readonly Guid _task2Id = Guid.NewGuid();


        public GetTasksForProjectQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetTasksForProject_{Guid.NewGuid()}")
                .Options;
            _dbContext = new TaskManagementDbContext(options, null);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _mockCurrentUser = new Mock<ICurrentUserService>();

            SeedDatabase();

            _handler = new GetTasksForProjectQueryHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private void SeedDatabase()
        {
            var project1 = new Project { Id = _projectIdWithTasks, Name = "Project With Tasks", OwnerUserId = _projectOwnerId, Members = new List<ProjectMember> { new ProjectMember { UserId = _projectMemberId, ProjectId = _projectIdWithTasks } }, CreatedAt = DateTime.UtcNow, CreatedByUserId = _projectOwnerId, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId };
            var project2 = new Project { Id = _projectWithoutAccess, Name = "Project Without Access", OwnerUserId = "another-owner", CreatedAt = DateTime.UtcNow, CreatedByUserId = "another-owner", LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = "another-owner" };

            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = _task1Id, Title = "Task 1 in Project", ProjectId = _projectIdWithTasks, Project = project1, AssignedUserId = _projectMemberId, CreatedByUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, LastModifiedAt=DateTime.UtcNow, LastModifiedByUserId=_projectOwnerId, Status = TaskStatus.InProgress },
                new TaskItem { Id = _task2Id, Title = "Task 2 in Project", ProjectId = _projectIdWithTasks, Project = project1, AssignedUserId = _projectOwnerId, CreatedByUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow.AddMinutes(1), LastModifiedAt=DateTime.UtcNow.AddMinutes(1), LastModifiedByUserId=_projectOwnerId, Status = TaskStatus.Todo }
            };
            _dbContext.Projects.AddRange(project1, project2);
            _dbContext.TaskItems.AddRange(tasks);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnTasks_WhenUserIsProjectOwner()
        {
            // Arrange
            var query = new GetTasksForProjectQuery { ProjectId = _projectIdWithTasks };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(t => t.Id).Should().Contain(_task1Id);
            result.Select(t => t.Id).Should().Contain(_task2Id);
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnTasks_WhenUserIsProjectMember()
        {
            // Arrange
            var query = new GetTasksForProjectQuery { ProjectId = _projectIdWithTasks };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectMemberId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotMemberOrOwner()
        {
            // Arrange
            var query = new GetTasksForProjectQuery { ProjectId = _projectIdWithTasks };
            _mockCurrentUser.Setup(u => u.Id).Returns(_unrelatedUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowException_WhenProjectDoesNotExist()
        {
            // Arrange
            var query = new GetTasksForProjectQuery { ProjectId = _nonExistentProjectId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var query = new GetTasksForProjectQuery { ProjectId = _projectIdWithTasks };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}