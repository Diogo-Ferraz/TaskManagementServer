using TaskManagement.Api.Features.Dashboard.Queries;

namespace TaskManagement.Api.Features.Dashboard.Configuration
{
    public static class DashboardConfiguration
    {
        public static IServiceCollection AddDashboardFeature(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(GetDashboardSummaryQuery).Assembly));
            return services;
        }
    }
}
