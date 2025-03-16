using Microsoft.AspNetCore.Identity;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Shared.Models;

namespace TaskManagement.Auth.Features.Identity.Services
{
    public static class RoleSeeder
    {
        public static async Task SeedRolesAsync(this IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = [Roles.Administrator, Roles.ProjectManager, Roles.RegularUser];

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public static async Task SeedDefaultAdminAsync(this IServiceProvider serviceProvider, ILogger logger)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AuthUser>>();

            var adminEmail = Environment.GetEnvironmentVariable("DEV_ADMIN_EMAIL");
            var adminPassword = Environment.GetEnvironmentVariable("DEV_ADMIN_PASSWORD");

            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
            {
                logger.LogWarning("Default admin credentials are not set in the environment variables. Skipping default admin creation.");
                return;
            }

            if (await userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AuthUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true
                };

                var createUserResult = await userManager.CreateAsync(adminUser, adminPassword);

                if (createUserResult.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Administrator);
                    logger.LogInformation($"Admin user {adminEmail} created successfully.");
                }
                else
                {
                    var errors = string.Join(", ", createUserResult.Errors.Select(e => e.Description));
                    logger.LogError($"Error creating admin user: {errors}");
                }
            }
            else
            {
                logger.LogInformation($"Admin user {adminEmail} already exists.");
            }
        }
    }
}
