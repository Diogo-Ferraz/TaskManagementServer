using Microsoft.IdentityModel.Tokens;
using OpenIddict.Validation.AspNetCore;

namespace TaskManagement.Api.Infrastructure.Configurations.OpenIddict
{
    public static class OpenIddictConfig
    {
        public static IServiceCollection AddOpenIddictConfig(this IServiceCollection services, WebApplicationBuilder builder)
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

            builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            builder.Services.AddAuthorization();

            return services;
        }
    }
}
