using Serilog;
using Serilog.Events;
using TaskManagement.Auth.Features.Authorization.Configuration;
using TaskManagement.Auth.Features.Identity.Configuration;
using TaskManagement.Auth.Infrastructure.Common.Configuration;
using TaskManagement.Auth.Infrastructure.Persistence.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDatabaseConfiguration(builder.Configuration);
    builder.Services.AddApiConfiguration();
    builder.Services.AddCorsConfiguration(builder.Configuration);
    builder.Host.AddLoggingConfiguration(builder.Configuration);

    builder.Services.AddIdentityConfiguration();
    builder.Services.AddAuthorizationConfiguration(builder);

    builder.Services.AddRazorPagesConfiguration();

    var app = builder.Build();

    await app.ApplyMigrationsAndSeedDataAsync();

    app.ConfigureRequestPipeline(builder.Environment);

    app.Run();

    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }