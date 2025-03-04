namespace TaskManagement.Auth.Infrastructure.Configurations.OpenIddict
{
    public class OpenIddictSettings
    {
        public string Audience { get; set; } = string.Empty;
        public string EncryptionKey { get; set; } = string.Empty;
    }
}