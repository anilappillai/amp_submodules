using Amp.Core.Common.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using System.Text.Json;

namespace Amp.Core.Middleware.HealthCheck;

/// <summary>
/// Provides two EKS probe endpoints:
///   GET /health/live  — liveness:  returns 200 if the process is running
///   GET /health/ready — readiness: returns 200 only when all registered checks pass
///
/// Used by the Helm deployment.yaml liveness/readinessProbe configuration.
/// </summary>
public sealed class HealthCheckMiddleware(
    RequestDelegate next,
    HealthCheckService healthCheckService)
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Path.Equals("/health/live", StringComparison.OrdinalIgnoreCase))
        {
            // Liveness — only checks that the process is alive
            await WriteLivenessAsync(context);
            return;
        }

        if (context.Request.Path.Equals("/health/ready", StringComparison.OrdinalIgnoreCase))
        {
            // Readiness — runs all registered IHealthCheck implementations
            await WriteReadinessAsync(context);
            return;
        }

        await next(context);
    }

    private static async Task WriteLivenessAsync(HttpContext context)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            status = "live",
            timestamp = DateTimeHelper.ToIso8601(DateTime.UtcNow)
        }, _jsonOptions));
    }

    private async Task WriteReadinessAsync(HttpContext context)
    {
        var report = await healthCheckService.CheckHealthAsync(
            predicate: r => r.Tags.Contains("ready"),
            context.RequestAborted);

        var isHealthy = report.Status == HealthStatus.Healthy;
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = isHealthy
            ? (int)HttpStatusCode.OK
            : (int)HttpStatusCode.ServiceUnavailable;

        var payload = new
        {
            status = report.Status.ToString().ToLowerInvariant(),
            timestamp = DateTimeHelper.ToIso8601(DateTime.UtcNow),
            duration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString().ToLowerInvariant(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, _jsonOptions));
    }
}
