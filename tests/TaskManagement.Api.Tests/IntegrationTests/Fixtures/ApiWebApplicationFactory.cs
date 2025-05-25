using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TaskManagement.Api.Infrastructure.Persistence;

namespace TaskManagement.Api.Tests.IntegrationTests.Fixtures
{
    public class ApiWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IAsyncLifetime
    where TEntryPoint : class
    {
        private readonly string _databaseName;
        private IServiceScopeFactory _scopeFactory;

        public Action<IServiceCollection> ConfigureTestServices { get; set; }

        public ApiWebApplicationFactory()
        {
            _databaseName = $"InMemoryApiDb_{Guid.NewGuid()}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((hostingContext, config) =>
            {
                config.Sources.Clear();
                config.AddJsonFile("appsettings.Testing.json", optional: false, reloadOnChange: false);
            });

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<TaskManagementDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<TaskManagementDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                });

                services.RemoveAll<IAuthenticationService>();
                services.RemoveAll<AuthenticationHandler<AuthenticationSchemeOptions>>();
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>(
                        "TestScheme", options => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("TestScheme")
                        .RequireAuthenticatedUser()
                        .Build();
                });

                ConfigureTestServices?.Invoke(services);
            });
        }

        public async Task InitializeAsync()
        {
            _scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            await db.Database.EnsureCreatedAsync();
        }

        public async Task DisposeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        public async Task SeedDatabaseAsync(Func<TaskManagementDbContext, Task> seedAction)
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            try
            {
                await seedAction(db);
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetService<ILogger<ApiWebApplicationFactory<TEntryPoint>>>();
                logger?.LogError(ex, "An error occurred seeding the database with test data.");
                throw;
            }
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<TaskManagementDbContext>();

            await db.Database.EnsureDeletedAsync();
            await db.Database.EnsureCreatedAsync();
        }
    }
}