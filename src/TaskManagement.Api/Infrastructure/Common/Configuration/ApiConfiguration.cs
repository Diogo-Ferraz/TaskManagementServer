using Serilog;
using TaskManagement.Api.Infrastructure.Common.Settings;
using TaskManagement.Api.Infrastructure.ExceptionHandling;

namespace TaskManagement.Api.Infrastructure.Common.Configuration
{
    public static class ApiConfiguration
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddEndpointsApiExplorer();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
            services.AddHealthChecks();

            return services;
        }

        public static WebApplication ConfigureRequestPipeline(this WebApplication app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    var swaggerSettings = app.Configuration.GetSection("Swagger").Get<SwaggerSettings>();
                    if (swaggerSettings == null)
                    {
                        throw new InvalidOperationException("Swagger settings are missing.");
                    }
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

            return app;
        }
    }
}
