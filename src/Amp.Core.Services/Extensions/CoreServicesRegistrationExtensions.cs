using Amp.Core.Services.Abstractions.Caching;
using Amp.Core.Services.Abstractions.Database;
using Amp.Core.Services.Abstractions.Http;
using Amp.Core.Services.Abstractions.Secrets;
using Amp.Core.Services.Caching;
using Amp.Core.Services.Database;
using Amp.Core.Services.Http;
using Amp.Core.Services.Resilience;
using Amp.Core.Services.Secrets;
using Amp.Core.Common.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Amp.Core.Services.Extensions;

/// <summary>
/// Single-call registration for all Amp.Core.Services implementations.
///
/// Typical usage in any AMP API/service/web Program.cs:
/// <code>
///   builder.Services.AddAmpCoreServices(builder.Configuration);
/// </code>
/// </summary>
public static class CoreServicesRegistrationExtensions
{
    /// <summary>
    /// Registers database, caching (in-memory + optional Redis), HTTP client,
    /// and secrets service implementations against their abstractions.
    /// </summary>
    public static IServiceCollection AddAmpCoreServices(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AmpCoreServicesOptions>? configure = null)
    {
        var options = new AmpCoreServicesOptions();
        configure?.Invoke(options);

        // ── Database ──────────────────────────────────────────────────────────
        services.AddScoped<IDatabaseConnectionService, DatabaseConnectionService>();

        // ── Caching ───────────────────────────────────────────────────────────
        services.AddMemoryCache();

        var redisConnection = configuration.GetConnectionString("RedisConnection");
        if (!string.IsNullOrWhiteSpace(redisConnection) && options.UseRedisCache)
        {
            services.AddStackExchangeRedisCache(o => o.Configuration = redisConnection);

            // IConnectionMultiplexer is needed by DistributedCachingService.RemoveByPrefixAsync
            // to perform SCAN-based prefix deletion. Registered as singleton — one connection
            // pool per process is the StackExchange.Redis recommendation.
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConnection));

            services.AddScoped<ICachingService, DistributedCachingService>();
        }
        else
        {
            services.AddScoped<ICachingService, InMemoryCachingService>();
        }

        // ── HTTP clients ──────────────────────────────────────────────────────
        foreach (var (name, baseUrl) in options.HttpClients)
        {
            var clientBuilder = services.AddHttpClient(name, client =>
            {
                if (!string.IsNullOrWhiteSpace(baseUrl))
                    client.BaseAddress = new Uri(baseUrl);
                client.Timeout = options.DefaultHttpTimeout;
            });

            if (options.EnableResilience)
                clientBuilder.AddAmpResilienceHandler(options.ResilienceOptions);
        }

        // Register the general-purpose pipeline for non-HTTP resilience (DB, AWS SDK, etc.)
        if (options.EnableResilience)
            services.AddAmpResiliencePipeline();

        services.AddScoped<IHttpClientService>(sp =>
        {
            var factory = sp.GetRequiredService<IHttpClientFactory>();
            var logger = sp.GetRequiredService<ILogger<HttpClientService>>();
            return new HttpClientService(factory.CreateClient(options.DefaultHttpClientName), logger);
        });

        services.AddSingleton<IAmpHttpClientFactory>(sp =>
            new AmpHttpClientFactory(sp));

        // ── Secrets service ───────────────────────────────────────────────────
        if (options.RegisterSecretsService)
        {
            var secretName = EnvironmentHelper.AwsSecretName
                ?? configuration["Aws:SecretsManager:SecretName"]
                ?? "amp/config";
            var region = EnvironmentHelper.AwsRegion;

            services.AddSingleton<ISecretsService>(sp =>
                new AwsSecretsService(
                    secretName, region,
                    sp.GetRequiredService<ILogger<AwsSecretsService>>(),
                    options.SecretsCacheOptions));
        }

        return services;
    }
}

