namespace Amp.Core.Services.Abstractions.Secrets;

/// <summary>
/// Abstraction over AWS Secrets Manager so services can retrieve individual
/// secret values without depending directly on the AWS SDK.
/// </summary>
public interface ISecretsService
{
    /// <summary>
    /// Returns the value of <paramref name="key"/> from the application's secret bundle.
    /// Throws <see cref="KeyNotFoundException"/> if the key does not exist.
    /// </summary>
    Task<string> GetSecretValueAsync(string key, CancellationToken ct = default);

    /// <summary>
    /// Returns the value of <paramref name="key"/>, or <paramref name="defaultValue"/>
    /// if the key is not present.
    /// </summary>
    Task<string?> GetSecretValueOrDefaultAsync(string key, string? defaultValue = null, CancellationToken ct = default);

    /// <summary>
    /// Returns all key-value pairs in the secret bundle.
    /// </summary>
    Task<IReadOnlyDictionary<string, string>> GetAllSecretsAsync(CancellationToken ct = default);
}
