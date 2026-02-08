using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectMembersQuery : IRequest<IReadOnlyList<ProjectMemberDto>>
    {
        public Guid ProjectId { get; set; }
    }
}
