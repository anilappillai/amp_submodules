using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace Amp.Core.Middleware.Authentication;

/// <summary>
/// Custom JWT validation middleware that sits in the pipeline before
/// ASP.NET Core's built-in authentication handler.  It provides:
///
///   • Structured logging of auth failures (without leaking token details)
///   • Correlation ID propagation into auth error responses
///   • Pre-validation of token structure before the full handler runs
///
/// NOTE: For standard scenarios you should prefer
///   builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
///                    .AddJwtBearer(...)
/// and use this middleware only for additional cross-cutting behaviour.
/// </summary>
public sealed class JwtAuthenticationMiddleware(
    RequestDelegate next,
    IOptions<JwtOptions> options,
    ILogger<JwtAuthenticationMiddleware> logger)
{
    private static readonly JwtSecurityTokenHandler _handler = new();
    private readonly JwtOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;

        if (!TryExtractToken(context, out var rawToken))
        {
            await next(context); // Let downstream handle unauthenticated requests
            return;
        }

        try
        {
            var principal = _handler.ValidateToken(rawToken, BuildParameters(), out var validatedToken);
            context.User = principal;
            context.Items["JwtToken"] = validatedToken;
            logger.LogDebug("JWT validated for subject {Subject} | CorrelationId: {CorrelationId}",
                principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown", correlationId);
        }
        catch (SecurityTokenExpiredException)
        {
            logger.LogWarning("Expired JWT received | CorrelationId: {CorrelationId}", correlationId);
            await WriteAuthErrorAsync(context, "Token has expired.", HttpStatusCode.Unauthorized, correlationId);
            return;
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning("Invalid JWT: {Reason} | CorrelationId: {CorrelationId}", ex.Message, correlationId);
            await WriteAuthErrorAsync(context, "Token is invalid.", HttpStatusCode.Unauthorized, correlationId);
            return;
        }

        await next(context);
    }

    private TokenValidationParameters BuildParameters() => new()
    {
        ValidIssuer = _options.Issuer,
        ValidAudience = _options.Audience,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = _options.ValidateLifetime,
        ValidateIssuerSigningKey = true,
        ClockSkew = _options.ClockSkew,
        // IssuerSigningKeys resolved via JWKS endpoint configured in AddJwtBearer
    };

    private static bool TryExtractToken(HttpContext context, out string token)
    {
        token = string.Empty;
        var authHeader = context.Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return false;
        token = authHeader["Bearer ".Length..].Trim();
        return !string.IsNullOrWhiteSpace(token);
    }

    private static async Task WriteAuthErrorAsync(
        HttpContext context, string message, HttpStatusCode statusCode, string correlationId)
    {
        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/json";
        context.Response.Headers["X-Correlation-Id"] = correlationId;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            success = false,
            message,
            correlationId,
            timestamp = DateTime.UtcNow
        }));
    }
}
