namespace TaskManagement.Api.Features.Dashboard.Models.DTOs
{
    public class DashboardSummaryDto
    {
        public int AssignedTasksCount { get; set; }
        public int TasksClosedThisWeekCount { get; set; }
        public int ProjectsCount { get; set; }
        public int OverdueAssignedTasksCount { get; set; }
    }
}
