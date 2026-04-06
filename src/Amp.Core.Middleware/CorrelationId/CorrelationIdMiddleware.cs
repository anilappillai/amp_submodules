using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog.Context;

namespace Amp.Core.Middleware.CorrelationId;

/// <summary>
/// Ensures every HTTP request carries a Correlation ID so distributed traces
/// can be stitched together across microservices and in CloudWatch Logs.
///
/// Behaviour:
///   1. Reads the ID from the configured header (default: X-Correlation-Id).
///   2. Generates a new GUID if the header is absent (configurable).
///   3. Stores the ID in HttpContext.Items["CorrelationId"].
///   4. Echoes it in the response header.
///   5. Pushes it to Serilog's LogContext for the duration of the request.
/// </summary>
public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    IOptions<CorrelationIdOptions> options,
    ILogger<CorrelationIdMiddleware> logger)
{
    private readonly CorrelationIdOptions _options = options.Value;

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items["CorrelationId"] = correlationId;

        if (_options.IncludeInResponse)
            context.Response.Headers[_options.HeaderName] = correlationId;

        if (_options.PushToLogContext)
        {
            using (LogContext.PushProperty("CorrelationId", correlationId))
            {
                logger.LogDebug("Request {Method} {Path} | CorrelationId: {CorrelationId}",
                    context.Request.Method, context.Request.Path, correlationId);

                await next(context);
            }
        }
        else
        {
            await next(context);
        }
    }

    private string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.HeaderName, out var value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value.ToString();
        }

        return _options.GenerateIfMissing ? Guid.NewGuid().ToString() : string.Empty;
    }
}

/// <summary>Accessor injected into downstream services to read the current correlation ID.</summary>
public interface ICorrelationIdAccessor
{
    string? CorrelationId { get; }
}

internal sealed class HttpContextCorrelationIdAccessor(IHttpContextAccessor accessor) : ICorrelationIdAccessor
{
    public string? CorrelationId =>
        accessor.HttpContext?.Items["CorrelationId"]?.ToString();
}
