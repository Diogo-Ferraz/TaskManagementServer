using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenIddict.EntityFrameworkCore.Models;
using TaskManagement.Auth.Infrastructure.Persistence;
using TaskManagement.Auth.Tests.TestHelpers.Data;

namespace TaskManagement.Auth.Tests.TestHelpers.Fixtures
{
    public class CustomWebApplicationFactory<TEntryPoint> : WebApplicationFactory<TEntryPoint>, IAsyncLifetime
    where TEntryPoint : class
    {
        private readonly string _databaseName;
        private IServiceScopeFactory _scopeFactory;

        public CustomWebApplicationFactory()
        {
            _databaseName = $"InMemoryAuthDb_{Guid.NewGuid()}";
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            var contentRoot = ResolveAuthContentRoot(AppContext.BaseDirectory);
            builder.UseContentRoot(contentRoot);
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.SetBasePath(contentRoot);
                config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true, reloadOnChange: false);
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:TaskManagementDbConnection"] =
                        "Server=(localdb)\\mssqllocaldb;Database=TaskManagementDb_Test;Trusted_Connection=True;MultipleActiveResultSets=true",
                    ["OpenIddict:Issuer"] = "https://localhost/",
                    ["OpenIddict:EncryptionKey"] = "DRjd/GnduI3Efzen9V9BvbNUfc/VKgXltV7Kbk9sMkY=",
                    ["OpenIddict:Audience"] = ""
                });
            });

            builder.ConfigureServices(services =>
            {
                var descriptorsToRemove = services
                    .Where(d =>
                        d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>)
                        || d.ServiceType == typeof(DbContextOptions<ApplicationDbContextPostgres>)
                        || d.ServiceType == typeof(ApplicationDbContext)
                        || d.ServiceType == typeof(ApplicationDbContextPostgres))
                    .ToList();

                foreach (var descriptor in descriptorsToRemove)
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

        private static string ResolveAuthContentRoot(string baseDirectory)
        {
            var current = new DirectoryInfo(baseDirectory);

            while (current != null)
            {
                var candidate = Path.Combine(current.FullName, "src", "TaskManagement.Auth");
                var projectFile = Path.Combine(candidate, "TaskManagement.Auth.csproj");

                if (File.Exists(projectFile))
                {
                    return candidate;
                }

                current = current.Parent;
            }

            throw new DirectoryNotFoundException("Unable to locate TaskManagement.Auth content root.");
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
                var logger = scope.ServiceProvider.GetService<ILogger<CustomWebApplicationFactory<TEntryPoint>>>();
                logger?.LogError(ex, "An error occurred seeding the database. Error: {Message}", ex.Message);
            }
        }

        public async Task DisposeAsync()
        {
            if (_scopeFactory == null)
            {
                return;
            }

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
