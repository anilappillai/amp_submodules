using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Options controlling how the AMP core API versioning is configured.
/// All values have sensible defaults — most consuming APIs will not need to change anything.
/// </summary>
public sealed class AmpApiVersioningOptions
{
    /// <summary>Default API version when the client does not specify one. Default: 1.0.</summary>
    public ApiVersion DefaultVersion { get; set; } = new(1, 0);

    /// <summary>
    /// When <c>true</c>, requests without an explicit version are treated as if they
    /// requested <see cref="DefaultVersion"/>. Default: <c>true</c>.
    /// </summary>
    public bool AssumeDefaultWhenUnspecified { get; set; } = true;

    /// <summary>
    /// When <c>true</c>, the <c>api-supported-versions</c> and
    /// <c>api-deprecated-versions</c> response headers are emitted on every response.
    /// Default: <c>true</c>.
    /// </summary>
    public bool ReportApiVersions { get; set; } = true;

    /// <summary>
    /// Accept version from the URL segment (e.g. <c>/api/v1/orders</c>).
    /// Default: <c>true</c>.
    /// </summary>
    public bool ReadFromUrlSegment { get; set; } = true;

    /// <summary>
    /// Accept version from the <c>api-version</c> request header.
    /// Default: <c>true</c>.
    /// </summary>
    public bool ReadFromHeader { get; set; } = true;

    /// <summary>
    /// Accept version from the <c>api-version</c> query string parameter.
    /// Allows <c>GET /orders?api-version=2.0</c> — useful for browser clients and testing.
    /// Default: <c>true</c>.
    /// </summary>
    public bool ReadFromQueryString { get; set; } = true;

    /// <summary>
    /// Name of the query string parameter. Default: <c>"api-version"</c>.
    /// </summary>
    public string QueryStringParameterName { get; set; } = "api-version";

    /// <summary>
    /// Name of the request header. Default: <c>"api-version"</c>.
    /// </summary>
    public string HeaderName { get; set; } = "api-version";

    /// <summary>
    /// Format string used when grouping versioned Swagger documents.
    /// <c>'v'VVV</c> produces <c>v1</c>, <c>v1.1</c>, etc.
    /// Default: <c>"'v'VVV"</c>.
    /// </summary>
    public string GroupNameFormat { get; set; } = "'v'VVV";
}

/// <summary>
/// Registers ASP.NET Core API versioning consistently across all AMP services.
///
/// Supported version negotiation strategies (all active by default):
///   • URL segment  — /api/v1/orders
///   • Request header — api-version: 1.0
///   • Query string  — /orders?api-version=1.0
///
/// Deprecated versions are surfaced via:
///   • <c>api-deprecated-versions</c> response header  (built-in)
///   • <c>X-Api-Version</c> response header            (added by <see cref="ApiVersionHeaderMiddleware"/>)
///   • Swagger document description                    (added by <see cref="ConfigureSwaggerOptions"/>)
///   • Swagger operation extension                     (added by <see cref="ApiVersionMetadataFilter"/>)
///
/// Typical usage in Program.cs:
/// <code>
///   builder.Services.AddCoreApiVersioning();
///   // or with overrides:
///   builder.Services.AddCoreApiVersioning(o => o.ReadFromQueryString = false);
/// </code>
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddCoreApiVersioning(
        this IServiceCollection services,
        Action<AmpApiVersioningOptions>? configure = null)
    {
        var opts = new AmpApiVersioningOptions();
        configure?.Invoke(opts);

        // Build the composite reader from enabled strategies.
        var readers = new List<IApiVersionReader>();
        if (opts.ReadFromUrlSegment)   readers.Add(new UrlSegmentApiVersionReader());
        if (opts.ReadFromHeader)       readers.Add(new HeaderApiVersionReader(opts.HeaderName));
        if (opts.ReadFromQueryString)  readers.Add(new QueryStringApiVersionReader(opts.QueryStringParameterName));

        var reader = readers.Count switch
        {
            0 => (IApiVersionReader)new UrlSegmentApiVersionReader(),
            1 => readers[0],
            _ => ApiVersionReader.Combine(readers.ToArray())
        };

        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = opts.DefaultVersion;
                options.AssumeDefaultVersionWhenUnspecified = opts.AssumeDefaultWhenUnspecified;
                options.ReportApiVersions = opts.ReportApiVersions;
                options.ApiVersionReader = reader;
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = opts.GroupNameFormat;
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
