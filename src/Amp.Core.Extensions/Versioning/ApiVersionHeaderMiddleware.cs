using Asp.Versioning;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Amp.Core.Extensions.Versioning;

/// <summary>
/// Middleware that stamps every API response with version metadata headers:
///
///   X-Api-Version: 2.0
///     The exact API version the framework resolved for this request.
///
///   X-Api-Deprecated: true          (only on deprecated versions)
///     Signals to clients that they should migrate to a newer version.
///
///   Sunset: Sat, 01 Jan 2026 00:00:00 GMT   (when a date is in <see cref="ApiVersionSunsetPolicy"/>)
///     RFC 8594 — tells clients the exact date when this version will be removed.
///
/// Registration (called automatically by <c>UseAmpCoreVersioning</c>):
/// <code>
///   app.UseMiddleware&lt;ApiVersionHeaderMiddleware&gt;();
/// </code>
///
/// Sunset dates (optional — register before calling AddCoreServices):
/// <code>
///   builder.Services.AddSingleton(new ApiVersionSunsetPolicy
///   {
///       ["1.0"] = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
///   });
/// </code>
/// </summary>
public sealed class ApiVersionHeaderMiddleware(
    RequestDelegate next,
    ILogger<ApiVersionHeaderMiddleware> logger,
    ApiVersionSunsetPolicy? sunsetPolicy = null)
{
    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // IApiVersioningFeature is populated by Asp.Versioning after routing.
        var feature = context.Features.Get<IApiVersioningFeature>();
        if (feature is null) return;

        var version = feature.RequestedApiVersion;
        if (version is null) return;

        var versionString = version.ToString();

        // Always stamp the resolved version.
        context.Response.Headers["X-Api-Version"] = versionString;

        // Inspect endpoint metadata to detect deprecation.
        var endpoint = context.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<ApiVersionMetadata>();
        if (metadata is null) return;

        var model = metadata.Map(ApiVersionMapping.Explicit | ApiVersionMapping.Implicit);

        if (!model.DeprecatedApiVersions.Contains(version)) return;

        context.Response.Headers["X-Api-Deprecated"] = "true";

        // RFC 8594 Sunset header when a removal date is configured.
        var sunset = sunsetPolicy?.GetSunset(versionString);
        if (sunset.HasValue)
            context.Response.Headers["Sunset"] = sunset.Value.ToString("R");  // RFC 1123

        logger.LogDebug(
            "Request served on deprecated API version {Version}. Sunset: {Sunset}. Path: {Path}",
            versionString, sunset?.ToString("yyyy-MM-dd") ?? "not set", context.Request.Path);
    }
}
