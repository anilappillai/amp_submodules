using System.Data;

namespace Amp.Core.Services.Abstractions.Database;

/// <summary>
/// Abstracts database connection management so services remain testable
/// and the underlying provider (SQL Server, PostgreSQL, etc.) can be swapped.
/// </summary>
public interface IDatabaseConnectionService
{
    /// <summary>Opens and returns a new database connection for the named context.</summary>
    /// <param name="connectionName">Key used to look up the connection string (e.g. "Optima2Connection").</param>
    Task<IDbConnection> GetConnectionAsync(string connectionName, CancellationToken ct = default);

    /// <summary>Returns the raw connection string for a given name (already resolved from Secrets Manager).</summary>
    string GetConnectionString(string connectionName);

    /// <summary>True if the database for <paramref name="connectionName"/> can be reached.</summary>
    Task<bool> IsHealthyAsync(string connectionName, CancellationToken ct = default);
}
