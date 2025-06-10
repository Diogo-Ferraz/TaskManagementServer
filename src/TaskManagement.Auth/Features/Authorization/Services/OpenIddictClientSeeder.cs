using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using TaskManagement.Auth.Infrastructure.Common.Settings;
using TaskManagement.Auth.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskManagement.Auth.Features.Authorization.Services
{
    public class OpenIddictClientSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ClientSettings _clientSettings;

        public OpenIddictClientSeeder(IServiceProvider serviceProvider, IOptions<ClientSettings> clientSettings)
        {
            _serviceProvider = serviceProvider;
            _clientSettings = clientSettings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();

            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await context.Database.EnsureCreatedAsync(cancellationToken);

            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            foreach (var clientSettings in _clientSettings.Clients)
            {
                var client = await manager.FindByClientIdAsync(clientSettings.ClientId, cancellationToken);
                if (client != null)
                {
                    await manager.DeleteAsync(client, cancellationToken);
                }

                var applicationDescriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = clientSettings.ClientId,
                    ConsentType = ConsentTypes.Explicit,
                    DisplayName = clientSettings.DisplayName,
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
                };

                foreach (var uri in clientSettings.RedirectUris)
                {
                    applicationDescriptor.RedirectUris.Add(new Uri(uri));
                }

                foreach (var uri in clientSettings.PostLogoutRedirectUris)
                {
                    applicationDescriptor.PostLogoutRedirectUris.Add(new Uri(uri));
                }

                foreach (var extraScope in clientSettings.AllowedScopes)
                {
                    applicationDescriptor.Permissions.Add($"{Permissions.Prefixes.Scope}{extraScope}");
                }

                await manager.CreateAsync(applicationDescriptor, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
