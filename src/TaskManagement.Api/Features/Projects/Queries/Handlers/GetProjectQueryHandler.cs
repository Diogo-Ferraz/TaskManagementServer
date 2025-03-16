using AutoMapper;
using MediatR;
using TaskManagement.Api.Features.Projects.Models.DTOs;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Features.Projects.Queries.Handlers
{
    public class GetProjectQueryHandler : IRequestHandler<GetProjectQuery, Result<ProjectDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IMapper _mapper;

        public GetProjectQueryHandler(IProjectRepository projectRepository, IMapper mapper)
        {
            _projectRepository = projectRepository;
            _mapper = mapper;
        }

        public async Task<Result<ProjectDto>> Handle(GetProjectQuery request, CancellationToken cancellationToken)
        {
            var project = await _projectRepository.GetByIdAsync(request.Id);
            if (project == null)
            {
                return Result<ProjectDto>.Failure("Project not found");
            }

            var projectDto = _mapper.Map<ProjectDto>(project);
            return Result<ProjectDto>.Success(projectDto);
        }
    }
}
