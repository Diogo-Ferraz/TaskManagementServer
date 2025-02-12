using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Api.Application.Common.Interfaces;
using TaskManagement.Api.Application.Common.Mappings;
using TaskManagement.Api.Application.Projects.Commands;
using TaskManagement.Api.Application.TaskItems.Commands;
using TaskManagement.Api.Domain.Entities;
using TaskManagement.Api.Infrastructure.Persistence;
using TaskManagement.Api.Infrastructure.Repositories;
using TaskManagement.Api.Infrastructure.Services;

namespace TaskManagement.Api.Infrastructure.Configurations
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<TaskManagementDbContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("TaskManagementDbConnection")));

            services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<TaskManagementDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
            services.AddScoped<IProjectRepository, ProjectRepository>();
            services.AddScoped<ITaskItemRepository, TaskItemRepository>();
            services.AddScoped<IUserService, UserService>();

            services.AddAutoMapper(typeof(ProjectMappingProfile).Assembly);
            services.AddAutoMapper(typeof(TaskItemMappingProfile).Assembly);

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateProjectCommand).Assembly));
            services.AddValidatorsFromAssembly(typeof(CreateProjectCommandValidator).Assembly);

            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(CreateTaskItemCommand).Assembly));
            services.AddValidatorsFromAssembly(typeof(CreateTaskItemCommandValidator).Assembly);

            return services;
        }
    }
}
