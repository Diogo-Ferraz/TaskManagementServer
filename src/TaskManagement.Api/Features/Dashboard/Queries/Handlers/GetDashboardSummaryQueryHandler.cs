using MediatR;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Dashboard.Models.DTOs;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Features.Dashboard.Queries.Handlers
{
    public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
    {
        private readonly TaskManagementDbContext _dbContext;
        private readonly ICurrentUserService _currentUserService;

        public GetDashboardSummaryQueryHandler(
            TaskManagementDbContext dbContext,
            ICurrentUserService currentUserService)
        {
            _dbContext = dbContext;
            _currentUserService = currentUserService;
        }

        public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
        {
            var currentUserId = _currentUserService.Id;
            if (string.IsNullOrWhiteSpace(currentUserId))
            {
                throw new UnauthorizedAccessException("User not authenticated.");
            }

            var now = DateTime.UtcNow;
            var weekStart = StartOfWeekUtc(now, DayOfWeek.Monday);
            var isAdmin = _currentUserService.IsInRole(Roles.Administrator);

            IQueryable<Guid> visibleProjectIdsQuery = _dbContext.Projects.Select(p => p.Id);
            if (!isAdmin)
            {
                visibleProjectIdsQuery = _dbContext.Projects
                    .Where(p => p.OwnerUserId == currentUserId || p.Members.Any(m => m.UserId == currentUserId))
                    .Select(p => p.Id);
            }

            var projectsCountTask = visibleProjectIdsQuery.CountAsync(cancellationToken);

            var assignedTasksQuery = _dbContext.TaskItems
                .Where(t => t.AssignedUserId == currentUserId);

            if (!isAdmin)
            {
                assignedTasksQuery = assignedTasksQuery
                    .Where(t => t.Project.OwnerUserId == currentUserId || t.Project.Members.Any(m => m.UserId == currentUserId));
            }

            var assignedTasksCountTask = assignedTasksQuery.CountAsync(cancellationToken);
            var tasksClosedThisWeekCountTask = assignedTasksQuery
                .Where(t => t.Status == Features.TaskItems.Models.TaskStatus.Done && t.LastModifiedAt >= weekStart)
                .CountAsync(cancellationToken);
            var overdueAssignedTasksCountTask = assignedTasksQuery
                .Where(t => t.Status != Features.TaskItems.Models.TaskStatus.Done && t.DueDate.HasValue && t.DueDate.Value < now)
                .CountAsync(cancellationToken);

            await Task.WhenAll(
                projectsCountTask,
                assignedTasksCountTask,
                tasksClosedThisWeekCountTask,
                overdueAssignedTasksCountTask);

            return new DashboardSummaryDto
            {
                ProjectsCount = projectsCountTask.Result,
                AssignedTasksCount = assignedTasksCountTask.Result,
                TasksClosedThisWeekCount = tasksClosedThisWeekCountTask.Result,
                OverdueAssignedTasksCount = overdueAssignedTasksCountTask.Result
            };
        }

        private static DateTime StartOfWeekUtc(DateTime date, DayOfWeek startDay)
        {
            var diff = (7 + (date.DayOfWeek - startDay)) % 7;
            var start = date.Date.AddDays(-1 * diff);
            return DateTime.SpecifyKind(start, DateTimeKind.Utc);
        }
    }
}
