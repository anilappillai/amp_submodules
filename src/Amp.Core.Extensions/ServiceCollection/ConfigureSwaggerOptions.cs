using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

internal sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    string title,
    string description) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(desc.GroupName, new OpenApiInfo
            {
                Title = title,
                Version = desc.ApiVersion.ToString(),
                Description = desc.IsDeprecated ? $"{description} — **DEPRECATED**" : description
            });
        }
    }
}
