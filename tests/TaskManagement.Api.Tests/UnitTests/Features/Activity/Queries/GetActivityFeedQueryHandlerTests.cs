using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Activity.Mappings;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Queries;
using TaskManagement.Api.Features.Activity.Queries.Handlers;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Activity.Queries
{
    public class GetActivityFeedQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IProjectMembershipService> _mockProjectMembershipService;
        private readonly GetActivityFeedQueryHandler _handler;

        private readonly Guid _projectAId = Guid.NewGuid();
        private readonly Guid _projectBId = Guid.NewGuid();
        private readonly Guid _projectCId = Guid.NewGuid();
        private readonly string _userId = "user-activity-1";
        private readonly string _adminUserId = "admin-activity-1";

        public GetActivityFeedQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_ActivityFeed_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockProjectMembershipService = new Mock<IProjectMembershipService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<ActivityMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            SeedDatabase();

            _handler = new GetActivityFeedQueryHandler(
                _dbContext,
                _mockCurrentUser.Object,
                _mockProjectMembershipService.Object,
                _mapper);
        }

        private void SeedDatabase()
        {
            var now = DateTime.UtcNow;
            _dbContext.ActivityLogs.AddRange(
                new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    ProjectId = _projectAId,
                    ProjectName = "Project A",
                    Type = ActivityType.ProjectCreated,
                    CreatedAt = now.AddMinutes(-5),
                    CreatedByUserId = "creator-a",
                    CreatedByUserName = "Creator A"
                },
                new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    ProjectId = _projectBId,
                    ProjectName = "Project B",
                    Type = ActivityType.TaskCreated,
                    TaskItemId = Guid.NewGuid(),
                    TaskTitle = "Task B1",
                    CreatedAt = now.AddMinutes(-3),
                    CreatedByUserId = "creator-b",
                    CreatedByUserName = "Creator B"
                },
                new ActivityLog
                {
                    Id = Guid.NewGuid(),
                    ProjectId = _projectCId,
                    ProjectName = "Project C",
                    Type = ActivityType.TaskStatusChanged,
                    TaskItemId = Guid.NewGuid(),
                    TaskTitle = "Task C1",
                    OldStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus.Todo,
                    NewStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus.InProgress,
                    CreatedAt = now.AddMinutes(-1),
                    CreatedByUserId = "creator-c",
                    CreatedByUserName = "Creator C"
                });
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnOnlyMemberProjects_WhenUserIsNotAdmin()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);
            _mockProjectMembershipService
                .Setup(s => s.GetProjectIdsForUserAsync(_userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<Guid> { _projectAId, _projectBId });

            var query = new GetActivityFeedQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Select(r => r.ProjectId).Should().BeEquivalentTo(new[] { _projectAId, _projectBId });
        }

        [Fact]
        public async Task Handle_ShouldReturnAllProjects_WhenUserIsAdmin()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.Id).Returns(_adminUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            var query = new GetActivityFeedQuery();

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(3);
            result.Select(r => r.ProjectId).Should().BeEquivalentTo(new[] { _projectAId, _projectBId, _projectCId });
        }

        [Fact]
        public async Task Handle_ShouldThrowForbidden_WhenUserNotMemberAndProjectFilterIsProvided()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);
            _mockProjectMembershipService
                .Setup(s => s.IsMemberAsync(_projectCId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var query = new GetActivityFeedQuery { ProjectId = _projectCId };

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldReturnFilteredProject_WhenUserIsMember()
        {
            // Arrange
            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);
            _mockProjectMembershipService
                .Setup(s => s.IsMemberAsync(_projectBId, _userId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var query = new GetActivityFeedQuery { ProjectId = _projectBId };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().HaveCount(1);
            result.First().ProjectId.Should().Be(_projectBId);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
