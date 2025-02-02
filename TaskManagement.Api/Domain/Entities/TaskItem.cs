using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Domain.Entities
{
    public class TaskItem : BaseEntity
    {
        public required string Title { get; set; }
        public string Description { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid ProjectId { get; set; }
        public required string AssignedUserId { get; set; }
        public required Project Project { get; set; }
        public required User AssignedUser { get; set; }
    }

    public enum TaskStatus
    {
        Todo,
        InProgress,
        Done
    }
}
