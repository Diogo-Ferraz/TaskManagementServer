using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.TaskItems
{
    public class GetTaskItemEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private readonly string _projectOwnerId = "user-gettask-owner-1";
        private readonly string _project2OwnerId = "user-gettask-owner-2";
        private readonly string _projectMemberId = "user-gettask-member-2";
        private readonly string _taskAssigneeInProject1Id = "user-gettask-assignee-3";
        private readonly string _unrelatedUserId = "user-gettask-unrelated-4";

        private readonly Guid _project1Id = Guid.NewGuid();
        private readonly Guid _task1InProject1Id = Guid.NewGuid();
        private readonly Guid _task2InProject1Id = Guid.NewGuid();
        private readonly Guid _nonExistentTaskId = Guid.NewGuid();
        private readonly Guid _projectWithoutAccessId = Guid.NewGuid();
        private readonly Guid _emptyProjectId = Guid.NewGuid();


        public GetTaskItemEndpointTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();

            await _factory.SeedDatabaseAsync(db =>
            {
                var now = DateTime.UtcNow;
                var project1 = new Project
                {
                    Id = _project1Id,
                    Name = "Project For Getting Tasks",
                    OwnerUserId = _projectOwnerId,
                    CreatedAt = now,
                    CreatedByUserId = _projectOwnerId,
                    LastModifiedAt = now,
                    LastModifiedByUserId = _projectOwnerId
                };
                project1.Members.Add(new ProjectMember
                {
                    ProjectId = _project1Id,
                    UserId = _projectMemberId,
                    JoinedAt = now,
                    AddedByUserId = _projectOwnerId
                });
                project1.Members.Add(new ProjectMember
                {
                    ProjectId = _project1Id,
                    UserId = _taskAssigneeInProject1Id,
                    JoinedAt = now,
                    AddedByUserId = _projectOwnerId
                }); // Ensure assignee is also a member

                var project2 = new Project
                {
                    Id = _projectWithoutAccessId,
                    Name = "Project User Cannot Access",
                    OwnerUserId = _project2OwnerId,
                    CreatedAt = now,
                    CreatedByUserId = _project2OwnerId,
                    LastModifiedAt = now,
                    LastModifiedByUserId = _project2OwnerId
                };
                var project3 = new Project
                {
                    Id = _emptyProjectId,
                    Name = "Project With No Tasks",
                    OwnerUserId = _project2OwnerId,
                    CreatedAt = now,
                    CreatedByUserId = _project2OwnerId,
                    LastModifiedAt = now,
                    LastModifiedByUserId = _project2OwnerId
                };


                var task1 = new TaskItem
                {
                    Id = _task1InProject1Id,
                    Title = "Task One Details",
                    ProjectId = _project1Id,
                    Project = project1,
                    Status = TaskStatus.InProgress,
                    AssignedUserId = _taskAssigneeInProject1Id,
                    CreatedByUserId = _projectOwnerId,
                    CreatedAt = now,
                    LastModifiedAt = now,
                    LastModifiedByUserId = _projectOwnerId
                };
                var task2 = new TaskItem
                {
                    Id = _task2InProject1Id,
                    Title = "Task Two Details",
                    ProjectId = _project1Id,
                    Project = project1,
                    Status = TaskStatus.Todo,
                    AssignedUserId = _projectOwnerId,
                    CreatedByUserId = _projectMemberId,
                    CreatedAt = now.AddMinutes(1),
                    LastModifiedAt = now.AddMinutes(1),
                    LastModifiedByUserId = _projectMemberId
                };
                var task3 = new TaskItem
                {
                    Id = Guid.NewGuid(),
                    Title = "Task Three Project Two",
                    ProjectId = _projectWithoutAccessId,
                    Project = project2,
                    Status = TaskStatus.Done,
                    AssignedUserId = _project2OwnerId,
                    CreatedByUserId = _project2OwnerId,
                    CreatedAt = now.AddMinutes(-1),
                    LastModifiedAt = now.AddMinutes(-1),
                    LastModifiedByUserId = _project2OwnerId
                };

                db.Projects.AddRange(project1, project2, project3);
                db.TaskItems.AddRange(task1, task2, task3);
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
        public async Task GetTaskById_WhenUserIsProjectOwner_ShouldReturnTaskDto()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/{_task1InProject1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var taskDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            taskDto.Should().NotBeNull();
            taskDto!.Id.Should().Be(_task1InProject1Id);
            taskDto.Title.Should().Be("Task One Details");
        }

        [Fact]
        public async Task GetTaskById_WhenUserIsProjectMember_ShouldReturnTaskDto()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/{_task1InProject1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var taskDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            taskDto.Should().NotBeNull();
            taskDto!.Id.Should().Be(_task1InProject1Id);
        }

        [Fact]
        public async Task GetTaskById_WhenUserIsAdministrator_ShouldReturnTaskDto()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.Administrator);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/{_task1InProject1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var taskDto = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            taskDto.Should().NotBeNull();
            taskDto!.Id.Should().Be(_task1InProject1Id);
        }

        [Fact]
        public async Task GetTaskById_WhenUserIsNotMemberOrOwnerOfProject_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/{_task1InProject1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTaskById_WhenTaskDoesNotExist_ShouldReturnNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/{_nonExistentTaskId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTaskById_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.GetAsync($"/api/taskitems/{_task1InProject1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetTasksForProject_WhenUserIsOwner_ShouldReturnTasksList()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/project/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            tasks.Should().NotBeNull();
            tasks.Should().HaveCount(2);
            tasks.Should().Contain(t => t.Id == _task1InProject1Id);
            tasks.Should().Contain(t => t.Id == _task2InProject1Id);
        }

        [Fact]
        public async Task GetTasksForProject_WhenUserIsMember_ShouldReturnTasksList()
        {
            // Arrange
            SetAuthenticatedUser(_projectMemberId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/project/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            tasks.Should().NotBeNull();
            tasks.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTasksForProject_WhenUserIsNotMemberOrOwner_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/project/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetTasks_WithFilters_WhenUserIsAdministrator_ShouldReturnCrossProjectResults()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.Administrator);

            // Act
            var response = await _client.GetAsync($"/api/taskitems?projectId={_projectWithoutAccessId}&status=Done");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            tasks.Should().NotBeNull();
            tasks.Should().HaveCount(1);
            tasks!.All(t => t.ProjectId == _projectWithoutAccessId).Should().BeTrue();
        }

        [Fact]
        public async Task GetTasks_WithPagination_ShouldReturnRequestedPage()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.Administrator);

            // Act
            var firstPageResponse = await _client.GetAsync("/api/taskitems?page=1&pageSize=1");
            var secondPageResponse = await _client.GetAsync("/api/taskitems?page=2&pageSize=1");

            // Assert
            firstPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            secondPageResponse.StatusCode.Should().Be(HttpStatusCode.OK);

            var firstPage = await firstPageResponse.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            var secondPage = await secondPageResponse.Content.ReadFromJsonAsync<List<TaskItemDto>>();

            firstPage.Should().NotBeNull();
            secondPage.Should().NotBeNull();
            firstPage.Should().HaveCount(1);
            secondPage.Should().HaveCount(1);
            secondPage![0].Id.Should().NotBe(firstPage![0].Id);
        }

        [Fact]
        public async Task GetTasks_WithProjectFilter_WhenUserHasNoAccess_ShouldReturnForbidden()
        {
            // Arrange
            SetAuthenticatedUser(_unrelatedUserId, Roles.User);

            // Act
            var response = await _client.GetAsync($"/api/taskitems?projectId={_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }

        [Fact]
        public async Task GetTasksForProject_WhenProjectDoesNotExist_ShouldReturnForbiddenOrNotFound()
        {
            // Arrange
            SetAuthenticatedUser(_projectOwnerId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/project/{Guid.NewGuid()}");

            // Assert
            response.StatusCode.Should().Match(s => s == HttpStatusCode.Forbidden || s == HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task GetTasksForProject_WhenProjectHasNoTasks_ShouldReturnOkWithEmptyList()
        {
            // Arrange
            SetAuthenticatedUser(_project2OwnerId);

            // Act
            var response = await _client.GetAsync($"/api/taskitems/project/{_emptyProjectId}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var tasks = await response.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            tasks.Should().NotBeNull();
            tasks.Should().BeEmpty();
        }


        [Fact]
        public async Task GetTasksForProject_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthClient = _factory.CreateClient();

            // Act
            var response = await unauthClient.GetAsync($"/api/taskitems/project/{_project1Id}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
    }
}
