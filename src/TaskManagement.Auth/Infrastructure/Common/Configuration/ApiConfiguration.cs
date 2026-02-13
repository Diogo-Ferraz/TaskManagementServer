using Microsoft.AspNetCore.Mvc;
using Serilog;
using TaskManagement.Auth.Infrastructure.ExceptionHandling;

namespace TaskManagement.Auth.Infrastructure.Common.Configuration
{
    public static class ApiConfiguration
    {
        public static IServiceCollection AddApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerConfiguration();
            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        public static WebApplication ConfigureRequestPipeline(this WebApplication app, IWebHostEnvironment environment)
        {
            app.UseForwardedHeaders();

            if (environment.IsDevelopment() || environment.IsEnvironment("Testing"))
            {
                app.UseMigrationsEndPoint();
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "TaskManagement.Auth v1");
                    options.RoutePrefix = "swagger";
                });
            }
            else
            {
                app.UseStatusCodePagesWithReExecute("~/error");
            }

            app.UseExceptionHandler();
            app.MapHealthChecks("/health");
            app.UseSerilogRequestLogging();
            app.UseStaticFiles();
            app.UseCors();
            app.UseRouting();
            app.Use(async (context, next) =>
            {
                await next();

                if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                if (context.Response.HasStarted)
                {
                    return;
                }

                if (context.Response.StatusCode < 400 || context.Response.ContentLength.GetValueOrDefault() > 0)
                {
                    return;
                }

                var contentType = context.Response.ContentType ?? string.Empty;
                if (contentType.Contains("application/problem+json", StringComparison.OrdinalIgnoreCase) ||
                    contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                var problem = CreateProblemDetails(context.Response.StatusCode, context.Request.Path);
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsJsonAsync(problem);
            });
            app.UseRateLimiter();
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

        private static ProblemDetails CreateProblemDetails(int statusCode, PathString path)
        {
            return statusCode switch
            {
                StatusCodes.Status401Unauthorized => new ProblemDetails
                {
                    Type = "https://httpstatuses.com/401",
                    Title = "Unauthorized",
                    Status = StatusCodes.Status401Unauthorized,
                    Detail = "Authentication is required to access this resource.",
                    Instance = path
                },
                StatusCodes.Status403Forbidden => new ProblemDetails
                {
                    Type = "https://httpstatuses.com/403",
                    Title = "Forbidden",
                    Status = StatusCodes.Status403Forbidden,
                    Detail = "You do not have permission to perform this action.",
                    Instance = path
                },
                StatusCodes.Status404NotFound => new ProblemDetails
                {
                    Type = "https://httpstatuses.com/404",
                    Title = "Resource Not Found",
                    Status = StatusCodes.Status404NotFound,
                    Detail = "The requested resource does not exist.",
                    Instance = path
                },
                StatusCodes.Status429TooManyRequests => new ProblemDetails
                {
                    Type = "https://httpstatuses.com/429",
                    Title = "Too Many Requests",
                    Status = StatusCodes.Status429TooManyRequests,
                    Detail = "Rate limit exceeded. Please retry later.",
                    Instance = path
                },
                _ => new ProblemDetails
                {
                    Type = $"https://httpstatuses.com/{statusCode}",
                    Title = "Request Error",
                    Status = statusCode,
                    Detail = "An error occurred while processing the request.",
                    Instance = path
                }
            };
        }
    }
}
