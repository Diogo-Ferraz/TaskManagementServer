using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Models.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedBy { get; set; } = string.Empty;
        public ICollection<TaskItemDto>? TaskItems { get; set; }
    }
}
