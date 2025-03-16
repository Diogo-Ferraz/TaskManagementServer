namespace TaskManagement.Api.Infrastructure.Common.Settings
{
    public class SwaggerSettings
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
        public Dictionary<string, string> Scopes { get; set; } = new();
        public string Description { get; set; } = "OAuth2 Authorization Code Flow with PKCE";
    }
}
