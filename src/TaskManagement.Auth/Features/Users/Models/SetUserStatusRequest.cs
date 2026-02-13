namespace TaskManagement.Auth.Features.Users.Models
{
    /// <summary>
    /// Request payload for changing a user's active status.
    /// </summary>
    public class SetUserStatusRequest
    {
        /// <summary>
        /// True to activate the user; false to deactivate.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
