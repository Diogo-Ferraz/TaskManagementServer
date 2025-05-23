using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.TaskItems.Models
{
    public class TaskItem : BaseEntity
    {
        public required string Title { get; set; }
        public string? Description { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid ProjectId { get; set; }
        public string? AssignedUserId { get; set; } = string.Empty;
        public Project Project { get; set; } = null!;
    }

    public enum TaskStatus
    {
        Todo,
        InProgress,
        Done
    }
}
