using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Registers OpenTelemetry tracing and metrics with an OTLP exporter.
/// In EKS this forwards to AWS Distro for OpenTelemetry (ADOT) Collector,
/// which routes data to CloudWatch, X-Ray, and CloudWatch Metrics.
///
/// Usage:  builder.Services.AddAwsMetricsExporter("Amp.Facebook.Api");
///
/// EKS prerequisite: deploy the ADOT Collector as a DaemonSet and set
///   OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317  in values.yaml.
/// </summary>
public static class AwsMetricsExtensions
{
    public static IServiceCollection AddAwsMetricsExporter(
        this IServiceCollection services,
        string serviceName,
        string? serviceVersion = null,
        Action<OpenTelemetryBuilder>? configure = null)
    {
        var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")
                           ?? "http://localhost:4317";

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion ?? "1.0.0")
                .AddAttributes(new Dictionary<string, object>
                {
                    ["deployment.environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "production",
                    ["cloud.provider"] = "aws",
                    ["cloud.region"] = Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1"
                }))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation(opts =>
                {
                    opts.RecordException = true;
                    // Exclude health check probes from traces
                    opts.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                })
                .AddHttpClientInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)))
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint)));

        configure?.Invoke(builder);

        return services;
    }
}
