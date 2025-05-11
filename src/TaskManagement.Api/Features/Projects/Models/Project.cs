using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Infrastructure.Common.Models;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Features.Projects.Models
{
    public class Project : BaseEntity
    {
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public required string OwnerUserId { get; set; }
        public ICollection<ProjectMember> Members { get; set; } = [];
        public ICollection<TaskItem> TaskItems { get; set; } = [];
    }
}
