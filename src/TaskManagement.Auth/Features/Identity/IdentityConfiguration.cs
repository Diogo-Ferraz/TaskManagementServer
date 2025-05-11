using Microsoft.AspNetCore.Identity;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Auth.Infrastructure.Persistence;

namespace TaskManagement.Auth.Features.Identity.Configuration
{
    public static class IdentityConfiguration
    {
        public static IServiceCollection AddIdentityConfiguration(this IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders()
                .AddDefaultUI();

            return services;
        }
    }
}