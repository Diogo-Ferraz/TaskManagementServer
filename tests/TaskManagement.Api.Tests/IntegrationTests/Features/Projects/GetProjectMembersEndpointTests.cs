using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    public class GetProjectMembersEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _ownerUserId = "owner-members-endpoint";
        private readonly string _memberUserId = "member-members-endpoint";
        private readonly string _unrelatedUserId = "unrelated-members-endpoint";

        public GetProjectMembersEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(db =>
            {
                var project = new Project
                {
                    Id = _projectId,
                    Name = "Members Project",
                    OwnerUserId = _ownerUserId
                };

                project.Members.Add(new ProjectMember
                {
                    ProjectId = _projectId,
                    UserId = _memberUserId,
                    AddedByUserId = _ownerUserId,
                    JoinedAt = DateTime.UtcNow
                });

                db.Projects.Add(project);
                return Task.CompletedTask;
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetMembers_WhenUserIsMember_ShouldReturnDisplayNames()
        {
            // Arrange
            SetAuthenticatedUser(_memberUserId, Roles.User);

            // Act
            var response = await _client.GetAsync($"/api/projects/{_projectId}/members");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var members = await response.Content.ReadFromJsonAsync<List<ProjectMemberDto>>();
            members.Should().NotBeNull();
            members.Should().HaveCount(2);
            members.Should().Contain(m => m.UserId == _ownerUserId && m.IsOwner && m.DisplayName == $"Test User {_ownerUserId}");
            members.Should().Contain(m => m.UserId == _memberUserId && !m.IsOwner && m.DisplayName == $"Test User {_memberUserId}");
        }

        [Fact]
        public async Task GetMembers_WhenUserIsNotMember_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.User);

            // Act
            var response = await _client.GetAsync($"/api/projects/{_projectId}/members");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private void SetAuthenticatedUser(string userId, string roles)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserRolesHeader);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserRolesHeader, roles);
        }
    }
}
