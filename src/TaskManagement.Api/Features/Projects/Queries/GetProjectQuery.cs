using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Queries
{
    public class GetProjectQuery : IRequest<Result<ProjectDto>>
    {
        public Guid Id { get; set; }
    }
}
