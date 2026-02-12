using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Queries;
using TaskManagement.Api.Features.Projects.Queries.Handlers;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.UnitTests.Features.Projects.Queries
{
    public class GetProjectMembersQueryHandlerTests : IDisposable
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly Mock<ICurrentUserService> _mockCurrentUser;
        private readonly Mock<IProjectMembershipService> _mockProjectMembershipService;
        private readonly Mock<IUserDirectoryService> _mockUserDirectoryService;

        private readonly Guid _projectId = Guid.NewGuid();
        private const string OwnerUserId = "owner-user";
        private const string MemberUserId = "member-user";

        public GetProjectMembersQueryHandlerTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase($"TestDb_ProjectMembers_{Guid.NewGuid()}")
                .Options;

            _mockCurrentUser = new Mock<ICurrentUserService>();
            _mockProjectMembershipService = new Mock<IProjectMembershipService>();
            _mockUserDirectoryService = new Mock<IUserDirectoryService>();
            _dbContext = new TaskManagementDbContext(options, _mockCurrentUser.Object);

            SeedDatabase();
        }

        [Fact]
        public async Task Handle_ShouldReturnMembersWithDisplayNames()
        {
            // Arrange
            _mockCurrentUser.Setup(x => x.Id).Returns(MemberUserId);
            _mockCurrentUser.Setup(x => x.IsInRole(Roles.Administrator)).Returns(false);
            _mockProjectMembershipService
                .Setup(x => x.IsMemberAsync(_projectId, MemberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
            _mockUserDirectoryService
                .Setup(x => x.GetDisplayNameAsync(OwnerUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Owner Display");
            _mockUserDirectoryService
                .Setup(x => x.GetDisplayNameAsync(MemberUserId, It.IsAny<CancellationToken>()))
                .ReturnsAsync("Member Display");

            var handler = new GetProjectMembersQueryHandler(
                _dbContext,
                _mockCurrentUser.Object,
                _mockProjectMembershipService.Object,
                _mockUserDirectoryService.Object);

            // Act
            var result = await handler.Handle(new GetProjectMembersQuery { ProjectId = _projectId }, CancellationToken.None);

            // Assert
            result.Should().HaveCount(2);
            result[0].UserId.Should().Be(OwnerUserId);
            result[0].DisplayName.Should().Be("Owner Display");
            result[0].IsOwner.Should().BeTrue();

            result.Should().Contain(m => m.UserId == MemberUserId && m.DisplayName == "Member Display" && !m.IsOwner);
        }

        private void SeedDatabase()
        {
            var project = new Project
            {
                Id = _projectId,
                Name = "Project A",
                OwnerUserId = OwnerUserId
            };

            project.Members.Add(new ProjectMember
            {
                ProjectId = _projectId,
                UserId = MemberUserId,
                AddedByUserId = OwnerUserId,
                JoinedAt = DateTime.UtcNow
            });

            _dbContext.Projects.Add(project);
            _dbContext.SaveChanges();
        }

        public void Dispose()
        {
            _dbContext.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
