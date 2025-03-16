using TaskManagement.Api.Features.Users.Services;
using TaskManagement.Api.Features.Users.Services.Interfaces;

namespace TaskManagement.Api.Features.Users.Configuration
{
    public static class UserConfiguration
    {
        public static IServiceCollection AddUserFeature(this IServiceCollection services)
        {
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<ICurrentUserService, CurrentUserService>();
            services.AddHttpContextAccessor();

            return services;
        }
    }
}
