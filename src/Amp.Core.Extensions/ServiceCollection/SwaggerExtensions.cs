using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Registers Swagger/OpenAPI with versioned documents and JWT bearer auth support.
/// Usage: builder.Services.AddCoreSwagger("Amp.Facebook.Api", "Social platform API");
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddCoreSwagger(
        this IServiceCollection services,
        string title,
        string description = "",
        bool enableJwtAuth = true)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(sp =>
            new ConfigureSwaggerOptions(sp.GetRequiredService<IApiVersionDescriptionProvider>(), title, description));

        services.AddSwaggerGen(options =>
        {
            if (!enableJwtAuth) return;

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token (without the 'Bearer' prefix)."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });

            options.OperationFilter<CorrelationIdOperationFilter>();
        });

        return services;
    }
}

internal sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    string title,
    string description) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, new OpenApiInfo
            {
                Title = title,
                Version = desc.ApiVersion.ToString(),
                Description = desc.IsDeprecated ? $"{description} — **DEPRECATED**" : description
            });
        }
    }
}

/// <summary>Adds the X-Correlation-Id header to every Swagger operation.</summary>
internal sealed class CorrelationIdOperationFilter : Swashbuckle.AspNetCore.SwaggerGen.IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "X-Correlation-Id",
            In = ParameterLocation.Header,
            Required = false,
            Schema = new OpenApiSchema { Type = "string", Format = "uuid" }
        });
    }
}
