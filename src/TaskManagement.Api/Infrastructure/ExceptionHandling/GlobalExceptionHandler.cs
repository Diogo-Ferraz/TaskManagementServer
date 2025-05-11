using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskManagement.Api.Infrastructure.Common.Exceptions;

namespace TaskManagement.Api.Infrastructure.ExceptionHandling
{
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;
        private readonly IHostEnvironment _env;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
        {
            _logger = logger;
            _env = env;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "ExceptionHandler caught exception: {ErrorMessage}", exception.Message);

            int statusCode = StatusCodes.Status500InternalServerError;
            string title = "An unexpected error occurred.";
            string detail = exception.Message;
            string type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
            IDictionary<string, object?> extensions = null;

            switch (exception)
            {
                case ValidationException validationEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    title = "Validation Error";
                    detail = "One or more validation errors occurred.";
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    extensions = validationEx.Errors.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value);
                    break;

                case NotFoundException notFoundEx:
                    statusCode = StatusCodes.Status404NotFound;
                    title = "Resource Not Found";
                    detail = notFoundEx.Message;
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.4";
                    break;

                case ForbiddenAccessException forbiddenEx:
                    statusCode = StatusCodes.Status403Forbidden;
                    title = "Forbidden";
                    detail = forbiddenEx.Message;
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.3";
                    break;

                case UnauthorizedAccessException unauthEx:
                    statusCode = StatusCodes.Status401Unauthorized;
                    title = "Unauthorized";
                    detail = unauthEx.Message;
                    type = "https://tools.ietf.org/html/rfc7235#section-3.1";
                    break;

                case AppException appEx:
                    statusCode = appEx.StatusCode;
                    title = "Internal Server Error";
                    detail = appEx.Message;
                    break;
            }

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = _env.IsDevelopment() ? $"{detail} | StackTrace: {exception.StackTrace}" : detail,
                Type = type,
                Instance = httpContext.Request.Path
            };

            if (extensions != null)
            {
                foreach (var ext in extensions)
                {
                    problemDetails.Extensions.Add(ext.Key, ext.Value);
                }
            }

            httpContext.Response.StatusCode = statusCode;
            httpContext.Response.ContentType = "application/problem+json";
            await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

            return true;
        }
    }
}
