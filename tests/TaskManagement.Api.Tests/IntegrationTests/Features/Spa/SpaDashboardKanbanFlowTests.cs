using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Activity.Models.DTOs;
using TaskManagement.Api.Features.Dashboard.Models.DTOs;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Spa
{
    public class SpaDashboardKanbanFlowTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private const string ProjectManagerId = "spa-flow-project-manager";
        private const string MemberUserId = "spa-flow-member-user";

        public SpaDashboardKanbanFlowTests(ApiWebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        public async Task InitializeAsync()
        {
            _client = _factory.CreateClient();
            await _factory.ResetDatabaseAsync();
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task SpaDashboardAndKanbanFlow_ShouldSupportTypicalProjectLifecycle()
        {
            // 1) Project manager creates a project.
            SetAuthenticatedUser(ProjectManagerId, Roles.ProjectManager);
            var createProjectResponse = await _client.PostAsJsonAsync("/api/projects", new CreateProjectCommand
            {
                Name = "SPA Flow Project",
                Description = "Used for an end-to-end SPA-like test flow."
            });

            createProjectResponse.StatusCode.Should().Be(HttpStatusCode.Created);
            var project = await createProjectResponse.Content.ReadFromJsonAsync<ProjectDto>();
            project.Should().NotBeNull();
            var projectId = project!.Id;

            // 2) Project manager creates tasks that will populate Kanban columns.
            var todoTask = await CreateTaskAsync(projectId, "Task Todo", TaskStatus.Todo, MemberUserId);
            var inProgressTask = await CreateTaskAsync(projectId, "Task In Progress", TaskStatus.InProgress, ProjectManagerId);
            var doneTask = await CreateTaskAsync(projectId, "Task Done", TaskStatus.Done, MemberUserId);

            // 3) Member user can fetch visible projects (project picker in SPA).
            SetAuthenticatedUser(MemberUserId, Roles.User);
            var projectsResponse = await _client.GetAsync("/api/projects?page=1&pageSize=20");
            projectsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var visibleProjects = await projectsResponse.Content.ReadFromJsonAsync<List<ProjectDto>>();
            visibleProjects.Should().NotBeNull();
            visibleProjects!.Should().Contain(p => p.Id == projectId);

            // 4) Member user loads Kanban board tasks for that project.
            var kanbanResponse = await _client.GetAsync($"/api/taskitems/project/{projectId}");
            kanbanResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var kanbanTasks = await kanbanResponse.Content.ReadFromJsonAsync<List<TaskItemDto>>();
            kanbanTasks.Should().NotBeNull();
            kanbanTasks!.Should().HaveCount(3);
            kanbanTasks.Should().Contain(t => t.Id == todoTask.Id && t.AssignedUserId == MemberUserId);
            kanbanTasks.Should().Contain(t => t.Id == inProgressTask.Id && t.AssignedUserId == ProjectManagerId);
            kanbanTasks.Should().Contain(t => t.Id == doneTask.Id && t.Status == TaskStatus.Done);

            // 5) Member updates one task status (typical drag-and-drop status move).
            var patchResponse = await _client.PatchAsJsonAsync($"/api/taskitems/{todoTask.Id}", new PatchTaskItemCommand
            {
                Status = TaskStatus.InProgress
            });

            patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var patchedTask = await patchResponse.Content.ReadFromJsonAsync<TaskItemDto>();
            patchedTask.Should().NotBeNull();
            patchedTask!.Status.Should().Be(TaskStatus.InProgress);

            // 6) Member refreshes dashboard summary cards.
            var dashboardResponse = await _client.GetAsync("/api/dashboard/summary");
            dashboardResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var dashboard = await dashboardResponse.Content.ReadFromJsonAsync<DashboardSummaryDto>();
            dashboard.Should().NotBeNull();
            dashboard!.ProjectsCount.Should().Be(1);
            dashboard.AssignedTasksCount.Should().Be(2);
            dashboard.TasksClosedThisWeekCount.Should().Be(1);
            dashboard.OverdueAssignedTasksCount.Should().Be(0);

            // 7) Member refreshes activity feed used by notification widgets.
            var activityResponse = await _client.GetAsync($"/api/activity?projectId={projectId}&page=1&pageSize=50");
            activityResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            var activityItems = await activityResponse.Content.ReadFromJsonAsync<List<ActivityLogDto>>();
            activityItems.Should().NotBeNull();
            activityItems!.Should().Contain(x => x.Type == ActivityType.ProjectCreated && x.ProjectId == projectId);
            activityItems.Should().Contain(x => x.Type == ActivityType.TaskCreated && x.TaskItemId == todoTask.Id);
            activityItems.Should().Contain(x => x.Type == ActivityType.TaskStatusChanged && x.TaskItemId == todoTask.Id);
        }

        private async Task<TaskItemDto> CreateTaskAsync(Guid projectId, string title, TaskStatus status, string? assignedUserId)
        {
            var response = await _client.PostAsJsonAsync("/api/taskitems", new CreateTaskItemCommand
            {
                ProjectId = projectId,
                Title = title,
                Status = status,
                AssignedUserId = assignedUserId
            });

            response.StatusCode.Should().Be(HttpStatusCode.Created);
            var task = await response.Content.ReadFromJsonAsync<TaskItemDto>();
            task.Should().NotBeNull();
            return task!;
        }

        private void SetAuthenticatedUser(string userId, string roles)
        {
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserIdHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserRolesHeader);
            _client.DefaultRequestHeaders.Remove(TestAuthenticationHandler.TestUserNameHeader);

            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserIdHeader, userId);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserRolesHeader, roles);
            _client.DefaultRequestHeaders.Add(TestAuthenticationHandler.TestUserNameHeader, userId);
        }
    }
}
