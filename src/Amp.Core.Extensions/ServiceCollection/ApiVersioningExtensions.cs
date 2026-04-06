using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Registers ASP.NET Core API versioning so all AMP APIs expose versioned endpoints
/// consistently (header-based + URL-segment-based).
/// Usage in Program.cs:  builder.Services.AddCoreApiVersioning();
/// </summary>
public static class ApiVersioningExtensions
{
    public static IServiceCollection AddCoreApiVersioning(
        this IServiceCollection services,
        int defaultMajorVersion = 1,
        int defaultMinorVersion = 0)
    {
        services
            .AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(defaultMajorVersion, defaultMinorVersion);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ReportApiVersions = true;

                // Support both header and URL segment versioning:
                //   Header:  api-version: 1.0
                //   URL:     /api/v1/resource
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new HeaderApiVersionReader("api-version"),
                    new UrlSegmentApiVersionReader());
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
