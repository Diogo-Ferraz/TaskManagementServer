using TaskManagement.Auth.Tests.Common.Data;

namespace TaskManagement.Auth.Tests.IntegrationTests.Authorization.Models
{
    public record AuthorizationParameters(
        string ClientId = TestData.Client.Id,
        string ClientSecret = TestData.Client.Secret,
        string RedirectUri = TestData.Client.RedirectUri,
        string ResponseType = "code",
        string Scope = "openid profile"
    );
}
