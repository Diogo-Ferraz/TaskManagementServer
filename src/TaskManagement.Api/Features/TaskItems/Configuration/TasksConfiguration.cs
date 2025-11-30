using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Mappings;

namespace TaskManagement.Api.Features.TaskItems.Configuration
{
    public static class TasksConfiguration
    {
        public static IServiceCollection AddTasksFeature(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(TaskItemMappingProfile).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTaskItemCommand).Assembly));

            return services;
        }
    }
}
