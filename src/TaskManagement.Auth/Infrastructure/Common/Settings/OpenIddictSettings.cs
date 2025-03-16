namespace TaskManagement.Auth.Infrastructure.Common.Settings
{
    public class OpenIddictSettings
    {
        public string Audience { get; set; } = string.Empty;
        public string EncryptionKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
    }
}