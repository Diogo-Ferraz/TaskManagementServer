using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    public class DeleteProjectEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        // Test Data
        private readonly Guid _projectToDeleteId = Guid.NewGuid();
        private readonly Guid _projectOwnedByOtherUserId = Guid.NewGuid();
        private readonly Guid _nonExistentProjectId = Guid.NewGuid();

        private readonly string _ownerUserId = "user-project-owner-delete";
        private readonly string _otherUserId = "user-other-delete";

        public DeleteProjectEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(db =>
            {
                var project1 = new Project
                {
                    Id = _projectToDeleteId,
                    Name = "Project To Be Deleted",
                    Description = "This project will be removed.",
                    OwnerUserId = _ownerUserId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _ownerUserId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _ownerUserId
                };
                var project2 = new Project
                {
                    Id = _projectOwnedByOtherUserId,
                    Name = "Other User's Project (Delete Test)",
                    Description = "Belongs to someone else.",
                    OwnerUserId = _otherUserId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _otherUserId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _otherUserId
                };
                db.Projects.AddRange(project1, project2);
                return Task.CompletedTask;
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private void SetAuthenticatedUser(string userId, string? roles = null)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserRolesHeader);

            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
            _client.DefaultRequestHeaders.Add(
                TestAuthenticationHandler.TestUserRolesHeader,
                string.IsNullOrWhiteSpace(roles) ? Roles.ProjectManager : roles);
        }

        [Fact]
        public async Task DeleteProject_WhenUserIsOwner_ShouldReturnNoContentAndDeleteProject()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);
            int initialProjectCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialProjectCount = await dbContext.Projects.CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{_projectToDeleteId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects.FindAsync(_projectToDeleteId);
                projectInDb.Should().BeNull();
                (await dbContext.Projects.CountAsync()).Should().Be(initialProjectCount - 1);
                var activityExists = await dbContext.ActivityLogs
                    .AnyAsync(a => a.ProjectId == _projectToDeleteId && a.Type == ActivityType.ProjectDeleted);
                activityExists.Should().BeTrue();
            }
        }

        [Fact]
        public async Task DeleteProject_WhenUserIsProjectManagerButNotOwner_ShouldReturnNoContent()
        {
            // Arrange
            SetAuthenticatedUser(_otherUserId, Roles.ProjectManager);
            int initialProjectCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialProjectCount = await dbContext.Projects.CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{_projectToDeleteId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects.FindAsync(_projectToDeleteId);
                projectInDb.Should().BeNull();
                (await dbContext.Projects.CountAsync()).Should().Be(initialProjectCount - 1);
            }
        }

        [Fact]
        public async Task DeleteProject_WhenUserIsNotOwnerAndNotManager_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_otherUserId, Roles.User);
            int initialProjectCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialProjectCount = await dbContext.Projects.CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{_projectToDeleteId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects.FindAsync(_projectToDeleteId);
                projectInDb.Should().NotBeNull();
                (await dbContext.Projects.CountAsync()).Should().Be(initialProjectCount);
            }
        }

        [Fact]
        public async Task DeleteProject_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);

            // Act
            var response = await _client.DeleteAsync($"/api/projects/{_nonExistentProjectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteProject_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthenticatedClient = _factory.CreateClient();

            // Act
            var response = await unauthenticatedClient.DeleteAsync($"/api/projects/{_projectToDeleteId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
