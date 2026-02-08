using TaskManagement.Api.Features.TaskItems.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Features.Activity.Models.DTOs
{
    public class ActivityLogDto
    {
        public Guid Id { get; set; }
        public ActivityType Type { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? TaskItemId { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskTitle { get; set; }
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus? NewStatus { get; set; }
        public string ActorUserId { get; set; } = string.Empty;
        public string ActorDisplayName { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
    }
}
