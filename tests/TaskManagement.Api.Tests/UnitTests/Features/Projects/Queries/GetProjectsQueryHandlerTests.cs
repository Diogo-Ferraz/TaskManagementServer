using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Projects.Queries.Handlers;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Queries
{
    public class GetProjectsQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly GetProjectsQueryHandler _handler;

        private readonly string _userId = "user-projects-query";
        private readonly Guid _ownedProjectId = Guid.NewGuid();
        private readonly Guid _memberProjectId = Guid.NewGuid();
        private readonly Guid _otherProjectId = Guid.NewGuid();

        public GetProjectsQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_GetProjects_{Guid.NewGuid()}")
                .Options;
            _mockCurrentUser = new Mock<ICurrentUserService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            var mapper = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile<ProjectMappingProfile>();
                cfg.AddProfile<TaskItemMappingProfile>();
            }).CreateMapper();
            SeedDatabase();

            _handler = new GetProjectsQueryHandler(_dbContext, _mockCurrentUser.Object, mapper);
        }

        private void SeedDatabase()
        {
            var ownedProject = new Project
            {
                Id = _ownedProjectId,
                Name = "A Owned Project",
                OwnerUserId = _userId,
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = _userId,
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = _userId
            };
            var memberProject = new Project
            {
                Id = _memberProjectId,
                Name = "B Member Project",
                OwnerUserId = "other-owner",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "other-owner",
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = "other-owner"
            };
            memberProject.Members.Add(new Infrastructure.Persistence.Models.ProjectMember
            {
                ProjectId = _memberProjectId,
                UserId = _userId,
                JoinedAt = DateTime.UtcNow,
                AddedByUserId = "other-owner"
            });

            var otherProject = new Project
            {
                Id = _otherProjectId,
                Name = "C Other Project",
                OwnerUserId = "other-owner",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = "other-owner",
                LastModifiedAt = DateTime.UtcNow,
                LastModifiedByUserId = "other-owner"
            };

            _dbContext.Projects.AddRange(ownedProject, memberProject, otherProject);
            _dbContext.SaveChanges();
        }

        [Fact]
        public async Task Handle_ShouldReturnVisibleProjects_ForNonAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(false);

            var result = await _handler.Handle(new GetProjectsQuery(), CancellationToken.None);

            result.Should().HaveCount(2);
            result.Select(p => p.Id).Should().BeEquivalentTo(new[] { _ownedProjectId, _memberProjectId });
        }

        [Fact]
        public async Task Handle_ShouldReturnAllProjects_ForAdmin()
        {
            _mockCurrentUser.Setup(u => u.Id).Returns(_userId);
            _mockCurrentUser.Setup(u => u.IsInRole(Roles.Administrator)).Returns(true);

            var result = await _handler.Handle(new GetProjectsQuery(), CancellationToken.None);

            result.Should().HaveCount(3);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
