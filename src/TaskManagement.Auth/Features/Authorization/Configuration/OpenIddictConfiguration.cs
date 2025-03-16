using Microsoft.IdentityModel.Tokens;
using Quartz;
using TaskManagement.Auth.Features.Authorization.Services;
using TaskManagement.Auth.Infrastructure.Common.Settings;
using TaskManagement.Auth.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskManagement.Auth.Features.Authorization.Configuration
{
    public static class OpenIddictConfiguration
    {
        public static IServiceCollection AddAuthorizationConfiguration(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.Configure<OpenIddictSettings>(builder.Configuration.GetSection("OpenIddict"));
            services.Configure<ClientSettings>(builder.Configuration.GetSection("ClientSettings"));

            AddOpenIddictServices(services, builder);

            return services;
        }

        private static IServiceCollection AddOpenIddictServices(IServiceCollection services, WebApplicationBuilder builder)
        {
            var openIddictSettings = builder.Configuration.GetSection("OpenIddict").Get<OpenIddictSettings>();
            if (openIddictSettings == null)
            {
                throw new InvalidOperationException("OpenIddict settings are missing.");
            }

            // OpenIddict offers native integration with Quartz.NET to perform scheduled tasks
            // (like pruning orphaned authorizations/tokens from the database) at regular intervals.
            services.AddQuartz(options =>
            {
                options.UseSimpleTypeLoader();
                options.UseInMemoryStore();
            });

            services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

            services.AddOpenIddict()
                .AddCore(options =>
                {
                    options.UseEntityFrameworkCore()
                        .UseDbContext<ApplicationDbContext>();

                    options.UseQuartz();
                })
                .AddServer(options =>
                {
                    options.SetAuthorizationEndpointUris("connect/authorize")
                       .SetEndSessionEndpointUris("connect/logout")
                       .SetTokenEndpointUris("connect/token")
                       .SetIssuer(openIddictSettings.Issuer)
                       .SetUserInfoEndpointUris("connect/userinfo");

                    options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                    options.AllowAuthorizationCodeFlow();

                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String(openIddictSettings.EncryptionKey)));

                    options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                    options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableEndSessionEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserInfoEndpointPassthrough()
                       .EnableStatusCodePagesIntegration();
                })
                .AddValidation(options =>
                {
                    options.UseLocalServer();
                    options.UseAspNetCore();
                });

            // Register the worker responsible for seeding the database.
            // Note: in a real world application, this step should be part of a setup script.
            services.AddHostedService<OpenIddictClientSeeder>();

            return services;
        }
    }
}