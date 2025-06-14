using Microsoft.AspNetCore.Identity;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Auth.Infrastructure.Common.Settings;
using TaskManagement.Shared.Models;

namespace TaskManagement.Auth.Features.Identity.Services
{
    public static class IdentitySeeder
    {
        public static async Task SeedRolesAsync(this IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            string[] roleNames = [Roles.Administrator, Roles.ProjectManager, Roles.User];

            foreach (var roleName in roleNames)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        public static async Task SeedUsersAsync(this IServiceProvider serviceProvider, ILogger logger)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var usersToSeed = configuration.GetSection("SeedUsers").Get<List<SeedUserSettings>>();

            if (usersToSeed == null || usersToSeed.Count == 0)
            {
                logger.LogInformation("No seed users found in configuration. Skipping user seeding.");
                return;
            }

            foreach (var userSetting in usersToSeed)
            {
                await CreateUserIfNotExistsAsync(userManager, roleManager, userSetting, logger);
            }
        }

        private static async Task CreateUserIfNotExistsAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SeedUserSettings userSetting,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(userSetting.Email) ||
            string.IsNullOrWhiteSpace(userSetting.Password) ||
            string.IsNullOrWhiteSpace(userSetting.Role))
            {
                logger.LogWarning($"Skipping invalid seed user entry. Email, Password, and Role must all be provided. Email: '{userSetting.Email ?? "N/A"}'");
                return;
            }

            if (await userManager.FindByEmailAsync(userSetting.Email) == null)
            {
                if (!await roleManager.RoleExistsAsync(userSetting.Role))
                {
                    logger.LogWarning($"Role '{userSetting.Role}' does not exist. Skipping creation of user '{userSetting.Email}'. Please ensure roles are seeded first.");
                    return;
                }

                var user = new ApplicationUser
                {
                    UserName = userSetting.Email,
                    Email = userSetting.Email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, userSetting.Password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, userSetting.Role);
                    logger.LogInformation($"User '{userSetting.Email}' with role '{userSetting.Role}' created successfully");
                }
                else
                {
                    logger.LogError($"Failed to create user '{userSetting.Email}': {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
