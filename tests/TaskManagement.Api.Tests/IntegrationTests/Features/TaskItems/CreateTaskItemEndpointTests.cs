using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.TaskItems
{
    public class CreateTaskItemEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client;

        private readonly string _projectOwnerId = "user-task-owner-create";
        private readonly string _projectMemberId = "user-task-member-create";
        private readonly string _unrelatedUserId = "user-task-unrelated-create";

        private readonly Guid _project1Id = Guid.NewGuid();
        private readonly Guid _nonExistentProjectId = Guid.NewGuid();

        public CreateTaskItemEndpointTests(ApiWebApplicationFactory<Program> factory)
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
                    Name = "Project For Task Creation",
                    OwnerUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _projectOwnerId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };
                project1.Members.Add(new ProjectMember { ProjectId = _project1Id, UserId = _projectMemberId });

                db.Projects.Add(project1);
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private void SetAuthenticatedUser(string userId)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
        }

        [Fact]
        public async Task CreateTaskItem_WhenUserIsProjectOwner_ShouldReturnCreatedAndTaskDto()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new CreateTaskItemCommand
            {
                ProjectId = _project1Id,
                Title = "New Task by Owner",
                Description = "Owner creating this task.",
                Status = TaskStatus.InProgress,
                AssignedUserId = _projectMemberId
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/taskitems", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            createdDto.Should().NotBeNull();
            createdDto!.Title.Should().Be(command.Title);
            createdDto.ProjectId.Should().Be(_project1Id);
            createdDto.AssignedUserId.Should().Be(_projectMemberId);
            createdDto.CreatedByUserId.Should().Be(_projectOwnerId);

            // Assert Database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await db.TaskItems.FindAsync(createdDto.Id);
                taskInDb.Should().NotBeNull();
                taskInDb!.Title.Should().Be(command.Title);
                taskInDb.AssignedUserId.Should().Be(_projectMemberId);
                taskInDb.CreatedByUserId.Should().Be(_projectOwnerId);
            }
        }

        [Fact]
        public async Task CreateTaskItem_WhenUserIsProjectMember_ShouldReturnCreatedAndTaskDto()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberId);
            var command = new CreateTaskItemCommand
            {
                ProjectId = _project1Id,
                Title = "New Task by Member",
                Status = TaskStatus.Todo
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/taskitems", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var createdDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            createdDto.Should().NotBeNull();
            createdDto!.Title.Should().Be(command.Title);
            createdDto.ProjectId.Should().Be(_project1Id);
            createdDto.CreatedByUserId.Should().Be(_projectMemberId);
        }

        [Fact]
        public async Task CreateTaskItem_WhenUserIsNotMemberOrOwner_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId);
            var command = new CreateTaskItemCommand { ProjectId = _project1Id, Title = "Forbidden Task" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/taskitems", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task CreateTaskItem_ForNonExistentProject_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new CreateTaskItemCommand { ProjectId = _nonExistentProjectId, Title = "Task for Ghost Project" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/taskitems", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task CreateTaskItem_WithMissingTitle_ShouldReturnBadRequest()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new CreateTaskItemCommand { ProjectId = _project1Id, Title = "" };

            // Act
            var response = await _client.PostAsJsonAsync("/api/taskitems", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problemDetails.Should().NotBeNull();
            problemDetails!.Errors.Should().ContainKey(nameof(CreateTaskItemCommand.Title));
        }

        [Fact]
        public async Task CreateTaskItem_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var command = new CreateTaskItemCommand { ProjectId = _project1Id, Title = "Unauth Task" };
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.PostAsJsonAsync("/api/taskitems", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
