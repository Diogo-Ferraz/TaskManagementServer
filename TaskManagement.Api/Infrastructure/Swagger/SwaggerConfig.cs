using Microsoft.OpenApi.Models;

namespace TaskManagement.Api.Infrastructure.Swagger
{
    public static class SwaggerConfig
    {
        public static IServiceCollection AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(options =>
            {
                options.ResolveConflictingActions(apiDescriptions => apiDescriptions.First());

                options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.OAuth2,
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = new Uri("https://localhost:44377/connect/authorize"),
                            TokenUrl = new Uri("https://localhost:44377/connect/token"),
                            Scopes = new Dictionary<string, string>
                            {
                                { "profile", "Access your profile information" },
                                { "email", "Access your email address" },
                                { "roles", "Access your role information" }
                            }
                        }
                    },
                    Description = "OAuth2 Authorization Code Flow with PKCE"
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
