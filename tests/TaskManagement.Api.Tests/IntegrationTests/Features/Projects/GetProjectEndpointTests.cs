using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    public class GetProjectEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client;

        private readonly Guid _project1Id = Guid.NewGuid();
        private readonly Guid _project2Id = Guid.NewGuid();
        private readonly Guid _projectUnrelatedId = Guid.NewGuid();
        private readonly string _user1Id = "user-get-project-1";
        private readonly string _user2Id = "user-get-project-2";

        public GetProjectEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();

            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(async db =>
            {
                var project1 = new Project
                {
                    Id = _project1Id,
                    Name = "User1 Project 1",
                    OwnerUserId = _user1Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _user1Id,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _user1Id
                };
                var project2 = new Project
                {
                    Id = _project2Id,
                    Name = "User1 Project 2 (Member)",
                    OwnerUserId = _user2Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _user2Id,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _user2Id
                };
                project2.Members.Add(
                    new ProjectMember
                    {
                        ProjectId = _project2Id,
                        UserId = _user1Id
                    });
                var project3 = new Project
                {
                    Id = _projectUnrelatedId,
                    Name = "Unrelated Project",
                    OwnerUserId = _user2Id,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _user2Id,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _user2Id
                };

                db.Projects.AddRange(project1, project2, project3);
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private void SetAuthenticatedUser(string userId)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
        }

        [Fact]
        public async Task GetProjectById_WhenUserIsOwner_ShouldReturnProject()
        {
            // Arrange
            SetAuthenticatedUser(_user1Id);

            // Act
            var response = await _client.GetAsync($"/api/projects/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projectDto = await response.Content.ReadFromJsonAsync<ProjectDto>();
            projectDto.Should().NotBeNull();
            projectDto!.Id.Should().Be(_project1Id);
            projectDto.Name.Should().Be("User1 Project 1");
            projectDto.OwnerUserId.Should().Be(_user1Id);
        }

        [Fact]
        public async Task GetProjectById_WhenUserIsMember_ShouldReturnProject()
        {
            // Arrange
            SetAuthenticatedUser(_user1Id);

            // Act
            var response = await _client.GetAsync($"/api/projects/{_project2Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projectDto = await response.Content.ReadFromJsonAsync<ProjectDto>();
            projectDto.Should().NotBeNull();
            projectDto!.Id.Should().Be(_project2Id);
            projectDto.OwnerUserId.Should().Be(_user2Id);
        }

        [Fact]
        public async Task GetProjectById_WhenUserIsNotOwnerOrMember_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_user1Id);

            // Act
            var response = await _client.GetAsync($"/api/projects/{_projectUnrelatedId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetProjectById_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_user1Id);
            var nonExistentId = Guid.NewGuid();

            // Act
            var response = await _client.GetAsync($"/api/projects/{nonExistentId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetProjectById_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.GetAsync($"/api/projects/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }


        [Fact]
        public async Task GetMyProjects_ShouldReturnOwnedAndMemberProjects()
        {
            // Arrange
            SetAuthenticatedUser(_user1Id);

            // Act
            var response = await _client.GetAsync("/api/projects/my-projects");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var projects = await response.Content.ReadFromJsonAsync<List<ProjectDto>>();
            projects.Should().NotBeNull();
            projects.Should().HaveCount(2);
            projects.Should().Contain(p => p.Id == _project1Id);
            projects.Should().Contain(p => p.Id == _project2Id);
            projects.Should().NotContain(p => p.Id == _projectUnrelatedId);
        }
    }
}
