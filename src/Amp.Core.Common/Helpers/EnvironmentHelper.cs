namespace Amp.Core.Common.Helpers;

/// <summary>
/// Utilities for reading environment information, particularly useful
/// when running in EKS where environment variables drive configuration.
/// </summary>
public static class EnvironmentHelper
{
    /// <summary>Returns the value of an environment variable, or <paramref name="defaultValue"/> if not set.</summary>
    public static string GetRequired(string name) =>
        Environment.GetEnvironmentVariable(name)
        ?? throw new InvalidOperationException(
            $"Required environment variable '{name}' is not set. " +
            "Ensure it is configured in the Helm values or pod spec.");

    /// <summary>Returns the value of an environment variable, or <paramref name="defaultValue"/> if not set.</summary>
    public static string Get(string name, string defaultValue = "") =>
        Environment.GetEnvironmentVariable(name) ?? defaultValue;

    /// <summary>True when running inside a container (EKS pod sets DOTNET_RUNNING_IN_CONTAINER=true).</summary>
    public static bool IsContainerized =>
        string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the current ASPNETCORE_ENVIRONMENT value.</summary>
    public static string AspNetCoreEnvironment =>
        Get("ASPNETCORE_ENVIRONMENT", "Production");

    public static bool IsDevelopment => AspNetCoreEnvironment.Equals("Development", StringComparison.OrdinalIgnoreCase);
    public static bool IsStaging => AspNetCoreEnvironment.Equals("Staging", StringComparison.OrdinalIgnoreCase);
    public static bool IsProduction => AspNetCoreEnvironment.Equals("Production", StringComparison.OrdinalIgnoreCase);

    /// <summary>AWS region — read from environment variable injected by Helm.</summary>
    public static string AwsRegion => Get("AWS_REGION", "us-east-1");

    /// <summary>AWS Secrets Manager secret name — injected by Helm.</summary>
    public static string AwsSecretName => Get("AWS_SECRET_NAME");
}
