using TaskManagement.Api.Features.TaskItems.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Features.TaskItems.Queries.Handlers
{
    internal static class TaskAssigneeDisplayNameResolver
    {
        public static async Task ApplyAsync(IReadOnlyCollection<TaskItemDto> taskItems, IUserDirectoryService userDirectoryService, CancellationToken cancellationToken)
        {
            var assignedUserIds = taskItems
                .Select(task => task.AssignedUserId)
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Select(userId => userId!.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            var displayNamesByUserId = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var userId in assignedUserIds)
            {
                var displayName = await userDirectoryService.GetDisplayNameAsync(userId, cancellationToken);
                displayNamesByUserId[userId] = string.IsNullOrWhiteSpace(displayName) ? userId : displayName;
            }

            foreach (var task in taskItems)
            {
                var assignedUserId = task.AssignedUserId?.Trim();
                if (string.IsNullOrWhiteSpace(assignedUserId))
                {
                    task.AssignedUserName = "Unassigned";
                    continue;
                }

                task.AssignedUserName = displayNamesByUserId.TryGetValue(assignedUserId, out var displayName)
                    ? displayName
                    : assignedUserId;
            }
        }
    }
}
