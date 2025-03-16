using TaskManagement.Auth.Infrastructure.Common.Settings;

namespace TaskManagement.Auth.Infrastructure.Common.Configuration
{
    public static class CorsConfiguration
    {
        public static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration)
        {
            services.Configure<CorsSettings>(configuration.GetSection("CorsSettings"));

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