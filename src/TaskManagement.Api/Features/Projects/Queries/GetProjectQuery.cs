using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectQuery : IRequest<ProjectDto>
    {
        public Guid Id { get; set; }
    }
}
