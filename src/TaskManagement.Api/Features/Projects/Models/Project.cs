using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Models;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Models
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
