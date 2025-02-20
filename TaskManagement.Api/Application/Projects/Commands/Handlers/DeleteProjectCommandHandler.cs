using FluentValidation;
using MediatR;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Shared.Models;

namespace TaskManagement.Api.Application.Projects.Commands.Handlers
{
    public class DeleteProjectCommandHandler : IRequestHandler<DeleteProjectCommand, Result<bool>>
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;
        private readonly IValidator<DeleteProjectCommand> _validator;

        public DeleteProjectCommandHandler(
            IProjectRepository projectRepository,
            IUserService userService,
            IValidator<DeleteProjectCommand> validator)
        {
            _projectRepository = projectRepository;
            _userService = userService;
            _validator = validator;
        }

        public async Task<Result<bool>> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                return Result<bool>.Failure(validationResult.Errors.First().ErrorMessage);
            }

            var project = await _projectRepository.GetByIdAsync(request.Id);
            if (project == null)
            {
                return Result<bool>.Failure("Project not found");
            }

            if (!await _userService.IsInRoleAsync(request.UserId, Roles.ProjectManager) || project.UserId != request.UserId)
            {
                return Result<bool>.Failure("User is not authorized to delete this project");
            }

            await _projectRepository.DeleteAsync(project);
            return Result<bool>.Success(true);
        }
    }
}
