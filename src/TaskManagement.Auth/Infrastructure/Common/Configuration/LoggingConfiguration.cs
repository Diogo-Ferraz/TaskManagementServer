using Serilog;

namespace TaskManagement.Auth.Infrastructure.Common.Configuration
{
    public static class LoggingConfiguration
    {
        public static IHostBuilder AddLoggingConfiguration(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.UseSerilog((context, config) =>
                config.ReadFrom.Configuration(configuration)
                      .Enrich.FromLogContext());

            return hostBuilder;
        }
    }
}