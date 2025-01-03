using Quartz;
using TaskManagement.Auth.Infrastructure.Identity.Workers;
using TaskManagement.Auth.Infrastructure.Persistence;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TaskManagement.Auth.Infrastructure.Identity
{
    public static class OpenIddictConfig
    {
        public static IServiceCollection AddOpenIddictConfig(this IServiceCollection services, WebApplicationBuilder builder)
        {
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
                       .SetUserInfoEndpointUris("connect/userinfo");

                    options.RegisterScopes(Scopes.Email, Scopes.Profile, Scopes.Roles);

                    options.AllowAuthorizationCodeFlow();

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
            services.AddHostedService<Worker>();

            return services;
        }
    }
}
