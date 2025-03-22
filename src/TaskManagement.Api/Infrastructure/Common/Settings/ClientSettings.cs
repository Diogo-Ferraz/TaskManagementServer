namespace TaskManagement.Api.Infrastructure.Common.Settings
{
    public class ClientSettings
    {
        public List<ClientSettingsOptions> Clients { get; set; } = [];
        public string AuthorizationUrl { get; set; } = string.Empty;
        public string TokenUrl { get; set; } = string.Empty;
    }

    public class ClientSettingsOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public bool RequirePkce { get; set; } = true;
        public Dictionary<string, string> Scopes { get; set; } = [];
        public List<string> AllowedCorsOrigins { get; set; } = [];
        public string Description { get; set; } = string.Empty;
    }
}
