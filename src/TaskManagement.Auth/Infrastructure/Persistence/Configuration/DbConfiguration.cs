using Microsoft.EntityFrameworkCore;
using TaskManagement.Auth.Features.Identity.Services;

namespace TaskManagement.Auth.Infrastructure.Persistence.Configuration
{
    public static class DbConfiguration
    {
        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("TaskManagementDbConnection");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Connection string 'TaskManagementDbConnection' not found.");
            }

            services.AddDbContext<ApplicationDbContextPostgres>(options =>
            {
                options.UseNpgsql(connectionString,
                    npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorCodesToAdd: null));

                options.UseOpenIddict();
            });

            services.AddScoped<ApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContextPostgres>());

            services.AddDatabaseDeveloperPageExceptionFilter();
            services.AddHealthChecks();

            return services;
        }

        public static async Task ApplyMigrationsAndSeedDataAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("Applying database migrations...");
                var dbContext = services.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while migrating the database");
            }

            await services.SeedRolesAsync();

            if (app.Environment.IsDevelopment())
            {
                await services.SeedUsersAsync(logger);
            }
        }
    }
}
