using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Domain.Entities
{
    public class User : ApplicationUser
    {
        public UserRole Role { get; set; }
        public ICollection<Project>? ManagedProjects { get; set; }
        public ICollection<TaskItem>? AssignedTasks { get; set; }
    }

    public enum UserRole
    {
        ProjectManager,
        RegularUser
    }
}
