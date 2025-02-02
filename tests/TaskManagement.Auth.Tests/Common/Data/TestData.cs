using OpenIddict.Abstractions;
using TaskManagement.Auth.Infrastructure.Identity;

namespace TaskManagement.Auth.Tests.Common.Data
{
    public static class TestData
    {
        public static class Client
        {
            public const string Id = "test-client";
            public const string Secret = "test-secret";
            public const string RedirectUri = "https://localhost/callback";

            public static OpenIddictApplicationDescriptor GetDescriptor()
            {
                return new OpenIddictApplicationDescriptor
                {
                    ClientId = Id,
                    ClientSecret = Secret,
                    RedirectUris = { new Uri(RedirectUri) },
                    Permissions =
                {
                    OpenIddictConstants.Permissions.Endpoints.Authorization,
                    OpenIddictConstants.Permissions.Endpoints.Token,
                    OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                    OpenIddictConstants.Permissions.ResponseTypes.Code,
                    OpenIddictConstants.Permissions.Scopes.Email,
                    OpenIddictConstants.Permissions.Scopes.Profile
                }
                };
            }
        }

        public static class User
        {
            public const string Email = "authorized@example.com";
            public const string Password = "StrongPassword@123";

            public static AuthUser Create()
            {
                return new AuthUser
                {
                    UserName = Email,
                    Email = Email,
                    EmailConfirmed = true
                };
            }
        }
    }
}
