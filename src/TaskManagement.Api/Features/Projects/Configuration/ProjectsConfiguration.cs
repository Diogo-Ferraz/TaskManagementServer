using FluentValidation;
using TaskManagement.Api.Features.Projects.Commands;
using TaskManagement.Api.Features.Projects.Mappings;

namespace TaskManagement.Api.Features.Projects.Configuration
{
    public static class ProjectsConfiguration
    {
        public static IServiceCollection AddProjectsFeature(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ProjectMappingProfile).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
            services.AddValidatorsFromAssembly(typeof(CreateProjectCommandValidator).Assembly);

            return services;
        }
    }
}
