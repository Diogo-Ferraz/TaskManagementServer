using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Commands.Handlers;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Commands
{
    public class UpdateProjectCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly UpdateProjectCommandHandler _handler;

        private readonly Guid _projectIdToUpdate = Guid.NewGuid();
        private readonly string _ownerUserId = "user-owner-123";
        private readonly string _otherUserId = "user-other-789";
        private readonly Project _initialProjectState;

        public UpdateProjectCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_UpdateProject_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<ProjectMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _initialProjectState = SeedDatabase();

            _handler = new UpdateProjectCommandHandler(
                _dbContext,
                _mockCurrentUser.Object,
                _mapper);
        }

        private Project SeedDatabase()
        {
            var project = new Project
            {
                Id = _projectIdToUpdate,
                Name = "Original Name",
                Description = "Original Desc",
                OwnerUserId = _ownerUserId,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CreatedByUserId = _ownerUserId,
                LastModifiedAt = DateTime.UtcNow.AddDays(-1),
                LastModifiedByUserId = _ownerUserId
            };
            _dbContext.Projects.Add(project);
            _dbContext.SaveChanges();
            return project;
        }

        [Fact]
        public async Task Handle_ShouldUpdateProject_WhenRequestIsValidAndUserIsOwner()
        {
            // Arrange
            var command = new UpdateProjectCommand
            {
                Id = _projectIdToUpdate,
                Name = "Updated Name",
                Description = "Updated Desc"
            };
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);

            var originalLastModifiedAt = _initialProjectState.LastModifiedAt;

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Id.Should().Be(_projectIdToUpdate);
            resultDto.Name.Should().Be(command.Name);
            resultDto.Description.Should().Be(command.Description);

            var updatedProject = await _dbContext.Projects.FindAsync(_projectIdToUpdate);
            updatedProject.Should().NotBeNull();
            updatedProject!.Name.Should().Be(command.Name);
            updatedProject.Description.Should().Be(command.Description);
            updatedProject.LastModifiedAt.Should().BeAfter(originalLastModifiedAt);
            updatedProject.LastModifiedByUserId.Should().Be(_ownerUserId);

            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            var command = new UpdateProjectCommand { Id = Guid.NewGuid(), Name = "Updated Name" };
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
            var command = new UpdateProjectCommand { Id = _projectIdToUpdate, Name = "Updated Name" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_otherUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);

            var project = await _dbContext.Projects.FindAsync(_projectIdToUpdate);
            project!.Name.Should().Be(_initialProjectState.Name);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new UpdateProjectCommand { Id = _projectIdToUpdate, Name = "Updated Name" };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
        }

        // Optional: Test case for Administrator override if implemented in handler
        // [Fact]
        // public async Task Handle_ShouldUpdateProject_WhenUserIsAdministratorButNotOwner() { ... }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}