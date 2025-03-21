using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectsForUserQuery : IRequest<Result<IReadOnlyList<ProjectDto>>>
    {
        public string UserId { get; set; } = string.Empty;
    }
}
