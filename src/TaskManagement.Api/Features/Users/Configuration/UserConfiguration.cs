using TaskManagement.Api.Features.Users.Services;
using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Features.Users.Configuration
{
    public static class UserConfiguration
    {
        public static IServiceCollection AddUserFeature(this IServiceCollection services)
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
