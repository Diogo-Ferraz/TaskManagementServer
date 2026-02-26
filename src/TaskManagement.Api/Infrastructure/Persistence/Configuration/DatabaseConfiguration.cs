using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace TaskManagement.Api.Infrastructure.Persistence.Configuration
{
    public static class DatabaseConfiguration
    {
        public static IServiceCollection AddDatabaseConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            var databaseProvider = configuration["DatabaseProvider"] ?? "SqlServer";
            var sqlServerConnectionString = configuration.GetConnectionString("TaskManagementDbConnection");
            var postgresConnectionString = configuration.GetConnectionString("TaskManagementDbConnectionPostgres");

            if (IsPostgres(databaseProvider))
            {
                if (string.IsNullOrWhiteSpace(postgresConnectionString))
                {
                    throw new InvalidOperationException("Connection string 'TaskManagementDbConnectionPostgres' not found.");
                }

                services.AddDbContext<TaskManagementDbContextPostgres>(options =>
                {
                    options.UseNpgsql(postgresConnectionString,
                        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 5,
                            maxRetryDelay: TimeSpan.FromSeconds(10),
                            errorCodesToAdd: null));
                });

                services.AddScoped<TaskManagementDbContext>(sp =>
                    sp.GetRequiredService<TaskManagementDbContextPostgres>());
                return services;
            }

            if (string.IsNullOrWhiteSpace(sqlServerConnectionString))
            {
                throw new InvalidOperationException("Connection string 'TaskManagementDbConnection' not found.");
            }

            services.AddDbContext<TaskManagementDbContext>(options =>
            {
                options.UseSqlServer(sqlServerConnectionString,
                    sqlServerOptions => sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(10),
                        errorNumbersToAdd: null));
            });

            return services;
        }

        private static bool IsPostgres(string provider)
            => provider.Equals("postgres", StringComparison.OrdinalIgnoreCase)
               || provider.Equals("postgresql", StringComparison.OrdinalIgnoreCase)
               || provider.Equals("npgsql", StringComparison.OrdinalIgnoreCase);

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
