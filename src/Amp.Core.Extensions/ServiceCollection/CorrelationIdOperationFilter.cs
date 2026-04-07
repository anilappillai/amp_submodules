using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>Adds the X-Correlation-Id header to every Swagger operation.</summary>
internal sealed class CorrelationIdOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "uuid" }
        });
    }
}
