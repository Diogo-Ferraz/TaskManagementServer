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
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Queries
{
    public class GetTasksForUserQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetTasksForUserQueryHandler _handler;

        private readonly string _testUserId1 = "user-assignee-1";
        private readonly string _testUserId2 = "user-assignee-2";
        private readonly Guid _task1Id = Guid.NewGuid();
        private readonly Guid _task2Id = Guid.NewGuid();
        private readonly Guid _task3Id = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();


        public GetTasksForUserQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetTasksForUser_{Guid.NewGuid()}")
                .Options;
            _dbContext = new TaskManagementDbContext(options, null);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _mockCurrentUser = new Mock<ICurrentUserService>();

            SeedDatabase();

            _handler = new GetTasksForUserQueryHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private void SeedDatabase()
        {
            var project = new Project { Id = _projectId, Name = "Project For Tasks", OwnerUserId = "owner", CreatedAt = DateTime.UtcNow, CreatedByUserId = "owner", LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = "owner" };
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = _task1Id, Title = "Task 1", ProjectId = _projectId, Project = project, AssignedUserId = _testUserId1, CreatedByUserId="owner", CreatedAt=DateTime.UtcNow, LastModifiedAt=DateTime.UtcNow, LastModifiedByUserId="owner" },
                new TaskItem { Id = _task2Id, Title = "Task 2", ProjectId = _projectId, Project = project, AssignedUserId = _testUserId2, CreatedByUserId="owner", CreatedAt=DateTime.UtcNow, LastModifiedAt=DateTime.UtcNow, LastModifiedByUserId="owner" },
                new TaskItem { Id = _task3Id, Title = "Task 3", ProjectId = _projectId, Project = project, AssignedUserId = _testUserId1, CreatedByUserId="owner", CreatedAt=DateTime.UtcNow, LastModifiedAt=DateTime.UtcNow, LastModifiedByUserId="owner" }
            };
            _dbContext.Projects.Add(project);
            _dbContext.TaskItems.AddRange(tasks);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnTasksAssignedToCurrentUser()
        {
            // Arrange
            var query = new GetTasksForUserQuery();
            _mockCurrentUser.Setup(u => u.Id).Returns(_testUserId1);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(t => t.Id).Should().Contain(_task1Id);
            result.Select(t => t.Id).Should().Contain(_task3Id);
            result.Select(t => t.Id).Should().NotContain(_task2Id);
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenNoTasksAssignedToCurrentUser()
        {
            // Arrange
            var query = new GetTasksForUserQuery();
            _mockCurrentUser.Setup(u => u.Id).Returns("user-with-no-tasks");

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var query = new GetTasksForUserQuery();
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