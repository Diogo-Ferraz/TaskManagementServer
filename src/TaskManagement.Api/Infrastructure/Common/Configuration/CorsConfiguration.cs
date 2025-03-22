using TaskManagement.Api.Infrastructure.Common.Settings;

namespace TaskManagement.Api.Infrastructure.Common.Configuration
{
    public static class CorsConfiguration
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<ClientSettings>(configuration.GetSection("ClientSettings"));

            var clientConfig = configuration.GetSection("ClientSettings").Get<ClientSettings>();

            if (clientConfig != null && clientConfig.Clients.Any(c => c.AllowedCorsOrigins.Count > 0))
            {
                var allAllowedOrigins = clientConfig.Clients
                    .SelectMany(c => c.AllowedCorsOrigins)
                    .Distinct()
                    .ToArray();

                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.WithOrigins(allAllowedOrigins)
                             .AllowAnyHeader()
                             .AllowAnyMethod()
                             .AllowCredentials();
                    });
                });
            }
            else
            {
                services.AddCors(options =>
                {
                    options.AddDefaultPolicy(policy =>
                    {
                        policy.SetIsOriginAllowed(_ => false)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });
            }

            return services;
        }
    }
}
