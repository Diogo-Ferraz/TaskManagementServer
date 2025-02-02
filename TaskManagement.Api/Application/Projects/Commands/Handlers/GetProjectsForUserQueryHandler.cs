using AutoMapper;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Application.Projects.Queries;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Projects.Commands.Handlers
{
    public class GetProjectsForAdminQueryHandler : IRequestHandler<GetProjectsForUserQuery, Result<IReadOnlyList<ProjectDto>>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;

        public GetProjectsForAdminQueryHandler(
            IProjectRepository projectRepository,
            IUserService userService,
            IMapper mapper)
        {
            _projectRepository = projectRepository;
            _userService = userService;
            _mapper = mapper;
        }

        public async Task<Result<IReadOnlyList<ProjectDto>>> Handle(GetProjectsForUserQuery request, CancellationToken cancellationToken)
        {
            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user?.Role != UserRole.ProjectManager)
            {
                return Result<IReadOnlyList<ProjectDto>>.Failure("User is not authorized to view projects");
            }

            var projects = await _projectRepository.GetProjectsByUserIdAsync(request.UserId);
            var projectDtos = _mapper.Map<IReadOnlyList<ProjectDto>>(projects);

            return Result<IReadOnlyList<ProjectDto>>.Success(projectDtos);
        }
    }
}
