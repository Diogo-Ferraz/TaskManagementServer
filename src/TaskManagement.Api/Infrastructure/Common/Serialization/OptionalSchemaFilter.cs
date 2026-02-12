using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using TaskManagement.Api.Infrastructure.Common.Models;

namespace TaskManagement.Api.Infrastructure.Common.Serialization
{
    public class OptionalSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (!context.Type.IsGenericType || context.Type.GetGenericTypeDefinition() != typeof(Optional<>))
            {
                return;
            }

            var innerType = context.Type.GetGenericArguments()[0];
            var innerSchema = context.SchemaGenerator.GenerateSchema(innerType, context.SchemaRepository);

            schema.Type = innerSchema.Type;
            schema.Format = innerSchema.Format;
            schema.Nullable = true;
            schema.Reference = innerSchema.Reference;
            schema.Properties = innerSchema.Properties;
            schema.Required = innerSchema.Required;
            schema.Items = innerSchema.Items;
            schema.Enum = innerSchema.Enum;
            schema.AdditionalPropertiesAllowed = innerSchema.AdditionalPropertiesAllowed;
            schema.AdditionalProperties = innerSchema.AdditionalProperties;
            schema.AllOf = innerSchema.AllOf;
            schema.OneOf = innerSchema.OneOf;
            schema.AnyOf = innerSchema.AnyOf;
            schema.Description = innerSchema.Description;
        }
    }
}
