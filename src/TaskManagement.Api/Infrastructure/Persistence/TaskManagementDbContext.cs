using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Infrastructure.Persistence
{
    public class TaskManagementDbContext : IdentityDbContext<User>
    {
        public TaskManagementDbContext(DbContextOptions<TaskManagementDbContext> options)
            : base(options)
        {
        }

        public DbSet<Project> Projects { get; set; }
        public DbSet<TaskItem> TaskItems { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<User>(entity =>
            {
                entity.ToTable("AspNetUsers");

                entity.ToTable("AspNetUsers", t => t.ExcludeFromMigrations());
            });

            builder.Entity<Project>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(p => p.User)
                     .WithMany(u => u.ManagedProjects)
                     .HasForeignKey(p => p.UserId)
                     .OnDelete(DeleteBehavior.Restrict);
            });

            builder.Entity<TaskItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(t => t.Project)
                     .WithMany(p => p.TaskItems)
                     .HasForeignKey(t => t.ProjectId)
                     .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(t => t.AssignedUser)
                     .WithMany(u => u.AssignedTasks)
                     .HasForeignKey(t => t.AssignedUserId)
                     .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entry in ChangeTracker.Entries<BaseEntity>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.Entity.CreatedAt = DateTime.UtcNow;
                        entry.Entity.LastModifiedAt = DateTime.UtcNow;
                        break;
                    case EntityState.Modified:
                        entry.Entity.LastModifiedAt = DateTime.UtcNow;
                        break;
                }
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }
}
