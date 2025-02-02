using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.Projects.DTOs;
using TaskManagement.Api.Domain.Common;
using TaskManagement.Api.Domain.Entities;

namespace TaskManagement.Api.Infrastructure.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IUserService _userService;

        public ProjectService(IProjectRepository projectRepository, IUserService userService)
        {
            _projectRepository = projectRepository;
            _userService = userService;
        }

        public async Task<Result<ProjectDto>> CreateProjectAsync(CreateProjectCommand command)
        {
            var admin = await _userService.GetUserByIdAsync(command.UserId);
            if (admin?.Role != UserRole.ProjectManager)
            {
                return Result<ProjectDto>.Failure("User is not a project admin");
            }

            var project = new Project
            {
                Name = command.Name,
                Description = command.Description,
                UserId = command.UserId,
                CreatedBy = command.UserId,
                LastModifiedBy = command.UserId
            };

            await _projectRepository.AddAsync(project);

            return Result<ProjectDto>.Success(new ProjectDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                UserId = project.UserId
            });
        }

        Task<Result<bool>> IProjectService.DeleteProjectAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        Task<Result<ProjectDto>> IProjectService.GetProjectAsync(Guid id)
        {
            throw new NotImplementedException();
        }

        Task<Result<IReadOnlyList<ProjectDto>>> IProjectService.GetProjectsByUserAsync(string adminId)
        {
            throw new NotImplementedException();
        }

        Task<Result<ProjectDto>> IProjectService.UpdateProjectAsync(UpdateProjectCommand command)
        {
            throw new NotImplementedException();
        }
    }
}
