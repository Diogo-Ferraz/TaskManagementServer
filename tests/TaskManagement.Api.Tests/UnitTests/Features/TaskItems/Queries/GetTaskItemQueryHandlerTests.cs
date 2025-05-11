using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Queries;
using TaskManagement.Api.Features.TaskItems.Queries.Handlers;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.UnitTests.Features.TaskItems.Queries
{
    public class GetTaskItemQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetTaskItemQueryHandler _handler;

        private readonly Guid _existingTaskId = Guid.NewGuid();
        private readonly Guid _otherTaskId = Guid.NewGuid();
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _projectOwnerId = "project-owner-task-123";
        private readonly string _projectMemberId = "project-member-task-456";
        private readonly string _unrelatedUserId = "unrelated-user-task-789";

        public GetTaskItemQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetTaskItem_{Guid.NewGuid()}")
                .Options;
            _dbContext = new TaskManagementDbContext(options, null);

            var mappingConfig = new MapperConfiguration(cfg => cfg.AddProfile<TaskItemMappingProfile>());
            _mapper = mappingConfig.CreateMapper();

            _mockCurrentUser = new Mock<ICurrentUserService>();

            SeedDatabase();

            _handler = new GetTaskItemQueryHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private void SeedDatabase()
        {
            var project = new Project
            {
                Id = _projectId,
                Name = "Project With Tasks",
                OwnerUserId = _projectOwnerId,
                Members = new List<ProjectMember> { new ProjectMember { UserId = _projectMemberId, ProjectId = _projectId } },
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _projectOwnerId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _projectOwnerId
            };
            var tasks = new List<TaskItem>
            {
                new TaskItem { Id = _existingTaskId, Title = "Visible Task", ProjectId = _projectId, Project = project, AssignedUserId = _projectMemberId, CreatedByUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId, Status = TaskStatus.InProgress },
                new TaskItem { Id = _otherTaskId, Title = "Another Task", ProjectId = _projectId, Project = project, AssignedUserId = _projectOwnerId, CreatedByUserId = _projectOwnerId, CreatedAt = DateTime.UtcNow, LastModifiedAt = DateTime.UtcNow, LastModifiedByUserId = _projectOwnerId, Status = TaskStatus.Todo }
            };
            _dbContext.Projects.Add(project);
            _dbContext.TaskItems.AddRange(tasks);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnTaskItemDto_WhenTaskExistsAndUserIsProjectOwner()
        {
            // Arrange
            var query = new GetTaskItemQuery { Id = _existingTaskId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);
            var expectedDto = new TaskItemDto { Id = _existingTaskId, Title = "Visible Task", ProjectId = _projectId, AssignedUserId = _projectMemberId, Status = TaskStatus.InProgress };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto, options => options
                .Excluding(dto => dto.CreatedAt)
                .Excluding(dto => dto.LastModifiedAt)
                .Excluding(dto => dto.ProjectName)
                .Excluding(dto => dto.CreatedByUserId)
                .Excluding(dto => dto.LastModifiedByUserId));
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnTaskItemDto_WhenTaskExistsAndUserIsProjectMember()
        {
            // Arrange
            var query = new GetTaskItemQuery { Id = _existingTaskId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectMemberId);
            var expectedDto = new TaskItemDto { Id = _existingTaskId, Title = "Visible Task", ProjectId = _projectId, AssignedUserId = _projectMemberId, Status = TaskStatus.InProgress };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto, options => options
                .Excluding(dto => dto.CreatedAt)
                .Excluding(dto => dto.LastModifiedAt)
                .Excluding(dto => dto.ProjectName)
                .Excluding(dto => dto.CreatedByUserId)
                .Excluding(dto => dto.LastModifiedByUserId));
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenTaskItemDoesNotExist()
        {
            // Arrange
            var query = new GetTaskItemQuery { Id = Guid.NewGuid() };
            _mockCurrentUser.Setup(u => u.Id).Returns(_projectOwnerId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenUserIsNotProjectOwnerOrMember()
        {
            // Arrange
            var query = new GetTaskItemQuery { Id = _existingTaskId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_unrelatedUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var query = new GetTaskItemQuery { Id = _existingTaskId };
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        public void Dispose()
        {
            _dbContext?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}