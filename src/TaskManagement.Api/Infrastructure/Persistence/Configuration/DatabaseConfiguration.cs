using Microsoft.EntityFrameworkCore;

namespace TaskManagement.Api.Infrastructure.Persistence.Configuration
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<TaskManagementDbContext>(options =>
            {
                options.UseSqlServer(configuration.GetConnectionString("TaskManagementDbConnection"),
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null));
            });

            return services;
        }

        public static async Task ApplyMigrationsAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                logger.LogInformation("API service waiting for Auth migrations...");
                var maxRetries = 10;
                var retryCount = 0;

                while (retryCount < maxRetries)
                {
                    try
                    {
                        var dbContext = services.GetRequiredService<TaskManagementDbContext>();
                        await dbContext.Database.MigrateAsync();
                        logger.LogInformation("API migrations applied successfully");
                        break;
                    }
                    catch (Exception)
                    {
                        retryCount++;
                        logger.LogInformation("Waiting for database to be available. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                        await Task.Delay(TimeSpan.FromSeconds(5));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Migration error in API service");
            }
        }
    }
}
