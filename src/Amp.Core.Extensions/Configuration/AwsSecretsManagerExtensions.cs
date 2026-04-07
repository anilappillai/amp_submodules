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

