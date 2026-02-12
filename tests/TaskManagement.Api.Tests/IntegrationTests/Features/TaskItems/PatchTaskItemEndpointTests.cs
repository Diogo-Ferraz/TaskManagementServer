using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.TaskItems
{
    public class PatchTaskItemEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private readonly Guid _projectId = Guid.NewGuid();
        private readonly Guid _taskId = Guid.NewGuid();
        private readonly string _ownerUserId = "user-task-patch-owner";
        private readonly string _memberUserId = "user-task-patch-member";
        private readonly string _otherUserId = "user-task-patch-other";

        public PatchTaskItemEndpointTests(ApiWebApplicationFactory<Program> factory)
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
                    Name = "Patch Task Project",
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
                db.TaskItems.Add(new TaskItem
                {
                    Id = _taskId,
                    Title = "Initial Task",
                    Description = "Initial description",
                    Status = TaskStatus.Todo,
                    ProjectId = _projectId,
                    AssignedUserId = _memberUserId
                });
                return Task.CompletedTask;
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task PatchTaskItem_ShouldUpdateStatusAndCreateActivityLog()
        {
            SetAuthenticatedUser(_ownerUserId);
            var command = new PatchTaskItemCommand { Status = TaskStatus.Done };

            var response = await _client.PatchAsJsonAsync($"/api/taskitems/{_taskId}", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var dto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            dto.Should().NotBeNull();
            dto!.Status.Should().Be(TaskStatus.Done);

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var activityExists = await db.ActivityLogs.AnyAsync(a => a.TaskItemId == _taskId);
            activityExists.Should().BeTrue();
        }

        [Fact]
        public async Task PatchTaskItem_ClearAssignedUser_ShouldSetNull()
        {
            SetAuthenticatedUser(_ownerUserId);
            var command = new PatchTaskItemCommand { AssignedUserId = null };

            var response = await _client.PatchAsJsonAsync($"/api/taskitems/{_taskId}", command);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            var task = await db.TaskItems.FindAsync(_taskId);
            task.Should().NotBeNull();
            task!.AssignedUserId.Should().BeNull();
            var activityExists = await db.ActivityLogs
                .AnyAsync(a => a.TaskItemId == _taskId && a.Type == ActivityType.TaskAssigneeChanged);
            activityExists.Should().BeTrue();
        }

        [Fact]
        public async Task PatchTaskItem_WhenUserIsNotAuthorized_ShouldReturnForbidden()
        {
            SetAuthenticatedUser(_otherUserId);
            var command = new PatchTaskItemCommand { Title = "Not allowed" };

            var response = await _client.PatchAsJsonAsync($"/api/taskitems/{_taskId}", command);

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        private void SetAuthenticatedUser(string userId)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserRolesHeader);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserRolesHeader, Roles.User);
        }
    }
}
