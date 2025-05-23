using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Projects
{
    [Trait("Category", "Integration")]
    public class CreateProjectEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        private const string TestUserId = "test-user-create-project";
        private const string TestUserName = "Test User Create Project";

        public CreateProjectEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();

            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, TestUserId);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserNameHeader, TestUserName);
        }

        public async Task InitializeAsync()
        {
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task CreateProject_WithValidDataAndAuthenticatedUser_ShouldReturnCreatedAndProjectDto()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                Name = "Integration Test Project One",
                Description = "A project created via integration test."
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/projects", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            response.Headers.Location.Should().NotBeNull();

            var createdProjectDto = await response.Content.ReadFromJsonAsync<ProjectDto>();
            createdProjectDto.Should().NotBeNull();
            createdProjectDto!.Id.Should().NotBeEmpty();
            createdProjectDto.Name.Should().Be(command.Name);
            createdProjectDto.Description.Should().Be(command.Description);
            createdProjectDto.OwnerUserId.Should().Be(TestUserId);
            createdProjectDto.CreatedByUserId.Should().Be(TestUserId);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var projectInDb = await dbContext.Projects
                                            .Include(p => p.Members)
                                            .FirstOrDefaultAsync(p => p.Id == createdProjectDto.Id);

                projectInDb.Should().NotBeNull();
                projectInDb!.Name.Should().Be(command.Name);
                projectInDb.OwnerUserId.Should().Be(TestUserId);
                projectInDb.CreatedByUserId.Should().Be(TestUserId);
                projectInDb.Members.Should().ContainSingle(m => m.UserId == TestUserId && m.ProjectId == projectInDb.Id);
            }
        }

        [Fact]
        public async Task CreateProject_WithMissingName_ShouldReturnBadRequest()
        {
            // Arrange
            var command = new CreateProjectCommand
            {
                Name = "",
                Description = "Attempt to create project with no name."
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/projects", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Title.Should().Be("Validation Error");
            problemDetails.Errors.Should().ContainKey(nameof(CreateProjectCommand.Name))
                .WhoseValue.Should().Contain("Name is required");
        }

        [Fact]
        public async Task CreateProject_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var command = new CreateProjectCommand { Name = "Unauthenticated Project Attempt" };
            var unauthenticatedClient = _factory.CreateClient();

            // Act
            var response = await unauthenticatedClient.PostAsJsonAsync("/api/projects", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}