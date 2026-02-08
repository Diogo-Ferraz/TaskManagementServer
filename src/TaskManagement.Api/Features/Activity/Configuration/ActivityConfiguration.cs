using TaskManagement.Api.Features.Activity.Mappings;
using TaskManagement.Api.Features.Activity.Queries;
using TaskManagement.Api.Features.Activity.Services;
using TaskManagement.Api.Features.Activity.Services.Interfaces;
using TaskManagement.Api.Features.Projects.Services;
using TaskManagement.Api.Features.Projects.Services.Interfaces;

namespace TaskManagement.Api.Features.Activity.Configuration
{
    public static class ActivityConfiguration
    {
        public static IServiceCollection AddActivityFeature(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(ActivityMappingProfile).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetActivityFeedQuery).Assembly));
            services.AddSignalR();
            services.AddScoped<IActivityPublisher, SignalRActivityPublisher>();
            services.AddScoped<IProjectMembershipService, ProjectMembershipService>();

            return services;
        }
    }
}
