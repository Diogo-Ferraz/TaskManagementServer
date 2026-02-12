using TaskManagement.Api.Features.TaskItems.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Activity.Models
{
    public class ActivityLog : BaseEntity
    {
        public ActivityType Type { get; set; }
        public Guid? ProjectId { get; set; }
        public Guid? TaskItemId { get; set; }
        public string? ProjectName { get; set; }
        public string? TaskTitle { get; set; }
        public TaskStatus? OldStatus { get; set; }
        public TaskStatus? NewStatus { get; set; }
    }

    public enum ActivityType
    {
        ProjectCreated,
        ProjectRenamed,
        ProjectDeleted,
        TaskCreated,
        TaskStatusChanged,
        TaskRenamed,
        TaskDeleted,
        TaskAssigneeChanged,
        TaskDueDateChanged
    }
}
