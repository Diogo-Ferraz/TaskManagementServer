using Microsoft.EntityFrameworkCore;
using Serilog;
using TaskManagement.Api.Infrastructure.Configurations;
using TaskManagement.Api.Infrastructure.Configurations.OpenIddict;
using TaskManagement.Api.Infrastructure.Configurations.Swagger;
using TaskManagement.Api.Infrastructure.ExceptionHandling;
using TaskManagement.Api.Infrastructure.Logging;
using TaskManagement.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerConfig(builder);
builder.Services.AddOpenIddictConfig(builder);

builder.AddCustomLogging();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        logger.LogInformation("API service waiting for Auth migrations...");

        var maxRetries = 10;
        var retryCount = 0;

        while (retryCount < maxRetries)
        {
            try
            {
                var dbContext = services.GetRequiredService<TaskManagementDbContext>();
                await dbContext.Database.MigrateAsync();
                logger.LogInformation("API migrations applied successfully");
                break;
            }
            catch (Exception)
            {
                retryCount++;
                logger.LogInformation("Waiting for database to be available. Retry {RetryCount}/{MaxRetries}", retryCount, maxRetries);
                await Task.Delay(TimeSpan.FromSeconds(5));
            }
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Migration error in API service");
    }
}

if (app.Environment.IsDevelopment())
{
    var swaggerSettings = builder.Configuration.GetSection("Swagger").Get<SwaggerSettings>();
    if (swaggerSettings == null)
    {
        throw new InvalidOperationException("Swagger settings are missing.");
    }

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId(swaggerSettings.ClientId);
        options.OAuthClientSecret(swaggerSettings.ClientSecret);
        options.OAuthUsePkce();
    });
}

app.MapHealthChecks("/health");

app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
