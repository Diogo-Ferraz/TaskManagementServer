using Serilog;

namespace TaskManagement.Api.Infrastructure.Common.Configuration
{
    public static class LoggingConfiguration
    {
        public static WebApplicationBuilder AddLoggingConfiguration(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext(),
                true);

            return builder;
        }
    }
}
