using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Domain.Entities
{
    public class User : ApplicationUser
    {
        public ICollection<Project>? ManagedProjects { get; set; }
        public ICollection<TaskItem>? AssignedTasks { get; set; }
    }
}
