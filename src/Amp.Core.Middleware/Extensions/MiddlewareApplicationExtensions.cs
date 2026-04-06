using Amp.Core.Middleware.Authentication;
using Amp.Core.Middleware.CorrelationId;
using Amp.Core.Middleware.HealthCheck;
using Amp.Core.Middleware.RequestResponse;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Amp.Core.Middleware.Extensions;

/// <summary>
/// One-call extension to register all AMP core middleware components
/// in the correct pipeline order.
///
/// Usage in Program.cs:
/// <code>
///   app.UseAmpCoreMiddleware();
/// </code>
///
/// Middleware order (must not be changed — ASP.NET Core pipeline is order-sensitive):
///   1. ExceptionHandling   — outermost; catches everything
///   2. HealthCheck         — before auth so probes never need tokens
///   3. CorrelationId       — before logging so IDs appear in all logs
///   4. RequestLogging      — before routing/auth for full request coverage
///   5. JwtAuthentication   — validates Bearer tokens
/// </summary>
public static class MiddlewareApplicationExtensions
{
    public static IApplicationBuilder UseAmpCoreMiddleware(
        this IApplicationBuilder app,
        Action<AmpMiddlewareOptions>? configure = null)
    {
        var opts = new AmpMiddlewareOptions();
        configure?.Invoke(opts);

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (opts.EnableHealthCheck)
            app.UseMiddleware<HealthCheckMiddleware>();

        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<RequestLoggingMiddleware>();

        if (opts.EnableJwtAuthentication)
            app.UseMiddleware<JwtAuthenticationMiddleware>();

        return app;
    }

    /// <summary>
    /// Registers the DI services required by AMP core middleware.
    /// Call this inside ConfigureServices / builder.Services in Program.cs.
    /// </summary>
    public static IServiceCollection AddAmpCoreMiddleware(
        this IServiceCollection services,
        Action<CorrelationIdOptions>? configureCorrelationId = null,
        Action<JwtOptions>? configureJwt = null)
    {
        // Correlation ID
        if (configureCorrelationId is not null)
            services.Configure(configureCorrelationId);
        else
            services.AddSingleton(Options.Create(new CorrelationIdOptions()));

        services.AddSingleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();

        // JWT
        if (configureJwt is not null)
            services.Configure(configureJwt);
        else
            services.AddSingleton(Options.Create(new JwtOptions()));

        services.AddHttpContextAccessor();

        return services;
    }
}

public sealed class AmpMiddlewareOptions
{
    public bool EnableHealthCheck { get; set; } = true;
    public bool EnableJwtAuthentication { get; set; } = false; // opt-in
}
