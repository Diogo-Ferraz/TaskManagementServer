using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using TaskManagement.Api.Features.Dashboard.Models.DTOs;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Api.Tests.IntegrationTests.Fixtures;
using TaskManagement.Shared.Models;
using TaskItemStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Tests.IntegrationTests.Features.Dashboard
{
    public class GetDashboardSummaryEndpointTests : IClassFixture<ApiWebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly ApiWebApplicationFactory<Program> _factory;
        private HttpClient _client = null!;

        private const string UserId = "user-dashboard-endpoint-1";
        private const string OtherUserId = "user-dashboard-endpoint-2";

        private readonly Guid _ownedProjectId = Guid.NewGuid();
        private readonly Guid _memberProjectId = Guid.NewGuid();
        private readonly Guid _unrelatedProjectId = Guid.NewGuid();

        public GetDashboardSummaryEndpointTests(ApiWebApplicationFactory<Program> factory)
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

                var ownedProject = new Project
                {
                    Id = _ownedProjectId,
                    Name = "Owned Project",
                    OwnerUserId = UserId
                };

                var memberProject = new Project
                {
                    Id = _memberProjectId,
                    Name = "Member Project",
                    OwnerUserId = OtherUserId
                };
                memberProject.Members.Add(new ProjectMember
                {
                    ProjectId = _memberProjectId,
                    UserId = UserId,
                    AddedByUserId = OtherUserId,
                    JoinedAt = now
                });

                var unrelatedProject = new Project
                {
                    Id = _unrelatedProjectId,
                    Name = "Unrelated Project",
                    OwnerUserId = OtherUserId
                };

                db.Projects.AddRange(ownedProject, memberProject, unrelatedProject);

                db.TaskItems.AddRange(
                    new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Owned - Todo",
                        Status = TaskItemStatus.Todo,
                        ProjectId = _ownedProjectId,
                        AssignedUserId = UserId,
                        DueDate = now.AddDays(1),
                        LastModifiedAt = now.AddDays(-1)
                    },
                    new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Owned - Done",
                        Status = TaskItemStatus.Done,
                        ProjectId = _ownedProjectId,
                        AssignedUserId = UserId,
                        LastModifiedAt = now.AddDays(-2)
                    },
                    new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Member - Overdue",
                        Status = TaskItemStatus.InProgress,
                        ProjectId = _memberProjectId,
                        AssignedUserId = UserId,
                        DueDate = now.AddDays(-3),
                        LastModifiedAt = now.AddDays(-1)
                    },
                    new TaskItem
                    {
                        Id = Guid.NewGuid(),
                        Title = "Unrelated - Done",
                        Status = TaskItemStatus.Done,
                        ProjectId = _unrelatedProjectId,
                        AssignedUserId = UserId,
                        LastModifiedAt = now.AddDays(-1)
                    });

                return Task.CompletedTask;
            });
        }

        public Task DisposeAsync() => Task.CompletedTask;

        [Fact]
        public async Task GetSummary_WhenUserIsNotAdmin_ShouldReturnVisibleScopeCounters()
        {
            // Arrange
            SetAuthenticatedUser(UserId, Roles.User);

            // Act
            var response = await _client.GetAsync("/api/dashboard/summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
            summary.Should().NotBeNull();
            summary!.ProjectsCount.Should().Be(2);
            summary.AssignedTasksCount.Should().Be(3);
            summary.TasksClosedThisWeekCount.Should().Be(1);
            summary.OverdueAssignedTasksCount.Should().Be(1);
        }

        [Fact]
        public async Task GetSummary_WhenUserIsAdmin_ShouldReturnFullScopeCounters()
        {
            // Arrange
            SetAuthenticatedUser(UserId, Roles.Administrator);

            // Act
            var response = await _client.GetAsync("/api/dashboard/summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>();
            summary.Should().NotBeNull();
            summary!.ProjectsCount.Should().Be(3);
            summary.AssignedTasksCount.Should().Be(4);
            summary.TasksClosedThisWeekCount.Should().Be(2);
            summary.OverdueAssignedTasksCount.Should().Be(1);
        }

        [Fact]
        public async Task GetSummary_WithoutAuthentication_ShouldReturnUnauthorized()
        {
            // Arrange
            var unauthenticatedClient = _factory.CreateClient();

            // Act
            var response = await unauthenticatedClient.GetAsync("/api/dashboard/summary");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
