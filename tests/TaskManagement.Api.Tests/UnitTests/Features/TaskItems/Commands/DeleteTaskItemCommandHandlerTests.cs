using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Commands.Handlers;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class DeleteTaskItemCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly DeleteTaskItemCommandHandler _handler;

        private readonly Guid _taskIdToDelete = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _projectOwnerId = "project-owner-123";
        private readonly string _otherUserId = "other-user-789";

        public DeleteTaskItemCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_DeleteTaskItem_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            SeedDatabase();

            _handler = new DeleteTaskItemCommandHandler(_dbContext, _mockCurrentUser.Object);
        }

        private void SeedDatabase()
        {
            var project = new Project { Id = _projectId, Name = "Project For Task Deletion", OwnerUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, CreatedByUserId = _projectOwnerId, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId };
            var task = new TaskItem { Id = _taskIdToDelete, Title = "Task to Delete", ProjectId = _projectId, Project = project, AssignedUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, CreatedByUserId = _projectOwnerId, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId };
            _dbContext.Projects.Add(project);
            _dbContext.TaskItems.Add(task);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldDeleteTaskItem_WhenUserIsProjectOwner()
        {
            // Arrange
            var command = new DeleteTaskItemCommand { Id = _taskIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);
            int initialTaskCount = await _dbContext.TaskItems.CountAsync();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
            var deletedTask = await _dbContext.TaskItems.FindAsync(_taskIdToDelete);
            deletedTask.Should().BeNull();
            (await _dbContext.TaskItems.CountAsync()).Should().Be(initialTaskCount - 1);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenTaskItemDoesNotExist()
        {
            // Arrange
            var command = new DeleteTaskItemCommand { Id = Guid.NewGuid() };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotProjectOwner()
        {
            // Arrange
            var command = new DeleteTaskItemCommand { Id = _taskIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            int initialTaskCount = await _dbContext.TaskItems.CountAsync();

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
            (await _dbContext.TaskItems.CountAsync()).Should().Be(initialTaskCount);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new DeleteTaskItemCommand { Id = _taskIdToDelete };
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