using Serilog;

namespace TaskManagement.Api.Infrastructure.Logging
{
    public static class LoggingSetup
    {
        public static ILoggingBuilder AddCustomLogging(
            this ILoggingBuilder builder,
            IConfiguration configuration)
        {
            builder.ClearProviders();

            builder.AddConsole();

            builder.AddDebug();

            var logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .Enrich.FromLogContext()
                .CreateLogger();

            builder.AddSerilog(logger);

            return builder;
        }
    }
}
