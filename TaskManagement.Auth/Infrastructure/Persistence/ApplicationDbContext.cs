using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Auth.Infrastructure.Identity;

namespace TaskManagement.Auth.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<AuthUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
    }
}
