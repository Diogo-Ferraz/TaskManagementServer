using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Commands
{
    public class DeleteProjectCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IActivityPublisher> _mockActivityPublisher;
        private readonly DeleteProjectCommandHandler _handler;

        // Test Data
        private readonly Guid _projectIdToDelete = Guid.NewGuid();
        private readonly string _ownerUserId = "user-owner-123";
        private readonly string _otherUserId = "user-other-789";

        public DeleteProjectCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_DeleteProject_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockActivityPublisher = new Mock<IActivityPublisher>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            SeedDatabase();

            _mockActivityPublisher
                .Setup(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new DeleteProjectCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object);
        }

        private void SeedDatabase()
        {
            _dbContext.Projects.Add(new Project
            {
                Id = _projectIdToDelete,
                Name = "To Delete",
                OwnerUserId = _ownerUserId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _ownerUserId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _ownerUserId
            });
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldRemoveProject_WhenRequestIsValidAndUserIsOwner()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = _projectIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerUserId);
            int initialCount = await _dbContext.Projects.CountAsync();

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
            var deletedProject = await _dbContext.Projects.FindAsync(_projectIdToDelete);
            deletedProject.Should().BeNull();
            (await _dbContext.Projects.CountAsync()).Should().Be(initialCount - 1);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = Guid.NewGuid() };
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotOwner()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = _projectIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            int initialCount = await _dbContext.Projects.CountAsync();

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
            (await _dbContext.Projects.CountAsync()).Should().Be(initialCount);
            var project = await _dbContext.Projects.FindAsync(_projectIdToDelete);
            project.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_ShouldRemoveProject_WhenUserIsProjectManager()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = _projectIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.ProjectManager)).Returns(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var deletedProject = await _dbContext.Projects.FindAsync(_projectIdToDelete);
            deletedProject.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldRemoveProject_WhenUserIsAdministrator()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = _projectIdToDelete };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            var deletedProject = await _dbContext.Projects.FindAsync(_projectIdToDelete);
            deletedProject.Should().BeNull();
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new DeleteProjectCommand { Id = _projectIdToDelete };
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
