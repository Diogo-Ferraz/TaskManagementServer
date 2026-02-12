using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Features.Projects.Services.Interfaces;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Activity.Hubs
{
    [Authorize(AuthenticationSchemes = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)]
    public class ActivityHub : Hub
    {
        private readonly ICurrentUserService _currentUserService;
        private readonly IProjectMembershipService _projectMembershipService;

        public ActivityHub(
            ICurrentUserService currentUserService,
            IProjectMembershipService projectMembershipService)
        {
            _currentUserService = currentUserService;
            _projectMembershipService = projectMembershipService;
        }

        public override async Task OnConnectedAsync()
        {
            await AddUserScopeGroupsAsync();

            await base.OnConnectedAsync();
        }

        public async Task JoinProject(Guid projectId)
        {
            var userId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("User not authenticated.");
            }

            if (!_currentUserService.IsInRole(Roles.Administrator))
            {
                var isMember = await _projectMembershipService.IsMemberAsync(projectId, userId, Context.ConnectionAborted);
                if (!isMember)
                {
                    throw new HubException("User is not authorized to join this project.");
                }
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, GetProjectGroupName(projectId));
        }

        public async Task JoinProjects(IReadOnlyCollection<Guid> projectIds)
        {
            if (projectIds == null || projectIds.Count == 0)
            {
                return;
            }

            var userId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("User not authenticated.");
            }

            if (_currentUserService.IsInRole(Roles.Administrator))
            {
                foreach (var projectId in projectIds.Distinct())
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, GetProjectGroupName(projectId));
                }

                return;
            }

            var allowedProjects = await _projectMembershipService.GetProjectIdsForUserAsync(userId, Context.ConnectionAborted);
            var allowedSet = allowedProjects.ToHashSet();

            foreach (var projectId in projectIds.Distinct())
            {
                if (allowedSet.Contains(projectId))
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, GetProjectGroupName(projectId));
                }
            }
        }

        public async Task JoinAllProjects()
        {
            await AddUserScopeGroupsAsync();
        }

        public async Task ResubscribeToScope()
        {
            await AddUserScopeGroupsAsync();
        }

        private async Task AddUserScopeGroupsAsync()
        {
            var userId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new HubException("User not authenticated.");
            }

            if (_currentUserService.IsInRole(Roles.Administrator))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetAdminGroupName());
                return;
            }

            var projectIds = await _projectMembershipService.GetProjectIdsForUserAsync(userId, Context.ConnectionAborted);
            foreach (var projectId in projectIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetProjectGroupName(projectId));
            }
        }

        public Task LeaveProject(Guid projectId)
        {
            return Groups.RemoveFromGroupAsync(Context.ConnectionId, GetProjectGroupName(projectId));
        }

        public static string GetProjectGroupName(Guid projectId) => $"project:{projectId}";
        public static string GetAdminGroupName() => "admin:all-projects";
    }
}
