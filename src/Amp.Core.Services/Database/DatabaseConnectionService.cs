using Amp.Core.Services.Abstractions.Database;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Data;

namespace Amp.Core.Services.Database;

/// <summary>
/// SQL Server implementation of <see cref="IDatabaseConnectionService"/>.
/// Connection strings are injected via <see cref="IConfiguration"/>, which at
/// runtime is populated from AWS Secrets Manager — no connection strings are
/// hardcoded anywhere.
///
/// Usage:  var conn = await _db.GetConnectionAsync("Optima2Connection");
/// </summary>
public sealed class DatabaseConnectionService(
    IConfiguration configuration,
    ILogger<DatabaseConnectionService> logger) : IDatabaseConnectionService
{
    public async Task<IDbConnection> GetConnectionAsync(string connectionName, CancellationToken ct = default)
    {
        var connectionString = GetConnectionString(connectionName);
        var connection = new SqlConnection(connectionString);

        try
        {
            await connection.OpenAsync(ct);
            logger.LogDebug("Opened SQL connection to '{ConnectionName}'", connectionName);
            return connection;
        }
        catch (Exception ex)
        {
            await connection.DisposeAsync();
            logger.LogError(ex, "Failed to open SQL connection to '{ConnectionName}'", connectionName);
            throw;
        }
    }

    public string GetConnectionString(string connectionName)
    {
        var cs = configuration.GetConnectionString(connectionName);
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException(
                $"Connection string '{connectionName}' is not configured. " +
                "Ensure it is provided via AWS Secrets Manager " +
                "(secret key: ConnectionStrings__{connectionName}).");
        return cs;
    }

    public async Task<bool> IsHealthyAsync(string connectionName, CancellationToken ct = default)
    {
        try
        {
            using var conn = await GetConnectionAsync(connectionName, ct);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT 1";
            await ((SqlCommand)cmd).ExecuteScalarAsync(ct);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Health check failed for connection '{ConnectionName}'", connectionName);
            return false;
        }
    }
}
