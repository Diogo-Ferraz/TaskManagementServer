using FluentValidation;
using TaskManagement.Api.Features.Tasks.Commands;
using TaskManagement.Api.Features.Tasks.Mappings;
using TaskManagement.Api.Features.Tasks.Repositories;
using TaskManagement.Api.Features.Tasks.Repositories.Interfaces;

namespace TaskManagement.Api.Features.Tasks.Configuration
{
    public static class TasksConfiguration
    {
        public static IServiceCollection AddTasksFeature(this IServiceCollection services)
        {
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddAutoMapper(typeof(TaskItemMappingProfile).Assembly);
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTaskItemCommand).Assembly));
            services.AddValidatorsFromAssembly(typeof(CreateTaskItemCommandValidator).Assembly);

            return services;
        }
    }
}
