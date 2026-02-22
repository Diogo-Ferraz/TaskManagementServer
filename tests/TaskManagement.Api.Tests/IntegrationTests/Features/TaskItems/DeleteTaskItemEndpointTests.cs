using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.TaskItems
{
    public class DeleteTaskItemEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        // Test User IDs
        private readonly string _projectOwnerId = "user-task-delete-owner-1";
        private readonly string _taskAssigneeId = "user-task-delete-assignee-2";
        private readonly string _projectMemberId = "user-task-delete-member-3";
        private readonly string _unrelatedUserId = "user-task-delete-unrelated-4";
        // Test Project and Task IDs
        private readonly Guid _projectId = Guid.NewGuid();
        private readonly Guid _taskToDeleteId = Guid.NewGuid();
        private readonly Guid _taskOfOtherOwnerInSameProject = Guid.NewGuid();
        private readonly Guid _nonExistentTaskId = Guid.NewGuid();

        public DeleteTaskItemEndpointTests(ApiWebApplicationFactory<Program> factory)
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
                    Name = "Project For Deleting Tasks",
                    OwnerUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    CreatedByUserId = _projectOwnerId,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };

                project.Members.Add(new ProjectMember
                {
                    ProjectId = _projectId,
                    UserId = _taskAssigneeId,
                    JoinedAt = DateTime.UtcNow,
                    AddedByUserId = _projectOwnerId
                });
                project.Members.Add(new ProjectMember
                {
                    ProjectId = _projectId,
                    UserId = _projectMemberId,
                    JoinedAt = DateTime.UtcNow,
                    AddedByUserId = _projectOwnerId
                });

                var task1 = new TaskItem
                {
                    Id = _taskToDeleteId,
                    Title = "Task To Be Deleted",
                    ProjectId = _projectId,
                    Project = project,
                    Status = TaskStatus.Done,
                    AssignedUserId = _taskAssigneeId,
                    CreatedByUserId = _projectOwnerId,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectOwnerId
                };
                var task2 = new TaskItem
                {
                    Id = _taskOfOtherOwnerInSameProject,
                    Title = "Another Task, Different Creator/Assignee",
                    ProjectId = _projectId,
                    Project = project,
                    Status = TaskStatus.Todo,
                    AssignedUserId = _projectOwnerId,
                    CreatedByUserId = _projectMemberId,
                    CreatedAt = DateTime.UtcNow,
                    LastModifiedAt = DateTime.UtcNow,
                    LastModifiedByUserId = _projectMemberId
                };


                db.Projects.Add(project);
                db.TaskItems.AddRange(task1, task2);
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
                string.IsNullOrWhiteSpace(roles) ? Roles.User : roles);
        }

        [Fact]
        public async Task DeleteTaskItem_WhenUserIsProjectOwner_ShouldReturnNoContentAndDeleteTask()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);
            int initialTaskCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialTaskCount = await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database State
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await dbContext.TaskItems.FindAsync(_taskToDeleteId);
                taskInDb.Should().BeNull();
                (await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync()).Should().Be(initialTaskCount - 1);
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WhenUserIsAdministrator_ShouldReturnNoContentAndDeleteTask()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.Administrator);
            int initialTaskCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialTaskCount = await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database State
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await dbContext.TaskItems.FindAsync(_taskToDeleteId);
                taskInDb.Should().BeNull();
                (await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync()).Should().Be(initialTaskCount - 1);
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WhenUserIsAssigneeButNotOwner_ShouldReturnNoContentAndDeleteTask()
        {
            // Arrange
            SetAuthenticatedUser(_taskAssigneeId);
            int initialTaskCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialTaskCount = await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database State
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await dbContext.TaskItems.FindAsync(_taskToDeleteId);
                taskInDb.Should().BeNull();
                (await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync()).Should().Be(initialTaskCount - 1);
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WhenUserIsProjectMemberButNotOwnerOrAssignee_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberId);
            int initialTaskCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialTaskCount = await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                (await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync()).Should().Be(initialTaskCount);
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WhenUserIsProjectManagerAndProjectMember_ShouldReturnNoContent()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberId, Roles.ProjectManager);
            int initialTaskCount;
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                initialTaskCount = await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync();
            }

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Assert Database State
            using (var scope = _factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
                var taskInDb = await dbContext.TaskItems.FindAsync(_taskToDeleteId);
                taskInDb.Should().BeNull();
                (await dbContext.TaskItems.Where(t => t.ProjectId == _projectId).CountAsync()).Should().Be(initialTaskCount - 1);
            }
        }

        [Fact]
        public async Task DeleteTaskItem_WhenTaskItemDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);

            // Act
            var response = await _client.DeleteAsync($"/api/taskitems/{_nonExistentTaskId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task DeleteTaskItem_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.DeleteAsync($"/api/taskitems/{_taskToDeleteId}");

            // Assert Response
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
