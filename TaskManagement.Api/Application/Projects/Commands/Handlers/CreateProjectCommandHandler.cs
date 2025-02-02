using AutoMapper;
using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Application.Projects.Commands.Handlers
{
    public class CreateProjectCommandHandler : IRequestHandler<CreateProjectCommand, Result<ProjectDto>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IMapper _mapper;
        private readonly IValidator<CreateProjectCommand> _validator;

        public CreateProjectCommandHandler(
            IProjectRepository projectRepository,
            IUserService userService,
            IMapper mapper,
            IValidator<CreateProjectCommand> validator)
        {
            _projectRepository = projectRepository;
            _userService = userService;
            _mapper = mapper;
            _validator = validator;
        }

        public async Task<Result<ProjectDto>> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<ProjectDto>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var user = await _userService.GetUserByIdAsync(request.UserId);
            if (user?.Role != UserRole.ProjectManager)
            {
                return Result<ProjectDto>.Failure("User is not authorized to create projects");
            }

            var project = _mapper.Map<Project>(request);
            project.CreatedBy = request.UserId;
            project.LastModifiedBy = request.UserId;

            await _projectRepository.AddAsync(project);

            var projectDto = _mapper.Map<ProjectDto>(project);
            projectDto.UserName = user.UserName;

            return Result<ProjectDto>.Success(projectDto);
        }
    }

}
