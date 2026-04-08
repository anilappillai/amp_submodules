using Amp.Core.Extensions.Versioning;
using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Generates one <see cref="OpenApiInfo"/> per discovered API version.
/// Deprecated versions get a ⚠ banner in their document description.
/// </summary>
internal sealed class ConfigureSwaggerOptions(
    IApiVersionDescriptionProvider provider,
    string title,
    string description,
    AmpApiContact? contact = null) : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var desc in provider.ApiVersionDescriptions)
        {
            var docDescription = desc.IsDeprecated
                ? $"> ⚠ **This API version is deprecated.** Please migrate to the latest version.\n\n{description}"
                : description;

            var info = new OpenApiInfo
            {
                Title = title,
                Version = desc.ApiVersion.ToString(),
                Description = docDescription
            };

            if (contact is not null)
            {
                info.Contact = new OpenApiContact
                {
                    Name = contact.Name,
                    Email = contact.Email,
                    Url = contact.Url
                };
            }

            options.SwaggerDoc(desc.GroupName, info);
        }
    }
}
