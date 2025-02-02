using MediatR;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.Projects.Queries
{
    public class GetProjectsForUserQuery : IRequest<Result<IReadOnlyList<ProjectDto>>>
    {
        public string UserId { get; set; }
    }
}
