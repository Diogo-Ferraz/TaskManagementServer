using OpenIddict.Abstractions;
using TaskManagement.Auth.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskManagement.Auth.Infrastructure.Identity.Workers
{
    public class Worker : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;

        public Worker(IServiceProvider serviceProvider)
            => _serviceProvider = serviceProvider;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync();

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var client = await manager.FindByClientIdAsync("swagger-client", cancellationToken);

            if (client != null)
            {
                await manager.DeleteAsync(client, cancellationToken);
            }

            await manager.CreateAsync(new OpenIddictApplicationDescriptor
            {
                ClientId = "swagger-client",
                ClientSecret = "test_secret",
                ConsentType = ConsentTypes.Explicit,
                DisplayName = "Swagger Client application",
                RedirectUris =
                    {
                        new Uri("https://localhost:44377/callback/login/local")
                    },
                PostLogoutRedirectUris =
                    {
                        new Uri("https://localhost:44377/callback/logout/local")
                    },
                Permissions =
                    {
                        Permissions.Endpoints.Authorization,
                        Permissions.Endpoints.EndSession,
                        Permissions.Endpoints.Token,
                        Permissions.GrantTypes.AuthorizationCode,
                        Permissions.ResponseTypes.Code,
                        Permissions.Scopes.Email,
                        Permissions.Scopes.Profile,
                        Permissions.Scopes.Roles
                    },
                Requirements =
                    {
                        Requirements.Features.ProofKeyForCodeExchange
                    }
            }, cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
