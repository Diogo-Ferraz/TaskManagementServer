using Microsoft.OpenApi.Models;

namespace TaskManagement.Api.Infrastructure.Configurations.Swagger
{
    public static class SwaggerConfig
    {
        public static IServiceCollection AddSwaggerConfig(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddSwaggerGen(options =>
            {
                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                var swaggerSettings = builder.Configuration.GetSection("Swagger").Get<SwaggerSettings>();

                if (swaggerSettings == null)
                {
                    throw new InvalidOperationException("Swagger settings are not configured properly in the appsettings.json file.");
                }

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(swaggerSettings.AuthorizationUrl),
                            TokenUrl = new Uri(swaggerSettings.TokenUrl),
                            Scopes = swaggerSettings.Scopes
                        }
                    },
                    Description = swaggerSettings.Description
                });

                options.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                        },
                        Array.Empty<string>()
                    }
                });
            });

            return services;
        }
    }
}
