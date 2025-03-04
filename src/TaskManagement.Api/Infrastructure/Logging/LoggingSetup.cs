using Serilog;

namespace TaskManagement.Api.Infrastructure.Logging
{
    public static class LoggingSetup
    {
        public static WebApplicationBuilder AddCustomLogging(this WebApplicationBuilder builder)
        {
            builder.Host.UseSerilog((context, config) =>
                config.ReadFrom.Configuration(builder.Configuration)
                      .Enrich.FromLogContext());

            return builder;
        }
    }
}
