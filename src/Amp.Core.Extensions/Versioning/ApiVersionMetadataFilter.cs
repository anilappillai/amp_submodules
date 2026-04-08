using Asp.Versioning;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json.Nodes;

namespace Amp.Core.Extensions.Versioning;

/// <summary>
/// Swagger operation filter that annotates each operation with API version metadata:
///
///   • Marks deprecated operations with a ⚠ banner in the description.
///   • Sets <c>operation.Deprecated = true</c> on deprecated operations.
///   • Emits <c>x-api-deprecated: true</c> and <c>x-api-sunset</c> OpenAPI extensions
///     on deprecated operations so API gateways and generated clients can act on them.
///   • Adds the <c>api-version</c> query parameter to every operation so it is
///     visible and testable directly in Swagger UI.
///
/// Registered automatically by <c>AddCoreSwagger</c>.
/// </summary>
public sealed class ApiVersionMetadataFilter(ApiVersionSunsetPolicy? sunsetPolicy = null)
    : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;
        var groupVersion = context.DocumentName; // e.g. "v1", "v2"

        // Resolve the ApiVersion for this operation in this document's version group.
        var currentApiVersion = apiDescription.ActionDescriptor.EndpointMetadata
            .OfType<ApiVersionAttribute>()
            .SelectMany(a => a.Versions)
            .FirstOrDefault(v => $"v{v}" == groupVersion || v.ToString() == groupVersion);

        if (currentApiVersion is not null)
        {
            var model = apiDescription.ActionDescriptor.EndpointMetadata
                .OfType<ApiVersionMetadata>()
                .FirstOrDefault()
                ?.Map(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

            bool isDeprecated = model?.DeprecatedApiVersions.Contains(currentApiVersion) ?? false;

            if (isDeprecated)
            {
                operation.Deprecated = true;

                var sunsetDate = sunsetPolicy?.GetSunset(currentApiVersion.ToString());
                var sunsetText = sunsetDate.HasValue
                    ? $" Scheduled removal: **{sunsetDate.Value:yyyy-MM-dd}**."
                    : string.Empty;

                var banner = $"> ⚠ **This version is deprecated.**{sunsetText} Please migrate to the latest version.\n\n";
                operation.Description = banner + (operation.Description ?? string.Empty);

                // Machine-readable extensions for gateways and generated clients.
                operation.Extensions["x-api-deprecated"] = new OpenApiStringExtension("true");
                if (sunsetDate.HasValue)
                    operation.Extensions["x-api-sunset"] = new OpenApiStringExtension(sunsetDate.Value.ToString("yyyy-MM-dd"));
            }
        }

        // ── api-version query string parameter ────────────────────────────────
        // Documents the query string negotiation strategy so it is visible
        // and directly testable in Swagger UI (alongside URL segment and header).
        operation.Parameters ??= [];

        if (!operation.Parameters.Any(p => p.Name == "api-version" && p.In == ParameterLocation.Query))
        {
            var versionValue = groupVersion?.TrimStart('v') ?? "1.0";
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "api-version",
                In = ParameterLocation.Query,
                Required = false,
                Description = $"API version. Current document: **{groupVersion}**.",
                Schema = new OpenApiSchema
                {
                    Type = JsonSchemaType.String,
                    Default = JsonValue.Create(versionValue)
                }
            });
        }
    }

    /// <summary>
    /// Minimal <see cref="IOpenApiExtension"/> that writes a plain string value.
    /// Used for <c>x-api-deprecated</c> and <c>x-api-sunset</c> operation extensions.
    /// OpenAPI.NET v2 removed <c>OpenApiAny</c> — extensions must implement this interface.
    /// </summary>
    private sealed class OpenApiStringExtension(string value) : IOpenApiExtension
    {
        public void Write(IOpenApiWriter writer, OpenApiSpecVersion specVersion) =>
            writer.WriteValue(value);
    }
}
