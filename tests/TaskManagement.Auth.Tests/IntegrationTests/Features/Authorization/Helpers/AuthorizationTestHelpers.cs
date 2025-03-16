using System.Text.Json;
using TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Models;

namespace TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Helpers
{
    public static class AuthorizationTestHelpers
    {
        public static async Task<TokenResponse?> DeserializeTokenResponse(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            var jsonDocument = JsonSerializer.Deserialize<JsonDocument>(content);

            if (jsonDocument == null)
                return null;

            var tokens = jsonDocument.RootElement;

            return new TokenResponse(
                AccessToken: tokens.GetProperty("access_token").GetString() ?? string.Empty,
                TokenType: tokens.GetProperty("token_type").GetString() ?? string.Empty,
                ExpiresIn: tokens.GetProperty("expires_in").GetInt32(),
                IdToken: tokens.GetProperty("id_token").GetString() ?? string.Empty,
                RefreshToken: tokens.TryGetProperty("refresh_token", out var refreshToken) ? refreshToken.GetString() : null
            );
        }

        public static FormUrlEncodedContent CreateTokenRequestContent(Dictionary<string, string> parameters)
        {
            return new FormUrlEncodedContent(parameters);
        }
    }
}
