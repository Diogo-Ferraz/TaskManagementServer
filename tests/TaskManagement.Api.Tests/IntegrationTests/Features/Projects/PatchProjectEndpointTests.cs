using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    public class PatchProjectEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private readonly Guid _projectId = Guid.NewGuid();
        private readonly string _ownerUserId = "user-project-patch-owner";
        private readonly string _otherUserId = "user-project-patch-other";

        public PatchProjectEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(db =>
            {
                db.Projects.Add(new Project
                {
                    Id = _projectId,
                    Name = "Initial Name",
                    Description = "Initial Description",
                    OwnerUserId = _ownerUserId
                });
                return Task.CompletedTask;
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task PatchProject_WhenUserIsOwner_ShouldUpdateOnlyProvidedFields()
        {
            SetAuthenticatedUser(_ownerUserId, Roles.ProjectManager);
            var command = new PatchProjectCommand { Name = "Patched Name" };

            var response = await _client.PatchAsJsonAsync($"/api/projects/{_projectId}", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<ProjectDto>();
            dto.Should().NotBeNull();
            dto!.Name.Should().Be("Patched Name");
            dto.Description.Should().Be("Initial Description");

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var renameActivity = await db.ActivityLogs
                .FirstOrDefaultAsync(a => a.ProjectId == _projectId && a.Type == ActivityType.ProjectRenamed);
            renameActivity.Should().NotBeNull();
            renameActivity!.OldValue.Should().Be("Initial Name");
            renameActivity.NewValue.Should().Be("Patched Name");
        }

        [Fact]
        public async Task PatchProject_ClearDescription_ShouldSetDescriptionEmpty()
        {
            SetAuthenticatedUser(_ownerUserId, Roles.ProjectManager);
            var command = new PatchProjectCommand { Description = null };

            var response = await _client.PatchAsJsonAsync($"/api/projects/{_projectId}", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var project = await db.Projects.FindAsync(_projectId);
            project.Should().NotBeNull();
            project!.Description.Should().BeEmpty();
        }

        [Fact]
        public async Task PatchProject_WhenUserIsNotAuthorized_ShouldReturnForbidden()
        {
            SetAuthenticatedUser(_otherUserId, Roles.User);
            var command = new PatchProjectCommand { Name = "Not allowed" };

            var response = await _client.PatchAsJsonAsync($"/api/projects/{_projectId}", command);

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
