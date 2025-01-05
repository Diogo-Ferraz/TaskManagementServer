using Serilog;

namespace TaskManagement.Api.Infrastructure.Configurations
{
    public static class ServicesConfiguration
    {
        public static IHostBuilder AddSerilogLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.UseSerilog((context, config) =>
                config.ReadFrom.Configuration(configuration)
                      .Enrich.FromLogContext());
            return hostBuilder;
        }
    }
}
