using Amp.Core.Middleware.Authentication;
using Amp.Core.Middleware.CorrelationId;
using Amp.Core.Middleware.HealthCheck;
using Amp.Core.Middleware.RequestResponse;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amp.Core.Middleware.Extensions;

/// <summary>
/// One-call extension to register all AMP core middleware components
/// in the correct pipeline order.
///
/// Typical usage in Program.cs:
/// <code>
///   // Register services — reads Jwt:Authority / Jwt:Audience / Jwt:ClientId from config
///   builder.Services.AddAmpCoreMiddleware(builder.Configuration);
///
///   // Add middleware pipeline
///   app.UseAmpCoreMiddleware(o => o.EnableJwtAuthentication = true);
/// </code>
///
/// Config (appsettings.json or AWS Secrets Manager):
/// <code>
///   "Jwt": {
///     "Authority": "https://cognito-idp.us-east-1.amazonaws.com/us-east-1_abc123",
///     "Audience":  "amp-facebook-api",   // OR use ClientId
///     "ClientId":  "6exampleclientid"
///   }
/// </code>
///
/// Middleware order (must not be changed — ASP.NET Core pipeline is order-sensitive):
///   1. ExceptionHandling  — outermost; catches everything
///   2. HealthCheck        — before auth so probes never need tokens
///   3. CorrelationId      — before logging so IDs appear in all log entries
///   4. RequestLogging     — before routing/auth for full request coverage
///   5. Authentication     — validates Bearer tokens via OIDC/JWKS discovery
///   6. Authorization      — enforces [Authorize] policies
/// </summary>
public static class MiddlewareApplicationExtensions
{
    /// <summary>
    /// Registers AMP core middleware services. JWT bearer authentication is configured
    /// automatically from <paramref name="configuration"/> — no additional setup required
    /// in the consuming application.
    /// </summary>
    public static IServiceCollection AddAmpCoreMiddleware(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<CorrelationIdOptions>? configureCorrelationId = null)
    {
        // ── Correlation ID ────────────────────────────────────────────────────
        if (configureCorrelationId is not null)
            services.Configure(configureCorrelationId);
        else
            services.AddSingleton(Options.Create(new CorrelationIdOptions()));

        services.AddSingleton<ICorrelationIdAccessor, HttpContextCorrelationIdAccessor>();
        services.AddHttpContextAccessor();

        // ── JWT bearer authentication ─────────────────────────────────────────
        // Bind JwtOptions from IConfiguration so consuming apps only need to
        // set values in appsettings.json or AWS Secrets Manager (Jwt__Authority etc.)
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

        var jwtOpts = configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();

        if (!string.IsNullOrWhiteSpace(jwtOpts.Authority))
        {
            var audience = jwtOpts.ResolvedAudience;

            services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    // OIDC discovery fetches JWKS and validates issuer automatically.
                    options.Authority = jwtOpts.Authority;
                    options.RequireHttpsMetadata = jwtOpts.RequireHttpsMetadata;

                    if (!string.IsNullOrWhiteSpace(audience))
                        options.Audience = audience;

                    options.TokenValidationParameters.ClockSkew = jwtOpts.ClockSkew;

                    options.Events = BuildJwtBearerEvents();
                });

            services.AddAuthorization();
        }

        return services;
    }

    /// <summary>
    /// Adds the AMP core middleware pipeline. Call after
    /// <see cref="AddAmpCoreMiddleware"/> in Program.cs.
    /// </summary>
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
        {
            // ASP.NET Core's built-in auth pipeline handles OIDC/JWKS discovery,
            // token signature validation, lifetime, and audience checks.
            app.UseAuthentication();
            app.UseAuthorization();
        }

        return app;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static JwtBearerEvents BuildJwtBearerEvents() => new()
    {
        OnAuthenticationFailed = ctx =>
        {
            var logger = ctx.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger("Amp.Core.Middleware.JwtAuthentication");

            var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

            logger.LogWarning(
                "JWT authentication failed: {Reason} | CorrelationId: {CorrelationId}",
                ctx.Exception.GetType().Name, correlationId);

            return Task.CompletedTask;
        },

        OnChallenge = ctx =>
        {
            // Propagate correlation ID into 401 challenge responses.
            var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(correlationId))
                ctx.Response.Headers["X-Correlation-Id"] = correlationId;

            return Task.CompletedTask;
        },

        OnForbidden = ctx =>
        {
            // Propagate correlation ID into 403 forbidden responses.
            var correlationId = ctx.HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(correlationId))
                ctx.Response.Headers["X-Correlation-Id"] = correlationId;

            return Task.CompletedTask;
        }
    };
}
