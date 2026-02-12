using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Dashboard.Queries;
using TaskManagement.Api.Features.Dashboard.Queries.Handlers;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Shared.Models;
using TaskItemStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.Dashboard.Queries
{
    public class GetDashboardSummaryQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUserService;

        private const string UserId = "user-dashboard-1";
        private const string OtherUserId = "user-dashboard-2";

        private readonly Guid _ownedProjectId = Guid.NewGuid();
        private readonly Guid _memberProjectId = Guid.NewGuid();
        private readonly Guid _unrelatedProjectId = Guid.NewGuid();

        public GetDashboardSummaryQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase($"TestDb_DashboardSummary_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUserService = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUserService.Object);

            SeedDatabase();
        }

        [Fact]
        public async Task Handle_ShouldReturnVisibleScopeCounters_WhenUserIsNotAdmin()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Id).Returns(UserId);
            _mockCurrentUserService.Setup(x => x.IsInRole(Roles.Administrator)).Returns(false);
            var handler = new GetDashboardSummaryQueryHandler(_dbContext, _mockCurrentUserService.Object);

            // Act
            var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

            // Assert
            result.ProjectsCount.Should().Be(2);
            result.AssignedTasksCount.Should().Be(5);
            result.TasksClosedThisWeekCount.Should().Be(2);
            result.OverdueAssignedTasksCount.Should().Be(1);
        }

        [Fact]
        public async Task Handle_ShouldReturnFullScopeCounters_WhenUserIsAdmin()
        {
            // Arrange
            _mockCurrentUserService.Setup(x => x.Id).Returns(UserId);
            _mockCurrentUserService.Setup(x => x.IsInRole(Roles.Administrator)).Returns(true);
            var handler = new GetDashboardSummaryQueryHandler(_dbContext, _mockCurrentUserService.Object);

            // Act
            var result = await handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

            // Assert
            result.ProjectsCount.Should().Be(3);
            result.AssignedTasksCount.Should().Be(7);
            result.TasksClosedThisWeekCount.Should().Be(3);
            result.OverdueAssignedTasksCount.Should().Be(2);
        }

        private void SeedDatabase()
        {
            var now = DateTime.UtcNow;

            var ownedProject = new Project
            {
                Id = _ownedProjectId,
                Name = "Owned Project",
                Description = "Owned",
                OwnerUserId = UserId
            };

            var memberProject = new Project
            {
                Id = _memberProjectId,
                Name = "Member Project",
                Description = "Member",
                OwnerUserId = OtherUserId
            };

            memberProject.Members.Add(new ProjectMember
            {
                ProjectId = _memberProjectId,
                UserId = UserId,
                AddedByUserId = OtherUserId,
                JoinedAt = now
            });

            var unrelatedProject = new Project
            {
                Id = _unrelatedProjectId,
                Name = "Unrelated Project",
                Description = "Unrelated",
                OwnerUserId = OtherUserId
            };

            _dbContext.Projects.AddRange(ownedProject, memberProject, unrelatedProject);

            _dbContext.TaskItems.AddRange(
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Owned - Todo",
                    Status = TaskItemStatus.Todo,
                    ProjectId = _ownedProjectId,
                    AssignedUserId = UserId,
                    DueDate = now.AddDays(3),
                    LastModifiedAt = now.AddDays(-2)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Owned - Done This Week",
                    Status = TaskItemStatus.Done,
                    ProjectId = _ownedProjectId,
                    AssignedUserId = UserId,
                    LastModifiedAt = now.AddDays(-1)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Owned - Done Last Week",
                    Status = TaskItemStatus.Done,
                    ProjectId = _ownedProjectId,
                    AssignedUserId = UserId,
                    LastModifiedAt = now.AddDays(-8)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Owned - Overdue",
                    Status = TaskItemStatus.InProgress,
                    ProjectId = _ownedProjectId,
                    AssignedUserId = UserId,
                    DueDate = now.AddDays(-2),
                    LastModifiedAt = now.AddDays(-1)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Member - Done This Week",
                    Status = TaskItemStatus.Done,
                    ProjectId = _memberProjectId,
                    AssignedUserId = UserId,
                    LastModifiedAt = now.AddDays(-2)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Unrelated - Done This Week",
                    Status = TaskItemStatus.Done,
                    ProjectId = _unrelatedProjectId,
                    AssignedUserId = UserId,
                    LastModifiedAt = now.AddDays(-1)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Unrelated - Overdue",
                    Status = TaskItemStatus.Todo,
                    ProjectId = _unrelatedProjectId,
                    AssignedUserId = UserId,
                    DueDate = now.AddDays(-3),
                    LastModifiedAt = now.AddDays(-2)
                },
                new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Owned - Assigned To Other User",
                    Status = TaskItemStatus.InProgress,
                    ProjectId = _ownedProjectId,
                    AssignedUserId = OtherUserId,
                    LastModifiedAt = now.AddDays(-1)
                });

            _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
