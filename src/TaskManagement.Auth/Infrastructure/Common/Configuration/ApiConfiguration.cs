using Serilog;

namespace TaskManagement.Auth.Infrastructure.Common.Configuration
{
    public static class ApiConfiguration
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers();

            return services;
        }

        public static WebApplication ConfigureRequestPipeline(this WebApplication app, IWebHostEnvironment environment)
        {
            app.UseForwardedHeaders();

            if (environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("~/error");
            }

            app.MapHealthChecks("/health");
            app.UseSerilogRequestLogging();
            app.UseStaticFiles();
            app.UseCors();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
            app.MapRazorPages();

            return app;
        }

        public static IServiceCollection AddRazorPagesConfiguration(this IServiceCollection services)
        {
            services.AddRazorPages()
                .AddRazorOptions(options =>
                {
                    options.ViewLocationFormats.Clear();
                    options.ViewLocationFormats.Add("/Presentation/Views/{1}/{0}.cshtml");
                    options.ViewLocationFormats.Add("/Presentation/Views/Shared/{0}.cshtml");
                });

            return services;
        }
    }
}