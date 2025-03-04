using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Domain.Entities
{
    public class Project : BaseEntity
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string UserId { get; set; }
        public User? User { get; set; }
        public ICollection<TaskItem>? TaskItems { get; set; }
    }
}
