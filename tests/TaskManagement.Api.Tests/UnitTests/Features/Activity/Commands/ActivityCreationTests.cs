using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.Activity.Commands
{
    public class ActivityCreationTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IUserDirectoryService> _mockUserDirectory;
        private readonly Mock<IActivityPublisher> _mockActivityPublisher;
        private readonly IMapper _projectMapper;
        private readonly IMapper _taskMapper;

        private readonly string _userId = "user-activity-create";
        private readonly string _userName = "Activity User";

        public ActivityCreationTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_ActivityCreation_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockUserDirectory = new Mock<IUserDirectoryService>();
            _mockActivityPublisher = new Mock<IActivityPublisher>();

            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            _projectMapper = new MapperConfiguration(cfg => cfg.AddProfile<ProjectMappingProfile>()).CreateMapper();
            _taskMapper = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>()).CreateMapper();

            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.UserName).Returns(_userName);

            _mockUserDirectory
                .Setup(s => s.UserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockActivityPublisher
                .Setup(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
        }

        [Fact]
        public async Task CreateProject_ShouldCreateActivityLog()
        {
            // Arrange
            var handler = new CreateProjectCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object,
                _projectMapper);

            var command = new CreateProjectCommand { Name = "Activity Project", Description = "desc" };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var activity = await _dbContext.ActivityLogs.FirstOrDefaultAsync();
            activity.Should().NotBeNull();
            activity!.Type.Should().Be(ActivityType.ProjectCreated);
            activity.ProjectId.Should().Be(result.Id);
            activity.ProjectName.Should().Be(command.Name);

            _mockActivityPublisher.Verify(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task CreateTask_ShouldCreateActivityLog()
        {
            // Arrange
            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Activity Project",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _userId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _userId
            };
            _dbContext.Projects.Add(project);
            _dbContext.SaveChanges();

            var handler = new CreateTaskItemCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object,
                _taskMapper,
                _mockUserDirectory.Object);

            var command = new CreateTaskItemCommand
            {
                ProjectId = project.Id,
                Title = "Activity Task",
                Status = TaskStatus.Todo
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var activity = await _dbContext.ActivityLogs.FirstOrDefaultAsync(a => a.TaskItemId == result.Id);
            activity.Should().NotBeNull();
            activity!.Type.Should().Be(ActivityType.TaskCreated);
            activity.ProjectId.Should().Be(project.Id);
            activity.TaskTitle.Should().Be(command.Title);

            _mockActivityPublisher.Verify(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateTaskStatus_ShouldCreateActivityLogWithOldAndNewStatus()
        {
            // Arrange
            var projectId = Guid.NewGuid();
            var project = new Project
            {
                Id = projectId,
                Name = "Activity Project",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _userId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _userId,
                Members = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        ProjectId = projectId,
                        UserId = _userId,
                        JoinedAt = DateTime.UtcNow,
                        AddedByUserId = _userId
                    }
                }
            };

            var task = new TaskItem
            {
                Id = Guid.NewGuid(),
                Title = "Status Task",
                ProjectId = projectId,
                Project = project,
                Status = TaskStatus.Todo,
                AssignedUserId = _userId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _userId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _userId
            };

            _dbContext.Projects.Add(project);
            _dbContext.TaskItems.Add(task);
            _dbContext.SaveChanges();

            var handler = new UpdateTaskItemCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object,
                _taskMapper,
                _mockUserDirectory.Object);

            var command = new UpdateTaskItemCommand
            {
                Id = task.Id,
                Title = task.Title,
                Status = TaskStatus.InProgress
            };

            // Act
            var result = await handler.Handle(command, CancellationToken.None);

            // Assert
            var activity = await _dbContext.ActivityLogs.FirstOrDefaultAsync(a => a.TaskItemId == result.Id && a.Type == ActivityType.TaskStatusChanged);
            activity.Should().NotBeNull();
            activity!.OldStatus.Should().Be(TaskStatus.Todo);
            activity.NewStatus.Should().Be(TaskStatus.InProgress);

            _mockActivityPublisher.Verify(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
