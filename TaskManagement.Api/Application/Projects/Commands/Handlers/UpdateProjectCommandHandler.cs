using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Projects.Commands.Handlers
{
    public class UpdateProjectCommandHandler : IRequestHandler<UpdateProjectCommand, Result<ProjectDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidator<UpdateProjectCommand> _validator;

        public UpdateProjectCommandHandler(
            IProjectRepository projectRepository,
            IUserService userService,
            IMapper mapper,
            IValidator<UpdateProjectCommand> validator)
        {
            _projectRepository = projectRepository;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Result<ProjectDto>> Handle(UpdateProjectCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<ProjectDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var project = await _projectRepository.GetByIdAsync(request.Id);
            if (project == null)
            {
                return Result<ProjectDto>.Failure("Project not found");
            }

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user?.Role != UserRole.ProjectManager)
            {
                return Result<ProjectDto>.Failure("User is not authorized to update projects");
            }

            if (project.UserId != request.UserId)
            {
                return Result<ProjectDto>.Failure("User is not authorized to update this project");
            }

            _mapper.Map(request, project);
            project.LastModifiedBy = request.UserId;

            await _projectRepository.UpdateAsync(project);

            var projectDto = _mapper.Map<ProjectDto>(project);
            projectDto.UserName = user.UserName;

            return Result<ProjectDto>.Success(projectDto);
        }
    }
}
