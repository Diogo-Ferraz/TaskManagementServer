using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.TaskItems
{
    public class UpdateTaskItemEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client;

        // Test User IDs
        private readonly string _projectOwnerId = "user-task-update-owner-1";
        private readonly string _taskAssigneeId = "user-task-update-assignee-2";
        private readonly string _projectMemberNotAssigneeId = "user-task-update-member-3";
        private readonly string _unrelatedUserId = "user-task-update-unrelated-4";

        // Test Project and Task IDs
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly Guid _taskIdToUpdate = Guid.NewGuid();
        private readonly Guid _anotherTaskIdInProject = Guid.NewGuid();

        private const string InitialTaskTitle = "Initial Task Title for Update";
        private const TaskStatus InitialTaskStatus = TaskStatus.Todo;

        public UpdateTaskItemEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(async db =>
            {
                var project = new Project
                {
                    Id = _projectId,
                    Name = "Project For Updating Tasks",
                    OwnerUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _projectOwnerId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };
                project.Members.Add(new ProjectMember { ProjectId = _projectId, UserId = _taskAssigneeId });
                project.Members.Add(new ProjectMember { ProjectId = _projectId, UserId = _projectMemberNotAssigneeId });

                var task1 = new TaskItem
                {
                    Id = _taskIdToUpdate,
                    Title = InitialTaskTitle,
                    ProjectId = _projectId,
                    Project = project,
                    Status = InitialTaskStatus,
                    AssignedUserId = _taskAssigneeId,
                    CreatedByUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };
                var task2 = new TaskItem
                {
                    Id = _anotherTaskIdInProject,
                    Title = "Owner's Other Task",
                    ProjectId = _projectId,
                    Project = project,
                    Status = TaskStatus.InProgress,
                    AssignedUserId = _projectOwnerId,
                    CreatedByUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };

                db.Projects.Add(project);
                db.TaskItems.AddRange(task1, task2);
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        private void SetAuthenticatedUser(string userId)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
        }

        [Fact]
        public async Task UpdateTaskItem_WhenUserIsProjectOwner_ShouldReturnOkAndUpdatedDto()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new UpdateTaskItemCommand
            {
                Id = _taskIdToUpdate,
                Title = "Updated Title by Owner",
                Description = "Owner updated description",
                Status = TaskStatus.InProgress,
                AssignedUserId = _taskAssigneeId
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            updatedDto.Should().NotBeNull();
            updatedDto!.Id.Should().Be(_taskIdToUpdate);
            updatedDto.Title.Should().Be(command.Title);
            updatedDto.Status.Should().Be(command.Status);
            updatedDto.AssignedUserId.Should().Be(command.AssignedUserId);

            // Assert Database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await db.TaskItems.FindAsync(_taskIdToUpdate);
                taskInDb.Should().NotBeNull();
                taskInDb!.Title.Should().Be(command.Title);
                taskInDb.Description.Should().Be(command.Description);
                taskInDb.Status.Should().Be(command.Status);
                taskInDb.LastModifiedByUserId.Should().Be(_projectOwnerId);
            }
        }

        [Fact]
        public async Task UpdateTaskItem_WhenUserIsAssignee_ShouldReturnOkAndUpdatedDto()
        {
            // Arrange
            SetAuthenticatedUser(_taskAssigneeId);
            var command = new UpdateTaskItemCommand
            {
                Id = _taskIdToUpdate,
                Title = "Updated Title by Assignee",
                Status = TaskStatus.Done
            };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            updatedDto.Should().NotBeNull();
            updatedDto!.Title.Should().Be(command.Title);
            updatedDto.Status.Should().Be(command.Status);

            // Assert Database
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await db.TaskItems.FindAsync(_taskIdToUpdate);
                taskInDb!.Status.Should().Be(command.Status);
                taskInDb.LastModifiedByUserId.Should().Be(_taskAssigneeId);
            }
        }

        [Fact]
        public async Task UpdateTaskItem_WhenUserIsProjectMemberButNotAssigneeOrOwner_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberNotAssigneeId);
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Forbidden Update Attempt" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateTaskItem_WhenUserIsUnrelated_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId);
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Forbidden Update by Unrelated" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task UpdateTaskItem_WithMismatchedRouteIdAndCommandId_ShouldReturnBadRequest()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new UpdateTaskItemCommand { Id = Guid.NewGuid(), Title = "Mismatched IDs" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task UpdateTaskItem_WhenTaskItemDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new UpdateTaskItemCommand { Id = Guid.NewGuid(), Title = "Update NonExistent Task" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{command.Id}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task UpdateTaskItem_WithInvalidData_ShouldReturnBadRequest()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "" };

            // Act
            var response = await _client.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var problemDetails = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
            problemDetails!.Errors.Should().ContainKey(nameof(UpdateTaskItemCommand.Title));
        }

        [Fact]
        public async Task UpdateTaskItem_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var command = new UpdateTaskItemCommand { Id = _taskIdToUpdate, Title = "Unauth Update" };
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.PutAsJsonAsync($"/api/taskitems/{_taskIdToUpdate}", command);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
