using TaskManagement.Api.Features.Projects.Models;

namespace TaskManagement.Api.Infrastructure.Persistence.Models
{
    public class ProjectMember
    {
        public Guid ProjectId { get; set; }
        public required string UserId { get; set; }
        public Project Project { get; set; } = null!;
    }
}
