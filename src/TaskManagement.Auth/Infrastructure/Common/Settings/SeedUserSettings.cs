namespace TaskManagement.Auth.Infrastructure.Common.Settings
{
    public class SeedUserSettings
    {
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}
