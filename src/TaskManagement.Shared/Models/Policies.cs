namespace TaskManagement.Shared.Models
{
    /// <summary>
    /// The authorization policies used in the application.
    /// </summary>
    public static class Policies
    {
        public const string CanManageProjects = nameof(CanManageProjects);
        public const string CanViewOwnProjects = nameof(CanViewOwnProjects);
        public const string CanManageTasks = nameof(CanManageTasks);
    }
}