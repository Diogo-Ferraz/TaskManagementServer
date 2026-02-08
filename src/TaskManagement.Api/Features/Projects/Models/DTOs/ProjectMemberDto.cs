namespace TaskManagement.Api.Features.Projects.Models.DTOs
{
    public class ProjectMemberDto
    {
        public string UserId { get; set; } = string.Empty;
        public bool IsOwner { get; set; }
    }
}
