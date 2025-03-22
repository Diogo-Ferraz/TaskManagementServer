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

        public static async Task SeedUsersAsync(this IServiceProvider serviceProvider, ILogger logger)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<AuthUser>>();

            var usersToSeed = new List<(string Email, string Password, string Role)>
            {
                ("demo-admin@example.com", "Demo123!", Roles.Administrator),
                ("demo-manager@example.com", "Demo123!", Roles.ProjectManager),
                ("demo-user@example.com", "Demo123!", Roles.RegularUser)
            };

            foreach (var (email, password, role) in usersToSeed)
            {
                await CreateUserIfNotExistsAsync(userManager, email, password, role, logger);
            }
        }

        private static async Task CreateUserIfNotExistsAsync(
            UserManager<AuthUser> userManager,
            string email,
            string password,
            string role,
            ILogger logger)
        {
            if (await userManager.FindByEmailAsync(email) == null)
            {
                var user = new AuthUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, password);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, role);
                    logger.LogInformation($"User {email} with role {role} created successfully");
                }
                else
                {
                    logger.LogError($"Failed to create user {email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}
