namespace TaskManagement.Auth.Tests.IntegrationTests.Features.Authorization.Models
{
    public record TokenResponse(
        string AccessToken,
        string TokenType,
        int ExpiresIn,
        string IdToken,
        string? RefreshToken = null
    );
}
