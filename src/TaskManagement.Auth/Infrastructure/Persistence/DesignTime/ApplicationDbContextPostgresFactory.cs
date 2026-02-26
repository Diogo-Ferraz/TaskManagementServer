using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManagement.Auth.Infrastructure.Persistence.DesignTime
{
    public sealed class ApplicationDbContextPostgresFactory : IDesignTimeDbContextFactory<ApplicationDbContextPostgres>
    {
        public ApplicationDbContextPostgres CreateDbContext(string[] args)
        {
            var connectionString =
                Environment.GetEnvironmentVariable("ConnectionStrings__TaskManagementDbConnectionPostgres")
                ?? "Host=localhost;Port=5432;Database=TaskManagementDb;Username=postgres;Password=postgres";

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContextPostgres>();
            optionsBuilder.UseNpgsql(connectionString);
            optionsBuilder.UseOpenIddict();

            return new ApplicationDbContextPostgres(optionsBuilder.Options);
        }
    }
}
