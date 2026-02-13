namespace TaskManagement.Auth.Features.Users.Models
{
    /// <summary>
    /// Admin-facing user details payload.
    /// </summary>
    public class UserDetailsDto
    {
        /// <summary>
        /// User identifier.
        /// </summary>
        public string Id { get; set; } = string.Empty;
        /// <summary>
        /// Display name used in UI.
        /// </summary>
        public string? DisplayName { get; set; }
        /// <summary>
        /// Username used for sign in.
        /// </summary>
        public string? UserName { get; set; }
        /// <summary>
        /// User email.
        /// </summary>
        public string? Email { get; set; }
        /// <summary>
        /// Indicates whether the account is active.
        /// </summary>
        public bool IsActive { get; set; }
        /// <summary>
        /// Indicates whether email is confirmed.
        /// </summary>
        public bool EmailConfirmed { get; set; }
        /// <summary>
        /// User phone number.
        /// </summary>
        public string? PhoneNumber { get; set; }
        /// <summary>
        /// Indicates whether phone number is confirmed.
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }
        /// <summary>
        /// Indicates whether two-factor authentication is enabled.
        /// </summary>
        public bool TwoFactorEnabled { get; set; }
        /// <summary>
        /// Lockout end timestamp when applicable.
        /// </summary>
        public DateTimeOffset? LockoutEnd { get; set; }
        /// <summary>
        /// Failed access attempts counter.
        /// </summary>
        public int AccessFailedCount { get; set; }
        /// <summary>
        /// Assigned role names.
        /// </summary>
        public List<string> Roles { get; set; } = [];
    }
}
