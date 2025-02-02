using AutoMapper;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.Projects.Queries;
using TaskManagement.Api.Domain.Common;

namespace TaskManagement.Api.Application.Projects.Commands.Handlers
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
