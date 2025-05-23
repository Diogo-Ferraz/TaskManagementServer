using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Serilog;
using Serilog.Events;
using TaskManagement.Api.Features.Projects.Configuration;
using TaskManagement.Api.Features.TaskItems.Configuration;
using TaskManagement.Api.Features.Users.Configuration;
using TaskManagement.Api.Infrastructure.Common.Behaviors;
using TaskManagement.Api.Infrastructure.Common.Configuration;
using TaskManagement.Api.Infrastructure.Persistence.Configuration;
using TaskManagement.Api.Infrastructure.Security.Configuration;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting API web host");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders =
            ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    builder.Services.AddDatabaseConfiguration(builder.Configuration);
    builder.Services.AddApiConfiguration();
    builder.Services.AddCorsConfiguration(builder.Configuration);

    builder.AddLoggingConfiguration();

    builder.Services.AddSwaggerConfiguration(builder);

    builder.Services.AddOpenIddictValidation(builder.Configuration, builder.Environment);

    builder.Services.AddProjectsFeature();
    builder.Services.AddTasksFeature();
    builder.Services.AddUserFeature();

    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
    builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

    var app = builder.Build();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        await app.ApplyMigrationsAsync();
    }

    app.ConfigureRequestPipeline(builder.Environment);

    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "API host terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

// Make the implicit Program class public so test projects can access it
public partial class Program { }