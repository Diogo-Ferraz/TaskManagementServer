using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;
using TaskManagement.Api.Infrastructure.Security.Settings;

namespace TaskManagement.Api.Infrastructure.Security.Configuration
{
    public static class SecurityConfiguration
    {
        public static IServiceCollection AddOpenIddictValidation(this IServiceCollection services, WebApplicationBuilder builder)
        {
            var openIddictSettings = builder.Configuration.GetSection("OpenIddict").Get<OpenIddictSettings>();
            if (openIddictSettings == null)
            {
                throw new InvalidOperationException("OpenIddict settings are missing.");
            }

            services.AddOpenIddict()
                .AddValidation(options =>
                {
                    options.SetIssuer(openIddictSettings.Issuer);
                    options.AddAudiences(openIddictSettings.Audience);
                    options.AddEncryptionKey(new SymmetricSecurityKey(
                        Convert.FromBase64String(openIddictSettings.EncryptionKey)));
                    options.UseSystemNetHttp()
                    .ConfigureHttpClientHandler(handler =>
                    {
                        if (builder.Environment.IsDevelopment())
                        {
                            handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
                        }
                    });
                    options.UseAspNetCore();
                });

            services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            services.AddAuthorization();

            return services;
        }
    }
}
