using AutoMapper;
using FluentAssertions; // For assertions
using Microsoft.EntityFrameworkCore; // For InMemory specifics
using Moq;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Projects.Queries.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Queries
{
    public class GetProjectsForUserQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetProjectsForUserQueryHandler _handler;

        private readonly string _testUserId = "user-123";
        private readonly Guid _ownedProjectId = Guid.NewGuid();
        private readonly Guid _memberProjectId = Guid.NewGuid();
        private readonly Guid _otherProjectId = Guid.NewGuid();

        public GetProjectsForUserQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetProjectsForUser_{Guid.NewGuid()}")
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

            _handler = new GetProjectsForUserQueryHandler(_dbContext, _mockCurrentUser.Object, _mapper);
        }

        private void SeedDatabase()
        {
            var projects = new List<Project>
            {
                new Project { Id = _ownedProjectId, Name = "A Owned Project", OwnerUserId = _testUserId, CreatedAt = DateTime.UtcNow },
                new Project { Id = _memberProjectId, Name = "B Member Project", OwnerUserId = "other-owner", CreatedAt = DateTime.UtcNow.AddMinutes(1),
                              Members = new List<ProjectMember> { new ProjectMember { UserId = _testUserId, ProjectId = _memberProjectId } } },
                new Project { Id = _otherProjectId, Name = "C Other Project", OwnerUserId = "other-owner", CreatedAt = DateTime.UtcNow.AddMinutes(2) }
            };
            _dbContext.Projects.AddRange(projects);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnOwnedAndMemberProjects_WhenUserIsAuthenticated()
        {
            // Arrange
            var query = new GetProjectsForUserQuery();
            _mockCurrentUser.Setup(u => u.Id).Returns(_testUserId);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);

            result.Select(p => p.Id).Should().ContainInOrder(_ownedProjectId, _memberProjectId);
            result.First(p => p.Id == _ownedProjectId).Name.Should().Be("A Owned Project");
            result.First(p => p.Id == _memberProjectId).Name.Should().Be("B Member Project");

            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldReturnEmptyList_WhenUserHasNoProjects()
        {
            // Arrange
            var query = new GetProjectsForUserQuery();
            var newUser = "new-user-id";
            _mockCurrentUser.Setup(u => u.Id).Returns(newUser);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        [Fact]
        public async Task Handle_ShouldThrowUnauthorizedAccessException_WhenUserIsNotAuthenticated()
        {
            // Arrange
            var query = new GetProjectsForUserQuery();
            _mockCurrentUser.Setup(u => u.Id).Returns((string?)null);

            // Act
            Func<Task> act = async () => await _handler.Handle(query, CancellationToken.None);

            // Assert
            await act.Should().ThrowAsync<UnauthorizedAccessException>();
            _mockCurrentUser.Verify(u => u.Id, Times.Once);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}