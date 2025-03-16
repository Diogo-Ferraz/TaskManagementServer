using FluentValidation;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Mappings;
using TaskManagement.Api.Features.Projects.Repositories;
using TaskManagement.Api.Features.Projects.Repositories.Interfaces;

namespace TaskManagement.Api.Features.Projects.Configuration
{
    public static class ProjectsConfiguration
    {
        public static IServiceCollection AddProjectsFeature(this IServiceCollection services)
        {
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddAutoMapper(typeof(ProjectMappingProfile).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
            services.AddValidatorsFromAssembly(typeof(CreateProjectCommandValidator).Assembly);

            return services;
        }
    }
}
