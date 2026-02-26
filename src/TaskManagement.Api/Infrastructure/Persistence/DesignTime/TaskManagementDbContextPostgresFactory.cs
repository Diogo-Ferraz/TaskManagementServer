using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Api.Infrastructure.Persistence.DesignTime
{
    public sealed class TaskManagementDbContextPostgresFactory : IDesignTimeDbContextFactory<TaskManagementDbContextPostgres>
    {
        public TaskManagementDbContextPostgres CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__TaskManagementDbConnection")
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__TaskManagementDbConnectionPostgres")
                ?? "Host=localhost;Port=5432;Database=TaskManagementDb;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<TaskManagementDbContextPostgres>();
            optionsBuilder.UseNpgsql(connectionString);

            return new TaskManagementDbContextPostgres(optionsBuilder.Options);
        }
    }
}
