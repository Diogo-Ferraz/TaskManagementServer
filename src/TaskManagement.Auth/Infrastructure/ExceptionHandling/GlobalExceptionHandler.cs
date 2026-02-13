using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace TaskManagement.Auth.Infrastructure.ExceptionHandling
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionHandler(
            ILogger<GlobalExceptionHandler> logger,
            IHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (!httpContext.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            _logger.LogError(exception, "Unhandled API exception on path {Path}", httpContext.Request.Path);

            var problem = CreateProblemDetails(httpContext, exception);
            httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
            return true;
        }

        private ProblemDetails CreateProblemDetails(HttpContext context, Exception exception)
        {
            var (status, title, detail) = exception switch
            {
                UnauthorizedAccessException => (
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized",
                    "Authentication is required to access this resource."),

                InvalidOperationException => (
                    StatusCodes.Status400BadRequest,
                    "Bad Request",
                    exception.Message),

                _ => (
                    StatusCodes.Status500InternalServerError,
                    "An Internal Server Error Occurred",
                    _environment.IsDevelopment()
                        ? exception.Message
                        : "An unexpected error occurred.")
            };

            return new ProblemDetails
            {
                Type = $"https://httpstatuses.com/{status}",
                Title = title,
                Status = status,
                Detail = detail,
                Instance = context.Request.Path
            };
        }
    }
}
