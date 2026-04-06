using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Amp.Core.Services.Abstractions.Secrets;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Amp.Core.Services.Secrets;

/// <summary>
/// AWS Secrets Manager implementation of <see cref="ISecretsService"/>.
/// Caches the secret bundle in memory after the first load to avoid
/// excess API calls (AWS Secrets Manager has rate limits).
///
/// Credentials are obtained via IRSA — no static keys anywhere.
/// </summary>
public sealed class AwsSecretsService(
    string secretName,
    string region,
    ILogger<AwsSecretsService> logger) : ISecretsService
{
    private IReadOnlyDictionary<string, string>? _cache;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public async Task<string> GetSecretValueAsync(string key, CancellationToken ct = default)
    {
        var secrets = await GetAllSecretsAsync(ct);
        return secrets.TryGetValue(key, out var value)
            ? value
            : throw new KeyNotFoundException($"Secret key '{key}' was not found in secret '{secretName}'.");
    }

    public async Task<string?> GetSecretValueOrDefaultAsync(string key, string? defaultValue = null, CancellationToken ct = default)
    {
        var secrets = await GetAllSecretsAsync(ct);
        return secrets.TryGetValue(key, out var value) ? value : defaultValue;
    }

    public async Task<IReadOnlyDictionary<string, string>> GetAllSecretsAsync(CancellationToken ct = default)
    {
        if (_cache is not null) return _cache;

        await _semaphore.WaitAsync(ct);
        try
        {
            if (_cache is not null) return _cache;

            using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));
            var response = await client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            }, ct);

            if (string.IsNullOrWhiteSpace(response.SecretString))
            {
                _cache = new Dictionary<string, string>();
                return _cache;
            }

            var raw = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                response.SecretString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? [];

            _cache = raw.ToDictionary(k => k.Key, v => v.Value.ToString())
                        .AsReadOnly();

            logger.LogInformation("Loaded {Count} secrets from AWS Secrets Manager (secret: {SecretName})",
                _cache.Count, secretName);

            return _cache;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load secrets from AWS Secrets Manager (secret: {SecretName})", secretName);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }
}
