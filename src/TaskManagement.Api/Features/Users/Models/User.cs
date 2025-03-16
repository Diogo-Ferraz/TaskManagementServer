using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Users.Models
{
    public class User : ApplicationUser
    {
        public ICollection<Project>? ManagedProjects { get; set; }
        public ICollection<TaskItem>? AssignedTasks { get; set; }
    }
}
