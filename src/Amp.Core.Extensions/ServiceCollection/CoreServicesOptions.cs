namespace Amp.Core.Extensions.ServiceCollection;

/// <summary>
/// Top-level options for common AMP infrastructure registrations.
/// Pass a configure delegate to <c>AddCoreServices</c> to override defaults.
/// </summary>
public sealed class CoreServicesOptions
{
    /// <summary>
    /// Registers ASP.NET Core response compression middleware. Default: <c>true</c>.
    /// </summary>
    public bool EnableResponseCompression { get; set; } = true;

    /// <summary>
    /// Registers the <c>AmpCorePolicy</c> CORS policy. Default: <c>true</c>.
    /// Use <see cref="AllowedOrigins"/> to restrict which origins are permitted.
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Origins permitted by the <c>AmpCorePolicy</c> CORS policy.
    /// Use <c>["*"]</c> (default) to allow any origin without credentials — suitable
    /// for development. In production, specify explicit origins so that credentials
    /// (cookies, Authorization headers) can be allowed by the browser.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = ["*"];

    /// <summary>
    /// Registers API versioning services (Asp.Versioning). Default: <c>true</c>.
    /// </summary>
    public bool EnableApiVersioning { get; set; } = true;

    /// <summary>
    /// Optional delegate to override API versioning defaults.
    /// Only used when <see cref="EnableApiVersioning"/> is <c>true</c>.
    /// </summary>
    public Action<AmpApiVersioningOptions>? ConfigureApiVersioning { get; set; }
}
