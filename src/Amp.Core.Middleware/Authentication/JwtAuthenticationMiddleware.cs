using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Amp.Core.Middleware.Authentication;

/// <summary>
/// Optional supplemental middleware that adds structured JWT auth logging and
/// propagates the Correlation ID into 401/403 responses.
///
/// NOTE: This middleware does NOT perform token validation. Token validation is
/// handled by ASP.NET Core's built-in JWT bearer handler registered via
/// <c>AddAmpCoreMiddleware(configuration)</c>, which uses OIDC/JWKS discovery.
///
/// This middleware is NOT added to the pipeline by default.
/// <c>UseAmpCoreMiddleware</c> calls <c>UseAuthentication()</c> and
/// <c>UseAuthorization()</c> directly — those are the auth components that protect
/// endpoints. Use this middleware only if you need custom post-authentication logic.
/// </summary>
public sealed class JwtAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<JwtOptions> options,
    ILogger<JwtAuthenticationMiddleware> logger)
{
    private readonly JwtOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        await next(context);

        // Propagate Correlation ID into any 401/403 that wasn't already handled
        // by the JwtBearerEvents registered in AddAmpCoreMiddleware.
        if (context.Response.StatusCode is 401 or 403)
        {
            var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(correlationId)
                && !context.Response.Headers.ContainsKey("X-Correlation-Id"))
            {
                context.Response.Headers["X-Correlation-Id"] = correlationId;
            }

            logger.LogWarning(
                "Request returned {StatusCode} | Authority: {Authority} | CorrelationId: {CorrelationId}",
                context.Response.StatusCode, _options.Authority, correlationId);
        }
    }
}
