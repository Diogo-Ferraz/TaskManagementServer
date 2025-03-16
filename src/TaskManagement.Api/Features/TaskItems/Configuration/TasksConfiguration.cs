using FluentValidation;
using TaskManagement.Api.Features.TaskItems.Commands;
using TaskManagement.Api.Features.TaskItems.Mappings;
using TaskManagement.Api.Features.TaskItems.Repositories;
using TaskManagement.Api.Features.TaskItems.Repositories.Interfaces;

namespace TaskManagement.Api.Features.TaskItems.Configuration
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
