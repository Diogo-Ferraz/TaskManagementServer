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
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Commands
{
    public class CreateTaskItemCommandHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IUserDirectoryService> _mockUserDirectory;
        private readonly Mock<IActivityPublisher> _mockActivityPublisher;
        private readonly CreateTaskItemCommandHandler _handler;

        private readonly string _projectOwnerId = "project-owner-123";
        private readonly string _projectMemberId = "project-member-456";
        private readonly string _unrelatedUserId = "unrelated-user-789";
        private readonly Guid _existingProjectId = Guid.NewGuid();
        private readonly Guid _nonExistentProjectId = Guid.NewGuid();

        public CreateTaskItemCommandHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_CreateTaskItem_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockUserDirectory = new Mock<IUserDirectoryService>();
            _mockActivityPublisher = new Mock<IActivityPublisher>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mappingConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<TaskItemMappingProfile>();
            });
            _mapper = mappingConfig.CreateMapper();

            SeedDatabase();

            _mockUserDirectory
                .Setup(s => s.UserExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mockActivityPublisher
                .Setup(p => p.PublishAsync(It.IsAny<ActivityLog>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            _handler = new CreateTaskItemCommandHandler(
                _dbContext,
                _mockActivityPublisher.Object,
                _mockCurrentUser.Object,
                _mapper,
                _mockUserDirectory.Object);
        }

        private void SeedDatabase()
        {
            _dbContext.Projects.Add(new Project
            {
                Id = _existingProjectId,
                Name = "Test Project",
                OwnerUserId = _projectOwnerId,
                Members = new List<ProjectMember>
                {
                    new ProjectMember
                    {
                        UserId = _projectMemberId,
                        ProjectId = _existingProjectId,
                        JoinedAt = DateTime.UtcNow,
                        AddedByUserId = _projectOwnerId
                    }
                },
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _projectOwnerId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _projectOwnerId
            });
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldCreateTaskItem_WhenUserIsProjectOwner()
        {
            // Arrange
            var command = new CreateTaskItemCommand
            {
                ProjectId = _existingProjectId,
                Title = "New Task by Owner",
                Status = TaskStatus.Todo
            };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            resultDto.ProjectId.Should().Be(_existingProjectId);
            resultDto.Id.Should().NotBeEmpty();

            var createdTask = await _dbContext.TaskItems.FindAsync(resultDto.Id);
            createdTask.Should().NotBeNull();
            createdTask!.Title.Should().Be(command.Title);
            createdTask.ProjectId.Should().Be(_existingProjectId);
            createdTask.CreatedByUserId.Should().Be(_projectOwnerId);
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldCreateTaskItem_WhenUserIsProjectMember()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _existingProjectId, Title = "New Task by Member", Status = TaskStatus.Todo };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectMemberId);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            var createdTask = await _dbContext.TaskItems.FindAsync(resultDto.Id);
            createdTask.Should().NotBeNull();
            createdTask!.CreatedByUserId.Should().Be(_projectMemberId);
            _mockCurrentUser.Verify(u => u.Id, Times.Exactly(2));
        }

        [Fact]
        public async Task Handle_ShouldCreateTaskItem_WhenUserIsAdministrator()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _existingProjectId, Title = "New Task by Admin", Status = TaskStatus.Todo };
            _mockCurrentUser.Setup(u => u.Id).Returns(_unrelatedUserId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            // Act
            var resultDto = await _handler.Handle(command, CancellationToken.None);

            // Assert
            resultDto.Should().NotBeNull();
            resultDto.Title.Should().Be(command.Title);
            resultDto.CreatedByUserId.Should().Be(_unrelatedUserId);
        }


        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _nonExistentProjectId, Title = "Task for NonExistent Project" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage($"*Project with ID {_nonExistentProjectId} not found*");
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowForbiddenAccessException_WhenUserIsNotMemberOrOwnerOfProject()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _existingProjectId, Title = "Unauthorized Task Attempt" };
            _mockCurrentUser.Setup(u => u.Id).Returns(_unrelatedUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<ForbiddenAccessException>()
                     .WithMessage("*not authorized to add tasks to this project*");
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _existingProjectId, Title = "Task by Unauth User" };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(command, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowValidationException_WhenAssignedUserDoesNotExist()
        {
            // Arrange
            var command = new CreateTaskItemCommand
            {
                ProjectId = _existingProjectId,
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
            ex.Which.Errors.Should().ContainKey(nameof(CreateTaskItemCommand.AssignedUserId));
            ex.Which.Errors[nameof(CreateTaskItemCommand.AssignedUserId)]
                .Should().Contain(x => x.Contains("existing user", StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task Handle_ShouldAutoAddMember_WhenAssignedUserIsNotProjectMember()
        {
            // Arrange
            var newAssigneeId = "new-project-member-999";
            var command = new CreateTaskItemCommand
            {
                ProjectId = _existingProjectId,
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
                .AnyAsync(pm => pm.ProjectId == _existingProjectId && pm.UserId == newAssigneeId);
            memberExists.Should().BeTrue();
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
