namespace TaskManagement.Shared.DemoData
{
    public sealed record DemoIdentityUser(
        string Id,
        string Email,
        string DisplayName,
        string Role);
}
