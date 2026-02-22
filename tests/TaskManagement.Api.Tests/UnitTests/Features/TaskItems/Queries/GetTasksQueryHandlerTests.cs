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
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Queries
{
    public class GetTasksQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IUserDirectoryService> _mockUserDirectoryService;
        private readonly GetTasksQueryHandler _handler;

        private readonly Guid _projectId = Guid.NewGuid();
        private readonly Guid _otherProjectId = Guid.NewGuid();
        private readonly string _ownerId = "task-query-owner";
        private readonly string _memberId = "task-query-member";
        private readonly string _otherUserId = "task-query-other";

        public GetTasksQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetTasksQuery_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockUserDirectoryService = new Mock<IUserDirectoryService>();
            _mockUserDirectoryService
                .Setup(service => service.GetDisplayNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((string userId, CancellationToken _) => $"Test User {userId}");
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);
            SeedDatabase();

            var mapper = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>()).CreateMapper();
            _handler = new GetTasksQueryHandler(_dbContext, _mockCurrentUser.Object, _mockUserDirectoryService.Object, mapper);
        }

        private void SeedDatabase()
        {
            var now = DateTime.UtcNow;
            var project = new Project
            {
                Id = _projectId,
                Name = "Project A",
                OwnerUserId = _ownerId,
                CreatedAt = now,
                CreatedByUserId = _ownerId,
                LastModifiedAt = now,
                LastModifiedByUserId = _ownerId
            };
            project.Members.Add(new ProjectMember
            {
                ProjectId = _projectId,
                UserId = _memberId,
                JoinedAt = now,
                AddedByUserId = _ownerId
            });

            var otherProject = new Project
            {
                Id = _otherProjectId,
                Name = "Project B",
                OwnerUserId = _otherUserId,
                CreatedAt = now,
                CreatedByUserId = _otherUserId,
                LastModifiedAt = now,
                LastModifiedByUserId = _otherUserId
            };

            _dbContext.Projects.AddRange(project, otherProject);
            _dbContext.TaskItems.AddRange(
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Task A1",
                    Description = "Frontend work",
                    ProjectId = _projectId,
                    Project = project,
                    AssignedUserId = _memberId,
                    Status = TaskStatus.Todo,
                    CreatedAt = now,
                    CreatedByUserId = _ownerId,
                    LastModifiedAt = now.AddDays(-5),
                    LastModifiedByUserId = _ownerId
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Task A2",
                    Description = "Backend API search",
                    ProjectId = _projectId,
                    Project = project,
                    AssignedUserId = null,
                    Status = TaskStatus.InProgress,
                    CreatedAt = now,
                    CreatedByUserId = _ownerId,
                    LastModifiedAt = now.AddDays(-1),
                    LastModifiedByUserId = _memberId
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Task B1",
                    Description = "Infra cleanup",
                    ProjectId = _otherProjectId,
                    Project = otherProject,
                    AssignedUserId = _otherUserId,
                    Status = TaskStatus.Done,
                    CreatedAt = now,
                    CreatedByUserId = _otherUserId,
                    LastModifiedAt = now.AddDays(-2),
                    LastModifiedByUserId = _otherUserId
                });

            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnAccessibleTasks_ForNonAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_memberId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);

            var result = await _handler.Handle(new GetTasksQuery(), CancellationToken.None);

            result.Should().HaveCount(2);
            result.All(t => t.ProjectId == _projectId).Should().BeTrue();
        }

        [Fact]
        public async Task Handle_ShouldFilterTasks_ForAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            var result = await _handler.Handle(new GetTasksQuery
            {
                ProjectId = _otherProjectId,
                AssignedUserId = _otherUserId,
                Status = TaskStatus.Done
            }, CancellationToken.None);

            result.Should().HaveCount(1);
            result.First().ProjectId.Should().Be(_otherProjectId);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbidden_WhenNonAdminFiltersByInaccessibleProject()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_memberId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);

            Func<Task> act = async () => await _handler.Handle(new GetTasksQuery { ProjectId = _otherProjectId }, CancellationToken.None);

            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldFilterByUpdatedByUserId_AndSearch_ForAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            var result = await _handler.Handle(new GetTasksQuery
            {
                UpdatedByUserId = _memberId,
                Search = "Backend"
            }, CancellationToken.None);

            result.Should().HaveCount(1);
            result[0].Title.Should().Be("Task A2");
        }

        [Fact]
        public async Task Handle_ShouldFilterByLastModifiedDateRange_ForAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            var now = DateTime.UtcNow;
            var result = await _handler.Handle(new GetTasksQuery
            {
                LastModifiedFrom = now.AddDays(-3),
                LastModifiedTo = now
            }, CancellationToken.None);

            result.Should().HaveCount(2);
            result.Should().Contain(t => t.Title == "Task A2");
            result.Should().Contain(t => t.Title == "Task B1");
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
