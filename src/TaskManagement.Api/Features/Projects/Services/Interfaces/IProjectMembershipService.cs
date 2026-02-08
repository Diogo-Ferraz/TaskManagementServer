namespace TaskManagement.Api.Features.Projects.Services.Interfaces
{
    public interface IProjectMembershipService
    {
        Task<IReadOnlyList<Guid>> GetProjectIdsForUserAsync(string userId, CancellationToken cancellationToken);
        Task<bool> IsMemberAsync(Guid projectId, string userId, CancellationToken cancellationToken);
    }
}
