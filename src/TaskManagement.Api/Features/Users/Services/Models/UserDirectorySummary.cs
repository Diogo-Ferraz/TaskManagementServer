namespace TaskManagement.Api.Features.Users.Services.Models
{
    public sealed class UserDirectorySummary
    {
        public string? DisplayName { get; init; }
        public string? Email { get; init; }
        public IReadOnlyCollection<string> Roles { get; init; } = Array.Empty<string>();
    }
}
