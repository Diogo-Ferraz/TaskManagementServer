using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Projects.Queries.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Exceptions;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Queries
{
    public class GetProjectQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetProjectQueryHandler _handler;

        private readonly Guid _projectExistsId = Guid.NewGuid();
        private readonly Guid _projectDoesNotExistId = Guid.NewGuid();
        private readonly string _ownerUserId = "user-owner-id-456";
        private readonly string _memberUserId = "user-member-id-789";
        private readonly string _unrelatedUserId = "user-unrelated-id-012";

        public GetProjectQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetProjectById_{Guid.NewGuid()}")
                .Options;

            _dbContext = new TaskManagementDbContext(options, null);

            var mappingConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProjectMappingProfile>();
                cfg.AddProfile<TaskItemMappingProfile>();
            });
            _mapper = mappingConfig.CreateMapper();

            _mockCurrentUser = new Mock<ICurrentUserService>();

            SeedDatabase();

            _handler = new GetProjectQueryHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private void SeedDatabase()
        {
            var project = new Project
            {
                Id = _projectExistsId,
                Name = "Project Visible",
                Description = "Test Description",
                OwnerUserId = _ownerUserId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _ownerUserId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _ownerUserId
            };
            var member = new ProjectMember { ProjectId = _projectExistsId, UserId = _memberUserId };

            _dbContext.Projects.Add(project);
            _dbContext.ProjectMembers.Add(member);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnProjectDto_WhenProjectExistsAndUserIsOwner()
        {
            // Arrange
            var query = new GetProjectQuery { Id = _projectExistsId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerUserId);

            var expectedDto = new ProjectDto { Id = _projectExistsId, Name = "Project Visible", OwnerUserId = _ownerUserId, Description = "Test Description" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto, options => options
                .Excluding(dto => dto.CreatedAt)
                .Excluding(dto => dto.LastModifiedAt)
                .Excluding(dto => dto.CreatedByUserId)
                .Excluding(dto => dto.LastModifiedByUserId)
                .Excluding(dto => dto.TaskItems));
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnProjectDto_WhenProjectExistsAndUserIsMember()
        {
            // Arrange
            var query = new GetProjectQuery { Id = _projectExistsId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_memberUserId);
            var expectedDto = new ProjectDto { Id = _projectExistsId, Name = "Project Visible", OwnerUserId = _ownerUserId, Description = "Test Description" };

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto, options => options
                .Excluding(dto => dto.CreatedAt)
                .Excluding(dto => dto.LastModifiedAt)
                .Excluding(dto => dto.CreatedByUserId)
                .Excluding(dto => dto.LastModifiedByUserId)
                .Excluding(dto => dto.TaskItems));
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenProjectDoesNotExist()
        {
            // Arrange
            var query = new GetProjectQuery { Id = _projectDoesNotExistId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_ownerUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage($"*{nameof(Project)}*key*({_projectDoesNotExistId})*not found*");
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowNotFoundException_WhenUserIsNotOwnerOrMember()
        {
            // Arrange
            var query = new GetProjectQuery { Id = _projectExistsId };
            _mockCurrentUser.Setup(u => u.Id).Returns(_unrelatedUserId);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<NotFoundException>()
                     .WithMessage($"*{nameof(Project)}*key*({_projectExistsId})*not found*");
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var query = new GetProjectQuery { Id = _projectExistsId };
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