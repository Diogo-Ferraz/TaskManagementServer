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
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _env = env ?? throw new ArgumentNullException(nameof(env));
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            _logger.LogError(exception, "An unhandled exception occurred. Path: {Path}, Message: {ErrorMessage}",
                httpContext.Request.Path, exception.Message);

            ProblemDetails problemDetails = CreateProblemDetails(httpContext, exception);

            httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
            httpContext.Response.ContentType = "application/problem+json";

            await httpContext.Response.WriteAsJsonAsync(problemDetails, problemDetails.GetType(), cancellationToken: cancellationToken);

            return true;
        }

        private ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
        {
            int statusCode;
            string title;
            string detail;
            string type;
            IDictionary<string, string[]> errors = null;

            switch (exception)
            {
                case ValidationException customValidationEx:
                    statusCode = StatusCodes.Status400BadRequest;
                    title = "Validation Error";
                    detail = customValidationEx.Message;
                    type = "https://tools.ietf.org/html/rfc7231#section-6.5.1";
                    errors = customValidationEx.Errors;

                    return new ValidationProblemDetails(errors)
                    {
                        Status = statusCode,
                        Title = title,
                        Detail = detail,
                        Type = type,
                        Instance = httpContext.Request.Path
                    };

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

                default:
                    statusCode = StatusCodes.Status500InternalServerError;
                    title = "An Internal Server Error Occurred";
                    detail = _env.IsDevelopment()
                        ? $"An unexpected error occurred: {exception.Message} | StackTrace: {exception.StackTrace}"
                        : "An unexpected error occurred. Please try again later.";
                    type = "https://tools.ietf.org/html/rfc7231#section-6.6.1";
                    break;
            }

            return new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Type = type,
                Instance = httpContext.Request.Path
            };
        }
    }
}