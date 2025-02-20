using Serilog;
using TaskManagement.Api.Infrastructure.Configurations;
using TaskManagement.Api.Infrastructure.Configurations.OpenIddict;
using TaskManagement.Api.Infrastructure.Configurations.Swagger;
using TaskManagement.Api.Infrastructure.ExceptionHandling;
using TaskManagement.Api.Infrastructure.Logging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddSwaggerConfig(builder);
builder.Services.AddOpenIddictConfig(builder);

builder.AddCustomLogging();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

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

app.UseSerilogRequestLogging();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
