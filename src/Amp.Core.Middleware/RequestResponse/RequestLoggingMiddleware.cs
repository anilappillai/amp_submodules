using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Amp.Core.Middleware.RequestResponse;

/// <summary>
/// Logs structured request/response metadata for every HTTP call:
///   • Method, path, query string
///   • Response status code and elapsed time in ms
///   • Correlation ID
///
/// Sensitive headers (Authorization, Cookie) are never logged.
/// Body logging is disabled by default to avoid PII exposure.
/// </summary>
public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    private static readonly HashSet<string> _sensitiveHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Authorization", "Cookie", "Set-Cookie", "X-Api-Key"
    };

    public async Task InvokeAsync(HttpContext context)
    {
        // Skip health probes to avoid log noise
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            await next(context);
            return;
        }

        var correlationId = context.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "HTTP {Method} {Path}{Query} started | CorrelationId: {CorrelationId}",
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString,
            correlationId);

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();
            var level = context.Response.StatusCode >= 500
                ? Microsoft.Extensions.Logging.LogLevel.Error
                : context.Response.StatusCode >= 400
                    ? Microsoft.Extensions.Logging.LogLevel.Warning
                    : Microsoft.Extensions.Logging.LogLevel.Information;

            logger.Log(level,
                "HTTP {Method} {Path} responded {StatusCode} in {Elapsed}ms | CorrelationId: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }
}
