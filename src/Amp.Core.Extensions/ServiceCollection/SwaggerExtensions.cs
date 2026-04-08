using Amp.Core.Extensions.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Controls Swagger/OpenAPI generation and UI behaviour for AMP APIs.
/// </summary>
public sealed class AmpSwaggerOptions
{
    /// <summary>API title shown in the Swagger UI header. Required.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Human-readable description shown in the Swagger UI.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, the JWT Bearer security definition is added to the Swagger
    /// document so developers can authenticate directly in the UI. Default: <c>true</c>.
    /// </summary>
    public bool EnableJwtAuth { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, Swagger UI is served in all environments.
    /// When <c>false</c> (default), UI is only served in Development.
    ///
    /// Useful for staging environments that need API exploration without publishing
    /// the UI in production.
    /// </summary>
    public bool EnableInAllEnvironments { get; set; } = false;

    /// <summary>
    /// Contact information embedded in each versioned OpenAPI document.
    /// Optional — omitted from the document when null.
    /// </summary>
    public AmpApiContact? Contact { get; set; }
}

/// <summary>Contact details embedded in the OpenAPI info block.</summary>
public sealed class AmpApiContact
{
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Uri? Url { get; set; }
}

/// <summary>
/// Registers Swagger/OpenAPI generation with versioned documents, JWT auth,
/// correlation ID headers, and API version deprecation metadata.
///
/// Typical usage in Program.cs:
/// <code>
///   // Services
///   builder.Services.AddCoreSwagger(o =>
///   {
///       o.Title       = "Payments API";
///       o.Description = "Handles all payment flows";
///       o.Contact     = new AmpApiContact { Name = "Platform Team", Email = "platform@amp.com" };
///   });
///
///   // Pipeline
///   app.UseAmpCoreSwagger();
/// </code>
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddCoreSwagger(
        this IServiceCollection services,
        Action<AmpSwaggerOptions> configure)
    {
        var opts = new AmpSwaggerOptions();
        configure(opts);

        // ConfigureSwaggerOptions generates one OpenApiInfo per discovered API version.
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(sp =>
            new ConfigureSwaggerOptions(
                sp.GetRequiredService<IApiVersionDescriptionProvider>(),
                opts.Title,
                opts.Description,
                opts.Contact));

        // Ensure a default ApiVersionSunsetPolicy is available for DI injection into
        // ApiVersionMetadataFilter. Consuming apps can override this with their own
        // registration before calling AddCoreSwagger (TryAddSingleton won't overwrite).
        services.TryAddSingleton(new ApiVersionSunsetPolicy());

        services.AddSwaggerGen(options =>
        {
            // ── API version metadata in every operation ────────────────────────
            // No constructor args — Swashbuckle resolves ApiVersionMetadataFilter
            // from DI, injecting ApiVersionSunsetPolicy automatically.
            options.OperationFilter<ApiVersionMetadataFilter>();

            // ── Correlation ID header on every operation ───────────────────────
            options.OperationFilter<CorrelationIdOperationFilter>();

            // ── JWT bearer auth ────────────────────────────────────────────────
            if (!opts.EnableJwtAuth) return;

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token (without the 'Bearer' prefix)."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                { new OpenApiSecuritySchemeReference("Bearer"), [] }
            });
        });

        return services;
    }

    /// <summary>
    /// Adds Swagger JSON and SwaggerUI to the pipeline.
    ///
    /// Respects <see cref="AmpSwaggerOptions.EnableInAllEnvironments"/>:
    ///   — Default (false): UI only served in Development.
    ///   — True: UI served in all environments (staging, production).
    ///
    /// Usage:
    /// <code>
    ///   app.UseAmpCoreSwagger();
    /// </code>
    /// </summary>
    public static IApplicationBuilder UseAmpCoreSwagger(
        this WebApplication app,
        Action<AmpSwaggerOptions>? configure = null)
    {
        var opts = new AmpSwaggerOptions();
        configure?.Invoke(opts);

        bool shouldServe = opts.EnableInAllEnvironments || app.Environment.EnvironmentName
            .Equals("Development", StringComparison.OrdinalIgnoreCase);

        if (!shouldServe) return app;

        app.UseSwagger();
        app.UseSwaggerUI(ui =>
        {
            foreach (var desc in app.DescribeApiVersions())
            {
                var label = desc.IsDeprecated
                    ? $"{desc.GroupName} (deprecated)"
                    : desc.GroupName;
                ui.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", label);
            }

            // Expand only the first (latest) version by default.
            ui.DefaultModelsExpandDepth(-1);
            ui.DisplayRequestDuration();
        });

        return app;
    }
}
