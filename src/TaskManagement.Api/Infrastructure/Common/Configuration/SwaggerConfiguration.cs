using Microsoft.OpenApi.Models;
using TaskManagement.Api.Infrastructure.Common.Settings;

namespace TaskManagement.Api.Infrastructure.Common.Configuration
{
    public static class SwaggerConfiguration
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, WebApplicationBuilder builder)
        {
            services.AddSwaggerGen(options =>
            {
                var clientSettings = builder.Configuration.GetSection("ClientSettings").Get<ClientSettings>();
                if (clientSettings == null)
                {
                    throw new InvalidOperationException("Client settings is not properly set in the appsettings.json file.");
                }
                var swaggerClient = clientSettings.Clients.FirstOrDefault(x => x.ClientId == "swagger-client");
                if (swaggerClient == null)
                {
                    throw new InvalidOperationException("The client swagger-client is not configured properly in the appsettings.json file.");
                }

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri(clientSettings.AuthorizationUrl),
                            TokenUrl = new Uri(clientSettings.TokenUrl),
                            Scopes = swaggerClient.Scopes
                        }
                    },
                    Description = swaggerClient.Description
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
