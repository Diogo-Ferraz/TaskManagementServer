using Serilog;
using TaskManagement.Api.Infrastructure.Configurations;
using TaskManagement.Api.Infrastructure.Configurations.OpenIddict;
using TaskManagement.Api.Infrastructure.Configurations.Swagger;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilogLogging(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerConfig(builder);
builder.Services.AddOpenIddictConfig(builder);

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

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
