using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Amp.Core.Extensions.Configuration;

/// <summary>
/// Configuration extension for loading secrets from AWS Secrets Manager.
/// Credentials are obtained via IRSA (IAM Roles for Service Accounts) in EKS —
/// no static keys are ever stored in code, config files, or environment variables.
///
/// Usage in Program.cs:
/// <code>
///   builder.Configuration.AddAwsSecretsManager(
///       secretName: Environment.GetEnvironmentVariable("AWS_SECRET_NAME")!,
///       region: Environment.GetEnvironmentVariable("AWS_REGION") ?? "us-east-1");
/// </code>
/// </summary>
public static class AwsSecretsManagerExtensions
{
    public static IConfigurationBuilder AddAwsSecretsManager(
        this IConfigurationBuilder builder,
        string secretName,
        string region = "us-east-1")
    {
        Amp.Core.Common.Helpers.Guard.NotNullOrWhiteSpace(secretName);
        return builder.Add(new AwsSecretsManagerSource(secretName, region));
    }
}

internal sealed class AwsSecretsManagerSource(string secretName, string region) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new AwsSecretsManagerProvider(secretName, region);
}

internal sealed class AwsSecretsManagerProvider(string secretName, string region) : ConfigurationProvider
{
    public override void Load()
    {
        using var client = new AmazonSecretsManagerClient(RegionEndpoint.GetBySystemName(region));

        try
        {
            var response = client.GetSecretValueAsync(new GetSecretValueRequest
            {
                SecretId = secretName
            }).GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(response.SecretString)) return;

            var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                response.SecretString,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (dict is null) return;

            foreach (var (key, value) in dict)
            {
                // AWS convention: double-underscore (__) maps to nested configuration
                // sections, e.g. "ConnectionStrings__Optima2Connection" → ConnectionStrings:Optima2Connection
                Data[key.Replace("__", ":")] = value.ToString();
            }
        }
        catch (ResourceNotFoundException ex)
        {
            throw new InvalidOperationException(
                $"AWS Secrets Manager secret '{secretName}' (region: {region}) was not found. " +
                "Verify the secret name and that the EKS pod's IRSA role has secretsmanager:GetSecretValue permission.", ex);
        }
    }
}
