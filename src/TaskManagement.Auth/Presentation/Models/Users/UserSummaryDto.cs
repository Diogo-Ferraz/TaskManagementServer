namespace TaskManagement.Auth.Presentation.Models.Users
{
    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
    }
}
