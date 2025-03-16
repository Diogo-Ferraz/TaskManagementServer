namespace TaskManagement.Auth.Infrastructure.Common.Settings
{
    public class ClientSettings
    {
        public List<ClientSettingsOptions> Clients { get; set; } = [];
    }

    public class ClientSettingsOptions
    {
        public string ClientId { get; set; } = string.Empty;
        public string ClientSecret { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string RedirectUri { get; set; } = string.Empty;
        public string PostLogoutRedirectUri { get; set; } = string.Empty;
    }
}
