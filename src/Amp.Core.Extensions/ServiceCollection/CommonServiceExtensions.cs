using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// One-stop extension that wires common infrastructure registrations shared
/// across all AMP API, service, and web projects.
///
/// Typical usage in Program.cs:
/// <code>
///   builder.Services.AddCoreServices(builder.Configuration, "MyApi");
/// </code>
/// </summary>
public static class CommonServiceExtensions
{
    public static IServiceCollection AddCoreServices(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName,
        Action<CoreServicesOptions>? configure = null)
    {
        var options = new CoreServicesOptions();
        configure?.Invoke(options);

        services.AddHttpContextAccessor();
        services.AddMemoryCache();

        if (options.EnableResponseCompression)
            services.AddResponseCompression();

        if (options.EnableCors)
            services.AddCors(cors => cors.AddPolicy("AmpCorePolicy", policy =>
                policy.WithOrigins(options.AllowedOrigins)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()));

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"]);

        if (options.EnableApiVersioning)
            services.AddCoreApiVersioning();

        return services;
    }

    /// <summary>
    /// Registers app-version metadata as a singleton so it can be injected
    /// into health endpoints and response headers.
    /// </summary>
    public static IServiceCollection AddAppVersionService(
        this IServiceCollection services,
        string applicationName,
        string version = "1.0.0")
    {
        services.AddSingleton(new AppVersionInfo(applicationName, version,
            Environment.GetEnvironmentVariable("BUILD_SHA") ?? "local",
            DateTime.UtcNow));
        return services;
    }
}

