using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Auth.Infrastructure.Persistence
{
    public sealed class ApplicationDbContextPostgres : ApplicationDbContext
    {
        public ApplicationDbContextPostgres(DbContextOptions<ApplicationDbContextPostgres> options)
            : base(options)
        {
        }
    }
}
