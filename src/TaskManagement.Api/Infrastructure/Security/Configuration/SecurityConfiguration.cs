using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Infrastructure.Security.Settings;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Infrastructure.Security.Configuration
{
    public static class SecurityConfiguration
    {
        public static IServiceCollection AddOpenIddictValidation(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var openIddictSettings = configuration.GetSection("OpenIddict").Get<OpenIddictSettings>();
            if (openIddictSettings == null || string.IsNullOrEmpty(openIddictSettings.Issuer) || string.IsNullOrEmpty(openIddictSettings.EncryptionKey)
                || string.IsNullOrEmpty(openIddictSettings.Audience))
            {
                throw new InvalidOperationException("OpenIddict settings (Issuer, Audience, EncryptionKey) are missing or incomplete in configuration.");
            }

            if (!Uri.TryCreate(openIddictSettings.Issuer, UriKind.Absolute, out var publicIssuerUri))
            {
                throw new InvalidOperationException($"Invalid OpenIddict Issuer URI configured: '{openIddictSettings.Issuer}'");
            }

            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    options.SetIssuer(publicIssuerUri);
                    options.AddAudiences(openIddictSettings.Audience);
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String(openIddictSettings.EncryptionKey)));
                    options.UseSystemNetHttp()
                    .ConfigureHttpClientHandler(handler =>
                    {
                        if (environment.IsDevelopment())
                        {
                            handler.ServerCertificateCustomValidationCallback =
                                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        }
                    });

                    options.UseAspNetCore();
                });

            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            services.AddAuthorizationBuilder()
                .AddPolicy(Policies.CanManageProjects, policy =>
                    policy.RequireRole(Roles.Administrator, Roles.ProjectManager))
                .AddPolicy(Policies.CanManageTasks, policy =>
                    policy.RequireRole(Roles.Administrator, Roles.User))
                .AddPolicy(Policies.CanViewOwnProjects, policy =>
                    policy.RequireRole(Roles.Administrator, Roles.ProjectManager, Roles.User));

            return services;
        }
    }
}
