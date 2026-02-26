using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Activity.Models;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Infrastructure.Persistence.Models;
using TaskManagement.Shared.DemoData;
using TaskManagement.Shared.Models;
using TaskStatus = TaskManagement.Api.Features.TaskItems.Models.TaskStatus;

namespace TaskManagement.Api.Infrastructure.Persistence.Seeding
{
    public static class DemoDataSeeder
    {
        public static async Task SeedDemoDataAsync(this IServiceProvider serviceProvider, ILogger logger, CancellationToken cancellationToken = default)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var enabled = configuration.GetValue<bool?>("DemoData:Enabled") ?? true;
            if (!enabled)
            {
                logger.LogInformation("Demo data seeding disabled by configuration.");
                return;
            }

            var dbContext = serviceProvider.GetRequiredService<TaskManagementDbContext>();

            var hasExistingProjects = await dbContext.Projects.AnyAsync(cancellationToken);
            if (hasExistingProjects)
            {
                logger.LogInformation("Projects already exist. Skipping demo data seeding.");
                return;
            }

            var now = DateTime.UtcNow;
            var random = new Random(42);

            var admins = DemoIdentityBlueprint.Administrators.ToList();
            var managers = DemoIdentityBlueprint.ProjectManagers.ToList();
            var users = DemoIdentityBlueprint.StandardUsers.ToList();
            var usersWithoutTasks = users.TakeLast(3).Select(user => user.Id).ToHashSet(StringComparer.Ordinal);

            var projectSeeds = new[]
            {
                new { Name = "Core Platform Revamp", Description = "Modernizing domain flows, auth boundaries, and telemetry." },
                new { Name = "Customer Portal v2", Description = "Next-gen UI for onboarding, support, and lifecycle workflows." },
                new { Name = "Mobile Companion App", Description = "Cross-platform mobile experience for contributors and managers." },
                new { Name = "Release Automation", Description = "CI/CD hardening, release quality gates, and deployment governance." },
                new { Name = "Billing Reliability", Description = "Resilience improvements for invoicing and payment reconciliation." },
                new { Name = "Workflow Intelligence", Description = "Insights and productivity metrics for teams and leadership." },
                new { Name = "Data Governance", Description = "Data quality, retention, auditability, and compliance controls." },
                new { Name = "Notifications Hub", Description = "Unified notification delivery and user preference management." },
                new { Name = "Performance Track", Description = "Latency and throughput improvements across API and SPA flows." },
                new { Name = "Design System Refresh", Description = "Consistent component library updates and accessibility improvements." }
            };

            var taskVolume = new[] { 24, 18, 16, 12, 9, 7, 5, 3, 2, 0 };
            var projects = new List<Project>(projectSeeds.Length);
            var projectMembers = new List<ProjectMember>();
            var tasks = new List<TaskItem>();
            var activityLogs = new List<ActivityLog>();

            var taskTitles = new[]
            {
                "Align API contracts with frontend needs",
                "Refine board drag-and-drop interaction",
                "Harden role-based authorization checks",
                "Improve activity feed readability",
                "Add resilient error-state placeholders",
                "Optimize table filtering for large datasets",
                "Polish responsive behavior for mobile layouts",
                "Enhance search relevance and filter presets",
                "Improve notification delivery consistency",
                "Expand regression test coverage for workflows",
                "Review project-level audit events",
                "Rework settings UX for discoverability"
            };

            var userRoleById = DemoIdentityBlueprint.Users.ToDictionary(user => user.Id, user => user.Role, StringComparer.Ordinal);
            var userDisplayNameById = DemoIdentityBlueprint.Users.ToDictionary(user => user.Id, user => user.DisplayName, StringComparer.Ordinal);

            for (var index = 0; index < projectSeeds.Length; index++)
            {
                var owner = managers[index % managers.Count];
                var coManager = managers[(index + 1) % managers.Count];
                var adminSponsor = admins[index % admins.Count];

                var project = new Project
                {
                    Id = Guid.Parse($"10000000-0000-0000-0000-{index + 1:000000000000}"),
                    Name = projectSeeds[index].Name,
                    Description = projectSeeds[index].Description,
                    OwnerUserId = owner.Id,
                    CreatedAt = now.AddDays(-70 + index * 3),
                    CreatedByUserId = owner.Id,
                    CreatedByUserName = owner.DisplayName,
                    LastModifiedAt = now.AddDays(-2 + (index % 4)),
                    LastModifiedByUserId = coManager.Id,
                    LastModifiedByUserName = coManager.DisplayName
                };

                projects.Add(project);
                activityLogs.Add(new ActivityLog
                {
                    Type = ActivityType.ProjectCreated,
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    CreatedAt = project.CreatedAt.AddHours(1),
                    CreatedByUserId = owner.Id,
                    CreatedByUserName = owner.DisplayName,
                    LastModifiedAt = project.CreatedAt.AddHours(1),
                    LastModifiedByUserId = owner.Id,
                    LastModifiedByUserName = owner.DisplayName
                });

                var baseMembers = users
                    .OrderBy(_ => random.Next())
                    .Take(6 + (index % 4))
                    .Select(user => user.Id)
                    .ToHashSet(StringComparer.Ordinal);

                baseMembers.Add(owner.Id);
                baseMembers.Add(coManager.Id);
                baseMembers.Add(adminSponsor.Id);

                foreach (var memberUserId in baseMembers)
                {
                    projectMembers.Add(new ProjectMember
                    {
                        ProjectId = project.Id,
                        UserId = memberUserId,
                        AddedByUserId = owner.Id,
                        JoinedAt = now.AddDays(-60 + random.Next(0, 35))
                    });
                }

                for (var taskIndex = 0; taskIndex < taskVolume[index]; taskIndex++)
                {
                    var eligibleAssignees = baseMembers
                        .Where(userId => userRoleById.TryGetValue(userId, out var role) && role == Roles.User)
                        .Where(userId => !usersWithoutTasks.Contains(userId))
                        .ToList();

                    var assignTask = eligibleAssignees.Count > 0 && taskIndex % 5 != 0;
                    var assignedUserId = assignTask ? eligibleAssignees[random.Next(eligibleAssignees.Count)] : null;

                    var status = (taskIndex % 4) switch
                    {
                        0 => TaskStatus.Todo,
                        1 => TaskStatus.InProgress,
                        2 => TaskStatus.Done,
                        _ => TaskStatus.Todo
                    };

                    var createdBy = taskIndex % 3 == 0 ? owner : coManager;
                    var updatedAt = now.AddDays(-random.Next(0, 30));
                    var dueDate = updatedAt.AddDays(random.Next(-7, 18));

                    tasks.Add(new TaskItem
                    {
                        Id = Guid.Parse($"20000000-0000-0000-0000-{(index * 100 + taskIndex + 1):000000000000}"),
                        ProjectId = project.Id,
                        Title = $"{taskTitles[(index + taskIndex) % taskTitles.Length]} #{taskIndex + 1}",
                        Description = "Seeded demo task representing realistic delivery work. Includes planning, implementation, and review checkpoints.",
                        Status = status,
                        DueDate = dueDate,
                        AssignedUserId = assignedUserId,
                        CreatedAt = updatedAt.AddDays(-random.Next(3, 15)),
                        CreatedByUserId = createdBy.Id,
                        CreatedByUserName = createdBy.DisplayName,
                        LastModifiedAt = updatedAt,
                        LastModifiedByUserId = createdBy.Id,
                        LastModifiedByUserName = createdBy.DisplayName
                    });

                    var seededTask = tasks[^1];
                    activityLogs.Add(new ActivityLog
                    {
                        Type = ActivityType.TaskCreated,
                        ProjectId = project.Id,
                        TaskItemId = seededTask.Id,
                        ProjectName = project.Name,
                        TaskTitle = seededTask.Title,
                        CreatedAt = seededTask.CreatedAt.AddHours(1),
                        CreatedByUserId = createdBy.Id,
                        CreatedByUserName = createdBy.DisplayName,
                        LastModifiedAt = seededTask.CreatedAt.AddHours(1),
                        LastModifiedByUserId = createdBy.Id,
                        LastModifiedByUserName = createdBy.DisplayName
                    });

                    if (seededTask.Status != TaskStatus.Todo && taskIndex % 2 == 0)
                    {
                        activityLogs.Add(new ActivityLog
                        {
                            Type = ActivityType.TaskStatusChanged,
                            ProjectId = project.Id,
                            TaskItemId = seededTask.Id,
                            ProjectName = project.Name,
                            TaskTitle = seededTask.Title,
                            OldStatus = TaskStatus.Todo,
                            NewStatus = seededTask.Status,
                            CreatedAt = seededTask.CreatedAt.AddHours(2),
                            CreatedByUserId = createdBy.Id,
                            CreatedByUserName = createdBy.DisplayName,
                            LastModifiedAt = seededTask.CreatedAt.AddHours(2),
                            LastModifiedByUserId = createdBy.Id,
                            LastModifiedByUserName = createdBy.DisplayName
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(seededTask.AssignedUserId) && taskIndex % 3 == 0)
                    {
                        var assigneeName = userDisplayNameById.TryGetValue(seededTask.AssignedUserId, out var displayName)
                            ? displayName
                            : seededTask.AssignedUserId;

                        activityLogs.Add(new ActivityLog
                        {
                            Type = ActivityType.TaskAssigneeChanged,
                            ProjectId = project.Id,
                            TaskItemId = seededTask.Id,
                            ProjectName = project.Name,
                            TaskTitle = seededTask.Title,
                            OldValue = "Unassigned",
                            NewValue = assigneeName,
                            CreatedAt = seededTask.CreatedAt.AddHours(3),
                            CreatedByUserId = createdBy.Id,
                            CreatedByUserName = createdBy.DisplayName,
                            LastModifiedAt = seededTask.CreatedAt.AddHours(3),
                            LastModifiedByUserId = createdBy.Id,
                            LastModifiedByUserName = createdBy.DisplayName
                        });
                    }

                    if (seededTask.DueDate.HasValue && taskIndex % 4 == 0)
                    {
                        activityLogs.Add(new ActivityLog
                        {
                            Type = ActivityType.TaskDueDateChanged,
                            ProjectId = project.Id,
                            TaskItemId = seededTask.Id,
                            ProjectName = project.Name,
                            TaskTitle = seededTask.Title,
                            OldValue = null,
                            NewValue = seededTask.DueDate.Value.ToString("O"),
                            CreatedAt = seededTask.CreatedAt.AddHours(4),
                            CreatedByUserId = createdBy.Id,
                            CreatedByUserName = createdBy.DisplayName,
                            LastModifiedAt = seededTask.CreatedAt.AddHours(4),
                            LastModifiedByUserId = createdBy.Id,
                            LastModifiedByUserName = createdBy.DisplayName
                        });
                    }

                    if (taskIndex % 5 == 0)
                    {
                        var oldTitle = $"Draft: {seededTask.Title}";
                        activityLogs.Add(new ActivityLog
                        {
                            Type = ActivityType.TaskRenamed,
                            ProjectId = project.Id,
                            TaskItemId = seededTask.Id,
                            ProjectName = project.Name,
                            TaskTitle = seededTask.Title,
                            OldValue = oldTitle,
                            NewValue = seededTask.Title,
                            CreatedAt = seededTask.CreatedAt.AddHours(5),
                            CreatedByUserId = createdBy.Id,
                            CreatedByUserName = createdBy.DisplayName,
                            LastModifiedAt = seededTask.CreatedAt.AddHours(5),
                            LastModifiedByUserId = createdBy.Id,
                            LastModifiedByUserName = createdBy.DisplayName
                        });
                    }
                }
            }

            dbContext.Projects.AddRange(projects);
            dbContext.ProjectMembers.AddRange(projectMembers);
            dbContext.TaskItems.AddRange(tasks);
            dbContext.ActivityLogs.AddRange(activityLogs);

            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Demo data seeded successfully: {ProjectCount} projects, {TaskCount} tasks, {ProjectMemberCount} project members, {ActivityLogCount} activity logs.",
                projects.Count,
                tasks.Count,
                projectMembers.Count,
                activityLogs.Count);
        }
    }
}
