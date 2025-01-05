using Microsoft.AspNetCore.Identity;
using Serilog;
using TaskManagement.Auth.Domain.Entities;
using TaskManagement.Auth.Infrastructure.Configurations.Client;
using TaskManagement.Auth.Infrastructure.Configurations.Cors;
using TaskManagement.Auth.Infrastructure.Configurations.OpenIddict;
using TaskManagement.Auth.Infrastructure.Persistence;

namespace TaskManagement.Auth.Infrastructure.Configurations
{
    public static class ServicesConfiguration
    {
        public static IServiceCollection ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<OpenIddictSettings>(configuration.GetSection("OpenIddict"));
            services.Configure<CorsSettings>(configuration.GetSection("CorsSettings"));
            services.Configure<List<ClientSettings>>(configuration.GetSection("ClientSettings"));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            return services;
        }

        public static IHostBuilder AddSerilogLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.UseSerilog((context, config) =>
                config.ReadFrom.Configuration(configuration)
                      .Enrich.FromLogContext());
            return hostBuilder;
        }

        public static IServiceCollection ConfigureCors(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    var corsSettings = configuration.GetSection("CorsSettings").Get<CorsSettings>();
                    var allowedOrigins = corsSettings?.AllowedOrigins ?? [];
                    policy.WithOrigins([.. allowedOrigins])
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });
            return services;
        }
    }
}
