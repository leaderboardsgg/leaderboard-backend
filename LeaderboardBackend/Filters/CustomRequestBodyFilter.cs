using System.Net.Mime;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace LeaderboardBackend.Filters;

/// <summary>
///     This filter allows us to define a custom request DTO type for the
///     OpenAPI doc to generate.
/// </summary>
public sealed class CustomRequestBodyFilter<T> : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        OpenApiSchema schema = context.SchemaGenerator.GenerateSchema(typeof(T), context.SchemaRepository);

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                [MediaTypeNames.Application.Json] = new OpenApiMediaType
                {
                    Schema = schema,
                }
            }
        };
    }
}
