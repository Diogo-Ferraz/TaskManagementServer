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
                var resolvedClientType = ResolveClientType(clientSettings);
                var resolvedClientSecret = ResolveClientSecret(clientSettings, resolvedClientType);
                var applicationDescriptor = new OpenIddictApplicationDescriptor
                {
                    ClientId = clientSettings.ClientId,
                    ClientType = resolvedClientType,
                    ClientSecret = resolvedClientSecret,
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

                var existingClient = await manager.FindByClientIdAsync(clientSettings.ClientId, cancellationToken);
                if (existingClient == null)
                {
                    await manager.CreateAsync(applicationDescriptor, cancellationToken);
                    continue;
                }

                var existingClientType = await manager.GetClientTypeAsync(existingClient, cancellationToken);
                if (string.IsNullOrWhiteSpace(existingClientType))
                {
                    await manager.DeleteAsync(existingClient, cancellationToken);
                    await manager.CreateAsync(applicationDescriptor, cancellationToken);
                    continue;
                }

                await manager.UpdateAsync(existingClient, applicationDescriptor, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        private static string ResolveClientType(ClientSettingsOptions clientSettings)
        {
            if (string.IsNullOrWhiteSpace(clientSettings.ClientType))
            {
                return ClientTypes.Public;
            }

            return clientSettings.ClientType.Trim().ToLowerInvariant() switch
            {
                "public" => ClientTypes.Public,
                "confidential" => ClientTypes.Confidential,
                _ => throw new InvalidOperationException(
                    $"Unsupported OpenIddict client type '{clientSettings.ClientType}' for client '{clientSettings.ClientId}'.")
            };
        }

        private static string? ResolveClientSecret(ClientSettingsOptions clientSettings, string resolvedClientType)
        {
            if (resolvedClientType == ClientTypes.Public)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(clientSettings.ClientSecret))
            {
                return clientSettings.ClientSecret;
            }

            throw new InvalidOperationException(
                $"Client '{clientSettings.ClientId}' is configured as confidential but has no client secret.");
        }
    }
}
