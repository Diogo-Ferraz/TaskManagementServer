namespace TaskManagement.Api.Features.Projects.Models.DTOs
{
    public class ProjectMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public bool IsOwner { get; set; }
    }
}
