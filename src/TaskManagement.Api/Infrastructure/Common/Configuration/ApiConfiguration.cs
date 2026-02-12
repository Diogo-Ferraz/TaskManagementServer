using Serilog;
using TaskManagement.Api.Features.Activity.Hubs;
using TaskManagement.Api.Infrastructure.Common.Serialization;
using TaskManagement.Api.Infrastructure.Common.Settings;
using TaskManagement.Api.Infrastructure.ExceptionHandling;

namespace TaskManagement.Api.Infrastructure.Common.Configuration
{
    public static class ApiConfiguration
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new OptionalJsonConverterFactory());
                });
            services.AddEndpointsApiExplorer();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();
            services.AddHealthChecks();

            return services;
        }

        public static WebApplication ConfigureRequestPipeline(this WebApplication app, IWebHostEnvironment env)
        {
            app.UseForwardedHeaders();
            app.UseRouting();

            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    var clientSettings = app.Configuration.GetSection("ClientSettings").Get<ClientSettings>();
                    if (clientSettings == null)
                    {
                        throw new InvalidOperationException("Client settings is not properly set in the appsettings.json file.");
                    }

                    var swaggerClient = clientSettings.Clients.FirstOrDefault(x => x.ClientId == "swagger-client");
                    if (swaggerClient == null)
                    {
                        throw new InvalidOperationException("The client swagger-client is not configured properly in the appsettings.json file.");
                    }

                    options.OAuthClientId(swaggerClient.ClientId);
                    options.OAuthUsePkce();
                });
            }

            app.MapHealthChecks("/health");
            app.UseSerilogRequestLogging();
            app.UseExceptionHandler();
            app.UseStatusCodePages();
            app.UseCors();
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.StartsWithSegments("/hubs/activity")
                    && string.IsNullOrWhiteSpace(context.Request.Headers.Authorization)
                    && context.Request.Query.TryGetValue("access_token", out var accessToken))
                {
                    context.Request.Headers.Authorization = $"Bearer {accessToken}";
                }

                await next();
            });
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHub<ActivityHub>("/hubs/activity", options =>
            {
                options.CloseOnAuthenticationExpiration = true;
            });

            return app;
        }
    }
}
