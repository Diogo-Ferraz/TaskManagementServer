using Microsoft.AspNetCore.Identity;
using TaskManagement.Auth.Features.Identity.Models;
using TaskManagement.Shared.Models;
using TaskManagement.Shared.DemoData;

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

        public static async Task SeedDemoUsersAsync(this IServiceProvider serviceProvider, ILogger logger)
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            var demoDataEnabled = configuration.GetValue<bool?>("DemoData:Enabled") ?? true;
            var defaultPassword = configuration.GetValue<string>("DemoData:DefaultPassword") ?? "Demo123!";

            if (!demoDataEnabled)
            {
                logger.LogInformation("Demo data seeding disabled by configuration. Skipping user seeding.");
                return;
            }

            if (DemoIdentityBlueprint.Users.Count == 0)
            {
                logger.LogInformation("No seed users found in configuration. Skipping user seeding.");
                return;
            }

            foreach (var userSetting in DemoIdentityBlueprint.Users)
            {
                await CreateUserIfNotExistsAsync(userManager, roleManager, userSetting, defaultPassword, logger);
            }
        }

        private static async Task CreateUserIfNotExistsAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            DemoIdentityUser userSetting,
            string defaultPassword,
            ILogger logger)
        {
            if (string.IsNullOrWhiteSpace(userSetting.Email) ||
            string.IsNullOrWhiteSpace(defaultPassword) ||
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
                    Id = userSetting.Id.Trim(),
                    UserName = userSetting.Email,
                    Email = userSetting.Email,
                    EmailConfirmed = true,
                    DisplayName = string.IsNullOrWhiteSpace(userSetting.DisplayName)
                        ? null
                        : userSetting.DisplayName.Trim()
                };

                var result = await userManager.CreateAsync(user, defaultPassword);

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
