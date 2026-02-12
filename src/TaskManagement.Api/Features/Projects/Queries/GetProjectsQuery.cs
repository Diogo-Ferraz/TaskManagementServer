using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectsQuery : IRequest<IReadOnlyList<ProjectDto>>
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }
}
