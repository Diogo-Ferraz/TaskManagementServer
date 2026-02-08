using Microsoft.Extensions.Options;
using TaskManagement.Api.Features.Users.Services;
using TaskManagement.Api.Features.Users.Services.Interfaces;
using TaskManagement.Api.Features.Users.Settings;

namespace TaskManagement.Api.Features.Users.Configuration
{
    public static class UserConfiguration
    {
        public static IServiceCollection AddUserFeature(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.Configure<AuthApiSettings>(configuration.GetSection("AuthApi"));
            services.AddHttpClient<IUserDirectoryService, AuthUserDirectoryService>((sp, client) =>
            {
                var settings = sp.GetRequiredService<IOptions<AuthApiSettings>>().Value;
                if (string.IsNullOrWhiteSpace(settings.BaseUrl))
                {
                    throw new InvalidOperationException("AuthApi:BaseUrl is missing or empty.");
                }

                client.BaseAddress = new Uri(settings.BaseUrl, UriKind.Absolute);
            })
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                var handler = new HttpClientHandler();
                if (environment.IsDevelopment())
                {
                    handler.ServerCertificateCustomValidationCallback =
                        HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                }

                return handler;
            });

            return services;
        }
    }
}
