using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.Common.Interfaces
{
    public interface IProjectService
    {
        Task<Result<ProjectDto>> CreateProjectAsync(CreateProjectCommand command);
        Task<Result<ProjectDto>> UpdateProjectAsync(UpdateProjectCommand command);
        Task<Result<bool>> DeleteProjectAsync(Guid id);
        Task<Result<ProjectDto>> GetProjectAsync(Guid id);
        Task<Result<IReadOnlyList<ProjectDto>>> GetProjectsByUserAsync(string adminId);
    }
}
