using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Projects.Models;
using TaskManagement.Api.Features.TaskItems.Models;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Models;
using TaskManagement.Api.Infrastructure.Persistence.Models;

namespace TaskManagement.Api.Infrastructure.Persistence
{
    public class TaskManagementDbContext : DbContext
    {
        private readonly ICurrentUserService? _currentUserService;

        public TaskManagementDbContext(
            DbContextOptions<TaskManagementDbContext> options,
            ICurrentUserService? currentUserService = null)
            : base(options)
        {
            _currentUserService = currentUserService;
        }

        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<TaskItem> TaskItems { get; set; } = null!;
        public DbSet<ProjectMember> ProjectMembers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.OwnerUserId).IsRequired();
            });

            builder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
                entity.Property(e => e.Description).HasMaxLength(1000);
                entity.Property(e => e.CreatedByUserId).IsRequired();

                entity.HasOne(t => t.Project)
                     .WithMany(p => p.TaskItems)
                     .HasForeignKey(t => t.ProjectId)
                     .OnDelete(DeleteBehavior.Cascade);
            });

            builder.Entity<ProjectMember>(entity =>
            {
                entity.HasKey(pm => new { pm.ProjectId, pm.UserId });

                entity.HasOne(pm => pm.Project)
                      .WithMany(p => p.Members)
                      .HasForeignKey(pm => pm.ProjectId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            var currentUserId = _currentUserService?.Id ?? "SYSTEM";

            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                var now = DateTime.UtcNow;

                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = now;
                        entry.Entity.CreatedByUserId = currentUserId;
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedByUserId = currentUserId;
                        break;

                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = now;
                        entry.Entity.LastModifiedByUserId = currentUserId;

                        entry.Property(nameof(BaseEntity.CreatedAt)).IsModified = false;
                        entry.Property(nameof(BaseEntity.CreatedByUserId)).IsModified = false;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
