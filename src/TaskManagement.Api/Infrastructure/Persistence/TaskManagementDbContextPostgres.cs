using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Infrastructure.Persistence
{
    public sealed class TaskManagementDbContextPostgres : TaskManagementDbContext
    {
        public TaskManagementDbContextPostgres(
            DbContextOptions<TaskManagementDbContextPostgres> options,
            ICurrentUserService? currentUserService = null)
            : base(options, currentUserService)
        {
        }
    }
}
