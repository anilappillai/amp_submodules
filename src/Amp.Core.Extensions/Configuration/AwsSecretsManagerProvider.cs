using Amazon;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace Amp.Core.Extensions.Configuration;

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
