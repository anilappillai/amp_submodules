using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Registers Swagger/OpenAPI with versioned documents and JWT bearer auth support.
/// Usage: builder.Services.AddCoreSwagger("Amp.Facebook.Api", "Social platform API");
/// </summary>
public static class SwaggerExtensions
{
    public static IServiceCollection AddCoreSwagger(
        this IServiceCollection services,
        string title,
        string description = "",
        bool enableJwtAuth = true)
    {
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>>(sp =>
            new ConfigureSwaggerOptions(sp.GetRequiredService<IApiVersionDescriptionProvider>(), title, description));

        services.AddSwaggerGen(options =>
        {
            if (!enableJwtAuth) return;

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token (without the 'Bearer' prefix)."
            });

            options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    []
                }
            });

            options.OperationFilter<CorrelationIdOperationFilter>();
        });

        return services;
    }

    /// <summary>
    /// Enables Swagger and SwaggerUI (with versioned endpoints) in Development.
    /// Usage: app.UseAmpCoreSwagger();
    /// </summary>
    public static IApplicationBuilder UseAmpCoreSwagger(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
            return app;

        app.UseSwagger();
        app.UseSwaggerUI(opts =>
        {
            foreach (var desc in app.DescribeApiVersions())
                opts.SwaggerEndpoint($"/swagger/{desc.GroupName}/swagger.json", desc.GroupName);
        });

        return app;
    }
}

