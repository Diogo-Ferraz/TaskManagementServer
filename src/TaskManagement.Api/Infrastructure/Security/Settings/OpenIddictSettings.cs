namespace TaskManagement.Api.Infrastructure.Security.Settings
{
    public class OpenIddictSettings
    {
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public string EncryptionKey { get; set; } = string.Empty;
    }
}
