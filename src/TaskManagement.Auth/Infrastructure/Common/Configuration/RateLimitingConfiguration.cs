using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TaskManagement.Auth.Infrastructure.Common.Settings;

namespace TaskManagement.Auth.Infrastructure.Common.Configuration
{
    public static class RateLimitingConfiguration
    {
        public static IServiceCollection AddRateLimitingConfiguration(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var settings = configuration.GetSection("RateLimiting").Get<RateLimitingSettings>() ?? new RateLimitingSettings();

            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                options.AddFixedWindowLimiter(RateLimitingPolicies.AdminUserManagement, limiter =>
                {
                    limiter.PermitLimit = Math.Max(1, settings.AdminUserManagement.PermitLimit);
                    limiter.Window = TimeSpan.FromSeconds(Math.Max(1, settings.AdminUserManagement.WindowSeconds));
                    limiter.QueueLimit = Math.Max(0, settings.AdminUserManagement.QueueLimit);
                    limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });

                options.AddFixedWindowLimiter(RateLimitingPolicies.TokenExchange, limiter =>
                {
                    limiter.PermitLimit = Math.Max(1, settings.TokenExchange.PermitLimit);
                    limiter.Window = TimeSpan.FromSeconds(Math.Max(1, settings.TokenExchange.WindowSeconds));
                    limiter.QueueLimit = Math.Max(0, settings.TokenExchange.QueueLimit);
                    limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                });
            });

            return services;
        }
    }
}
