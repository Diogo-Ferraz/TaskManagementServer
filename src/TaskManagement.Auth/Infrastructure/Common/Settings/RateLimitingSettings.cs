namespace TaskManagement.Auth.Infrastructure.Common.Settings
{
    public sealed class RateLimitingSettings
    {
        public FixedWindowPolicySettings AdminUserManagement { get; set; } = new();
        public FixedWindowPolicySettings TokenExchange { get; set; } = new();
    }

    public sealed class FixedWindowPolicySettings
    {
        public int PermitLimit { get; set; } = 60;
        public int WindowSeconds { get; set; } = 60;
        public int QueueLimit { get; set; } = 0;
    }

    public static class RateLimitingPolicies
    {
        public const string AdminUserManagement = nameof(AdminUserManagement);
        public const string TokenExchange = nameof(TokenExchange);
    }
}
