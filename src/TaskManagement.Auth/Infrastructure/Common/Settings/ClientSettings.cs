namespace TaskManagement.Auth.Infrastructure.Common.Settings
{
    public class ClientSettings
    {
        public List<ClientSettingsOptions> Clients { get; set; } = [];
    }

    public class ClientSettingsOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public List<string> RedirectUris { get; set; } = [];
        public List<string> PostLogoutRedirectUris { get; set; } = [];
        public List<string> AllowedScopes { get; set; } = [];
        public List<string> AllowedCorsOrigins { get; set; } = [];
    }
}
