using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskManagement.Api.Infrastructure.Common.Swagger
{
    /// <summary>
    /// Adds standardized ProblemDetails examples for common error status codes.
    /// </summary>
    public sealed class ProblemDetailsExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Responses == null || operation.Responses.Count == 0)
            {
                return;
            }

            AddExample(operation, "400", "Validation Error", "One or more validation errors occurred.", includeErrors: true);
            AddExample(operation, "401", "Unauthorized", "Authentication is required to access this resource.");
            AddExample(operation, "403", "Forbidden", "You do not have permission to perform this action.");
            AddExample(operation, "404", "Resource Not Found", "The requested resource does not exist.");
            AddExample(operation, "429", "Too Many Requests", "Rate limit exceeded. Please retry later.");
            AddExample(operation, "500", "An Internal Server Error Occurred", "An unexpected error occurred.");
        }

        private static void AddExample(
            OpenApiOperation operation,
            string statusCode,
            string title,
            string detail,
            bool includeErrors = false)
        {
            if (!operation.Responses.TryGetValue(statusCode, out var response) || response.Content == null)
            {
                return;
            }

            if (!response.Content.TryGetValue("application/problem+json", out var mediaType))
            {
                mediaType = new OpenApiMediaType();
                response.Content["application/problem+json"] = mediaType;
            }

            mediaType.Example = CreateProblemExample(statusCode, title, detail, includeErrors);
        }

        private static OpenApiObject CreateProblemExample(string statusCode, string title, string detail, bool includeErrors)
        {
            var example = new OpenApiObject
            {
                ["type"] = new OpenApiString($"https://httpstatuses.com/{statusCode}"),
                ["title"] = new OpenApiString(title),
                ["status"] = new OpenApiInteger(int.Parse(statusCode)),
                ["detail"] = new OpenApiString(detail),
                ["instance"] = new OpenApiString("/api/resource")
            };

            if (includeErrors)
            {
                example["errors"] = new OpenApiObject
                {
                    ["fieldName"] = new OpenApiArray
                    {
                        new OpenApiString("Example validation message.")
                    }
                };
            }

            return example;
        }
    }
}
