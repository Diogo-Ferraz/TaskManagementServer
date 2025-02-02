using MediatR;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.Projects.Queries
{
    public class GetProjectQuery : IRequest<Result<ProjectDto>>
    {
        public Guid Id { get; set; }
    }
}
