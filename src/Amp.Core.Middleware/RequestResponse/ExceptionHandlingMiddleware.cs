using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Text.Json;

namespace Amp.Core.Middleware.RequestResponse;

/// <summary>
/// Global exception handler — converts unhandled exceptions to structured
/// JSON error responses and logs them with full stack traces.
///
/// Placed first in the middleware pipeline so it catches errors from all
/// downstream components, including authentication and routing.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? Guid.NewGuid().ToString();

        var (statusCode, message) = exception switch
        {
            ArgumentNullException or ArgumentException => (HttpStatusCode.BadRequest, exception.Message),
            UnauthorizedAccessException => (HttpStatusCode.Forbidden, "Access denied."),
            KeyNotFoundException => (HttpStatusCode.NotFound, exception.Message),
            OperationCanceledException => (HttpStatusCode.ServiceUnavailable, "Request was cancelled."),
            TimeoutException => (HttpStatusCode.GatewayTimeout, "The operation timed out."),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred.")
        };

        // Log 500s as errors; 4xx as warnings
        if (statusCode == HttpStatusCode.InternalServerError)
            logger.LogError(exception,
                "Unhandled exception {ExceptionType} | CorrelationId: {CorrelationId}",
                exception.GetType().Name, correlationId);
        else
            logger.LogWarning(exception,
                "{ExceptionType}: {Message} | CorrelationId: {CorrelationId}",
                exception.GetType().Name, exception.Message, correlationId);

        if (!context.Response.HasStarted)
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
            }, _options));
        }
    }
}
