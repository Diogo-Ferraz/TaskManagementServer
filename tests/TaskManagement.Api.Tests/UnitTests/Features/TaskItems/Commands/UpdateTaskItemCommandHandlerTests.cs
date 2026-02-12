using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class UpdateTaskItemCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IUserDirectoryService> _mockUserDirectory;
        private readonly Mock<IActivityPublisher> _mockActivityPublisher;
        private readonly UpdateTaskItemCommandHandler _handler;

        private readonly Guid _taskIdToUpdate = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _projectOwnerId = "project-owner-123";
        private readonly string _taskAssigneeId = "task-assignee-456";
        private readonly string _projectMemberId = "project-member-321";
        private readonly string _otherUserId = "other-user-789";
        private TaskItem _initialTaskState;

        public UpdateTaskItemCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_UpdateTaskItem_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockUserDirectory = new Mock<IUserDirectoryService>();
            _mockActivityPublisher = new Mock<IActivityPublisher>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _initialTaskState = SeedDatabase();

            _mockUserDirectory
                .Setup(s => s.UserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockActivityPublisher
                .Setup(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new UpdateTaskItemCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object,
                _mapper,
                _mockUserDirectory.Object);
        }

        private TaskItem SeedDatabase()
        {
            var project = new Project
            {
                Id = _projectId,
                Name = "Project For Tasks",
                OwnerUserId = _projectOwnerId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _projectOwnerId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _projectOwnerId,
                Members = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        ProjectId = _projectId,
                        UserId = _taskAssigneeId,
                        JoinedAt = DateTime.UtcNow,
                        AddedByUserId = _projectOwnerId
                    },
                    new ProjectMember
                    {
                        ProjectId = _projectId,
                        UserId = _projectMemberId,
                        JoinedAt = DateTime.UtcNow,
                        AddedByUserId = _projectOwnerId
                    }
                }
            };
            var task = new TaskItem
            {
                Id = _taskIdToUpdate,
                Title = "Original Task Title",
                Description = "Original Desc",
                ProjectId = _projectId,
                Project = project,
                AssignedUserId = _taskAssigneeId,
                Status = TaskStatus.Todo,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _projectOwnerId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _projectOwnerId
            };
            _dbContext.Projects.Add(project);
            _dbContext.TaskItems.Add(task);
            _dbContext.SaveChanges();
            return task;
        }

        [Fact]
        public async Task Handle_ShouldUpdateTaskItem_WhenUserIsProjectOwner()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Updated by Owner", Status = TaskStatus.InProgress };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            resultDto.Status.Should().Be(command.Status);

            var updatedTask = await _dbContext.TaskItems.FindAsync(_taskIdToUpdate);
            updatedTask!.Title.Should().Be(command.Title);
            updatedTask.Status.Should().Be(command.Status);
            updatedTask.LastModifiedByUserId.Should().Be(_projectOwnerId);
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldUpdateTaskItem_WhenUserIsAssignee()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Updated by Assignee", Status = TaskStatus.Done };
            _mockCurrentUser.Setup(u => u.Id).Returns(_taskAssigneeId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);

            var updatedTask = await _dbContext.TaskItems.FindAsync(_taskIdToUpdate);
            updatedTask!.Title.Should().Be(command.Title);
            updatedTask.LastModifiedByUserId.Should().Be(_taskAssigneeId);
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldUpdateTaskItem_WhenUserIsProjectMember()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Updated by Member", Status = TaskStatus.InProgress };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectMemberId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            resultDto.Status.Should().Be(command.Status);
        }

        [Fact]
        public async Task Handle_ShouldUpdateTaskItem_WhenUserIsAdministrator()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Updated by Admin", Status = TaskStatus.Done };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            resultDto.Status.Should().Be(command.Status);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenTaskItemDoesNotExist()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = Guid.NewGuid(), Title = "NonExistent Task Update" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotOwnerOrAssignee()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Forbidden Update Attempt" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
            var task = await _dbContext.TaskItems.FindAsync(_taskIdToUpdate);
            task!.Title.Should().Be(_initialTaskState.Title);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Unauth Update" };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenAssignedUserDoesNotExist()
        {
            // Arrange
            var command = new UpdateTaskItemCommand
            {
                Id = _taskIdToUpdate,
                Title = "Task With Invalid Assignee",
                AssignedUserId = "invalid-user"
            };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);
            _mockUserDirectory
                .Setup(s => s.UserExistsAsync(command.AssignedUserId!, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            var ex = await act.Should().ThrowAsync<ValidationException>();
            ex.Which.Errors.Should().ContainKey(nameof(UpdateTaskItemCommand.AssignedUserId));
            ex.Which.Errors[nameof(UpdateTaskItemCommand.AssignedUserId)]
                .Should().Contain(x => x.Contains("existing user", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Handle_ShouldAutoAddMember_WhenAssignedUserIsNotProjectMember()
        {
            // Arrange
            var newAssigneeId = "new-project-member-999";
            var command = new UpdateTaskItemCommand
            {
                Id = _taskIdToUpdate,
                Title = "Task With New Member",
                AssignedUserId = newAssigneeId
            };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);
            _mockUserDirectory
                .Setup(s => s.UserExistsAsync(newAssigneeId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.AssignedUserId.Should().Be(newAssigneeId);
            var memberExists = await _dbContext.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == _projectId && pm.UserId == newAssigneeId);
            memberExists.Should().BeTrue();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
