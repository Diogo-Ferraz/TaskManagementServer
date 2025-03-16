using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.EntityFrameworkCore.Models;
using TaskManagement.Auth.Infrastructure.Persistence;
using TaskManagement.Auth.Tests.TestHelpers.Data;

namespace TaskManagement.Auth.Tests.TestHelpers.Fixtures
{
    public class CustomWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup>, IAsyncLifetime
    where TStartup : class
    {
        private readonly string _databaseName;
        private IServiceScopeFactory _scopeFactory;

        public CustomWebApplicationFactory()
        {
            _databaseName = $"InMemoryAuthDb_{Guid.NewGuid()}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_databaseName);
                    options.UseOpenIddict();
                });
            });
        }

        public async Task InitializeAsync()
        {
            _scopeFactory = Services.GetRequiredService<IServiceScopeFactory>();

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await db.Database.EnsureCreatedAsync();

            try
            {
                await TestDataSeeder.SeedAsync(scope);
            }
            catch (Exception ex)
            {
                var logger = scope.ServiceProvider.GetService<ILogger<CustomWebApplicationFactory<TStartup>>>();
                logger?.LogError(ex, "An error occurred seeding the database. Error: {Message}", ex.Message);
            }
        }

        public async Task DisposeAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.EnsureDeletedAsync();
        }

        public async Task ResetDatabaseAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (db.Set<OpenIddictEntityFrameworkCoreAuthorization>() is var authorizations && authorizations != null)
            {
                db.RemoveRange(authorizations);
            }

            if (db.Set<OpenIddictEntityFrameworkCoreToken>() is var tokens && tokens != null)
            {
                db.RemoveRange(tokens);
            }

            await db.SaveChangesAsync();
        }

        public HttpClient CreateClientWithNoRedirects()
        {
            return CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });
        }
    }
}
