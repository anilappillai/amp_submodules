namespace Amp.Core.Extensions.ServiceCollection;

public sealed class CoreServicesOptions
{
    public bool EnableResponseCompression { get; set; } = true;
    public bool EnableCors { get; set; } = true;
    public string[] AllowedOrigins { get; set; } = ["*"];
    public bool EnableApiVersioning { get; set; } = true;
}
