using TaskManagement.Auth.Tests.TestHelpers.Data;

namespace TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Models
{
    public record AuthorizationParameters(
        string ClientId = TestData.Client.Id,
        string ClientSecret = TestData.Client.Secret,
        string RedirectUri = TestData.Client.RedirectUri,
        string ResponseType = "code",
        string Scope = "openid profile"
    );
}
