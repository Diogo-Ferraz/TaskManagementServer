using TaskManagement.Api.Features.TaskItems.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Models.DTOs
{
    public class ProjectDto
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string OwnerUserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public DateTime LastModifiedAt { get; set; }
        public string LastModifiedByUserId { get; set; } = string.Empty;
        public ICollection<TaskItemDto>? TaskItems { get; set; }
    }
}
