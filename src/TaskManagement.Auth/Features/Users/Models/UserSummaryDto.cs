namespace TaskManagement.Auth.Features.Users.Models
{
    public class UserSummaryDto
    {
        public string Id { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public string? UserName { get; set; }
        public string? Email { get; set; }
        public bool IsActive { get; set; }
        public List<string> Roles { get; set; } = [];
    }
}
