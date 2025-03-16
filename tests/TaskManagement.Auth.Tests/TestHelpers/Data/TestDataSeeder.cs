using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;
using TaskManagement.Auth.Features.Identity.Models;

namespace TaskManagement.Auth.Tests.TestHelpers.Data
{
    public class TestDataSeeder
    {
        public static async Task SeedAsync(IServiceScope scope)
        {
            await SeedUserAsync(scope);
            await SeedClientAsync(scope);
        }

        private static async Task SeedUserAsync(IServiceScope scope)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AuthUser>>();

            var user = TestData.User.Create();
            var existingUser = await userManager.FindByEmailAsync(user.Email);

            if (existingUser == null)
            {
                await userManager.CreateAsync(user, TestData.User.Password);
            }
        }

        private static async Task SeedClientAsync(IServiceScope scope)
        {
            var applicationManager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

            var existingClient = await applicationManager.FindByClientIdAsync(TestData.Client.Id);

            if (existingClient == null)
            {
                await applicationManager.CreateAsync(TestData.Client.GetDescriptor());
            }
        }
    }
}
