using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using TaskManagement.Auth.Infrastructure.Common.Settings;

namespace TaskManagement.Auth.Features.Authorization.Services
{
    public class OpenIddictScopeSeeder : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ClientSettings _clientSettings;
        private readonly OpenIddictSettings _openIddictSettings;

        public OpenIddictScopeSeeder(
            IServiceProvider serviceProvider,
            IOptions<ClientSettings> clientSettings,
            IOptions<OpenIddictSettings> openIddictSettings)
        {
            _serviceProvider = serviceProvider;
            _clientSettings = clientSettings.Value;
            _openIddictSettings = openIddictSettings.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await using var scope = _serviceProvider.CreateAsyncScope();
            var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictScopeManager>();

            var allowedScopes = _clientSettings.Clients
                .SelectMany(client => client.AllowedScopes)
                .Where(scopeName => !string.IsNullOrWhiteSpace(scopeName))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var scopeName in allowedScopes)
            {
                var descriptor = new OpenIddictScopeDescriptor
                {
                    Name = scopeName,
                    DisplayName = $"{scopeName} scope"
                };

                if (!string.IsNullOrWhiteSpace(_openIddictSettings.Audience))
                {
                    descriptor.Resources.Add(_openIddictSettings.Audience);
                }

                var existingScope = await manager.FindByNameAsync(scopeName, cancellationToken);
                if (existingScope is null)
                {
                    await manager.CreateAsync(descriptor, cancellationToken);
                    continue;
                }

                await manager.UpdateAsync(existingScope, descriptor, cancellationToken);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
