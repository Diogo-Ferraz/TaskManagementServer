using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectsForUserQuery : IRequest<IReadOnlyList<ProjectDto>>
    {
    }
}
