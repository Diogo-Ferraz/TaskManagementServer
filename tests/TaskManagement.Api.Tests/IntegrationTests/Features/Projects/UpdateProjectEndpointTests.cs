using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    public class UpdateProjectEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client;

        // Test Data
        private readonly Guid _projectToUpdateId = Guid.NewGuid();
        private readonly Guid _projectOwnedByOtherUserId = Guid.NewGuid();
        private readonly Guid _nonExistentProjectId = Guid.NewGuid();

        private readonly string _ownerUserId = "user-project-owner-update";
        private readonly string _otherUserId = "user-other-update";

        private const string InitialProjectName = "Initial Project Name";
        private const string InitialProjectDescription = "Initial project description.";

        public UpdateProjectEndpointTests(ApiWebApplicationFactory<Program> factory)
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
                    Id = _projectToUpdateId,
                    Name = InitialProjectName,
                    Description = InitialProjectDescription,
                    OwnerUserId = _ownerUserId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _ownerUserId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _ownerUserId
                };
                var project2 = new Project
                {
                    Id = _projectOwnedByOtherUserId,
                    Name = "Other User's Project",
                    Description = "Belongs to someone else.",
                    OwnerUserId = _otherUserId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _otherUserId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _otherUserId
                };
                db.Projects.AddRange(project1, project2);
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private void SetAuthenticatedUser(string userId, string? roles = null)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserRolesHeader);

            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
            if (!string.IsNullOrEmpty(roles))
            {
                _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserRolesHeader, roles);
            }
        }

        [Fact]
        public async Task UpdateProject_WhenUserIsOwnerAndDataIsValid_ShouldReturnOkAndUpdatedDto()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);
            var command = new UpdateProjectCommand
            {
                Id = _projectToUpdateId,
                Name = "Updated Project Name",
                Description = "Updated project description."
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/projects/{_projectToUpdateId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedDto = await response.Content.ReadFromJsonAsync<ProjectDto>();
            updatedDto.Should().NotBeNull();
            updatedDto!.Id.Should().Be(_projectToUpdateId);
            updatedDto.Name.Should().Be(command.Name);
            updatedDto.Description.Should().Be(command.Description);
            updatedDto.OwnerUserId.Should().Be(_ownerUserId);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects.FindAsync(_projectToUpdateId);
                projectInDb.Should().NotBeNull();
                projectInDb!.Name.Should().Be(command.Name);
                projectInDb.Description.Should().Be(command.Description);
                projectInDb.LastModifiedByUserId.Should().Be(_ownerUserId);
                projectInDb.LastModifiedAt.Should().BeAfter(DateTime.UtcNow.AddMinutes(-1));
            }
        }

        [Fact]
        public async Task UpdateProject_WhenUserIsNotOwner_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_otherUserId);
            var command = new UpdateProjectCommand
            {
                Id = _projectToUpdateId,
                Name = "Attempted Update By Non-Owner",
                Description = "This should fail."
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/projects/{_projectToUpdateId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            // Assert
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects.FindAsync(_projectToUpdateId);
                projectInDb!.Name.Should().Be(InitialProjectName);
            }
        }

        [Fact]
        public async Task UpdateProject_WithMismatchedRouteIdAndCommandId_ShouldReturnBadRequest()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);
            var differentIdInCommand = Guid.NewGuid();
            var command = new UpdateProjectCommand
            {
                Id = differentIdInCommand,
                Name = "Mismatched ID Project",
                Description = "This should be a bad request."
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/projects/{_projectToUpdateId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateProject_WhenProjectDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);
            var command = new UpdateProjectCommand
            {
                Id = _nonExistentProjectId,
                Name = "Update NonExistent Project",
                Description = "This should fail."
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/projects/{_nonExistentProjectId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateProject_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            SetAuthenticatedUser(_ownerUserId);
            var command = new UpdateProjectCommand
            {
                Id = _projectToUpdateId,
                Name = "",
                Description = "Valid description."
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/projects/{_projectToUpdateId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Validation Error");
            problemDetails.Errors.Should().ContainKey(nameof(UpdateProjectCommand.Name));
        }

        [Fact]
        public async Task UpdateProject_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var command = new UpdateProjectCommand { Id = _projectToUpdateId, Name = "Update Attempt Unauthenticated" };
            var unauthenticatedClient = _factory.CreateClient();

            // Act
            var response = await unauthenticatedClient.PutAsJsonAsync($"/api/projects/{_projectToUpdateId}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
