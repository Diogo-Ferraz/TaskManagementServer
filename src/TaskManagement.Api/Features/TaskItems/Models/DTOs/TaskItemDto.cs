namespace TaskManagement.Api.Features.TaskItems.Models.DTOs
{
    public class TaskItemDto
    {
        public Guid Id { get; set; }
        public required string Title { get; set; }
        public string? Description { get; set; } = string.Empty;
        public TaskStatus Status { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string? AssignedUserId { get; set; } = string.Empty;
        public string AssignedUserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public string CreatedByUserName { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedByUserId { get; set; } = string.Empty;
        public string LastModifiedByUserName { get; set; } = string.Empty;
    }
}
