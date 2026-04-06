using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Convenience extensions to register typed health checks backed by
/// real infrastructure (SQL Server, Redis, S3) in a single call.
///
/// All connection strings / endpoints come from configuration (AWS Secrets Manager);
/// nothing is hardcoded here.
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds a SQL Server health check. Connection string is read from configuration
    /// using <paramref name="connectionStringKey"/>.
    /// </summary>
    public static IHealthChecksBuilder AddSqlServerCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string connectionStringKey = "Optima2Connection",
        string? name = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringKey)
            ?? throw new InvalidOperationException(
                $"Health check requires connection string '{connectionStringKey}' to be present in configuration. " +
                "Ensure it is provided via AWS Secrets Manager.");

        return builder.AddSqlServer(
            connectionString: connectionString,
            name: name ?? $"sqlserver-{connectionStringKey}",
            tags: ["db", "ready"]);
    }

    /// <summary>
    /// Adds a Redis health check. Connection string is read from configuration.
    /// </summary>
    public static IHealthChecksBuilder AddRedisCheck(
        this IHealthChecksBuilder builder,
        IConfiguration configuration,
        string connectionStringKey = "RedisConnection",
        string? name = null)
    {
        var connectionString = configuration.GetConnectionString(connectionStringKey)
            ?? throw new InvalidOperationException(
                $"Health check requires connection string '{connectionStringKey}' in configuration.");

        return builder.AddRedis(
            redisConnectionString: connectionString,
            name: name ?? "redis",
            tags: ["cache", "ready"]);
    }
}
