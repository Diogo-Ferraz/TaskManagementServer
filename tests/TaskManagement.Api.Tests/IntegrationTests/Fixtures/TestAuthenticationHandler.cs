using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace TaskManagement.Api.Tests.IntegrationTests.Fixtures
{
    public class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public const string TestUserIdHeader = "X-Test-User-Id";
        public const string TestUserRolesHeader = "X-Test-User-Roles";
        public const string TestUserNameHeader = "X-Test-User-Name";

        public TestAuthenticationHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue(TestUserIdHeader, out var userIdValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var userId = userIdValues.FirstOrDefault();
            if (string.IsNullOrEmpty(userId))
            {
                return Task.FromResult(AuthenticateResult.Fail("Test User ID header was empty."));
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Subject, userId)
            };

            if (Request.Headers.TryGetValue(TestUserNameHeader, out var userNameValues))
            {
                var userName = userNameValues.FirstOrDefault();
                if (!string.IsNullOrEmpty(userName))
                {
                    claims.Add(new Claim(ClaimTypes.Name, userName));
                    claims.Add(new Claim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Name, userName));
                }
            }

            if (Request.Headers.TryGetValue(TestUserRolesHeader, out var rolesValues))
            {
                var roles = rolesValues.FirstOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries);
                if (roles != null)
                {
                    foreach (var role in roles)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, role.Trim()));
                        claims.Add(new Claim(OpenIddict.Abstractions.OpenIddictConstants.Claims.Role, role.Trim()));
                    }
                }
            }

            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}