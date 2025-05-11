using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class UpdateTaskItemCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly UpdateTaskItemCommandHandler _handler;

        private readonly Guid _taskIdToUpdate = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _projectOwnerId = "project-owner-123";
        private readonly string _taskAssigneeId = "task-assignee-456";
        private readonly string _otherUserId = "other-user-789";
        private TaskItem _initialTaskState;

        public UpdateTaskItemCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_UpdateTaskItem_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _initialTaskState = SeedDatabase();

            _handler = new UpdateTaskItemCommandHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private TaskItem SeedDatabase()
        {
            var project = new Project { Id = _projectId, Name = "Project For Tasks", OwnerUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, CreatedByUserId = _projectOwnerId, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId };
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

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}