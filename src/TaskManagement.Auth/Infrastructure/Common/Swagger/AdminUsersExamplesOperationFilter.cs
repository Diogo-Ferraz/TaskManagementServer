using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TaskManagement.Auth.Infrastructure.Common.Swagger
{
    /// <summary>
    /// Adds practical Swagger examples for admin user-management endpoints.
    /// </summary>
    public sealed class AdminUsersExamplesOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation == null || context?.ApiDescription == null)
            {
                return;
            }

            var path = context.ApiDescription.RelativePath?.Trim('/').ToLowerInvariant() ?? string.Empty;
            var method = context.ApiDescription.HttpMethod?.ToUpperInvariant() ?? string.Empty;

            if (method == "PATCH" && path == "api/users/{id}/status")
            {
                if (operation.RequestBody?.Content?.TryGetValue("application/json", out var mediaType) == true)
                {
                    mediaType.Example = new OpenApiObject
                    {
                        ["isActive"] = new OpenApiBoolean(false)
                    };
                }
            }

            if (method == "GET" && path == "api/users")
            {
                SetParameterExample(operation, "search", new OpenApiString("demo"));
                SetParameterExample(operation, "isActive", new OpenApiBoolean(true));
                SetParameterExample(operation, "role", new OpenApiString("ProjectManager"));
                SetParameterExample(operation, "page", new OpenApiInteger(1));
                SetParameterExample(operation, "pageSize", new OpenApiInteger(25));
            }
        }

        private static void SetParameterExample(OpenApiOperation operation, string parameterName, IOpenApiAny example)
        {
            if (operation.Parameters == null || operation.Parameters.Count == 0)
            {
                return;
            }

            var parameter = operation.Parameters.FirstOrDefault(p =>
                string.Equals(p.Name, parameterName, StringComparison.OrdinalIgnoreCase));

            if (parameter != null)
            {
                parameter.Example = example;
            }
        }
    }
}
