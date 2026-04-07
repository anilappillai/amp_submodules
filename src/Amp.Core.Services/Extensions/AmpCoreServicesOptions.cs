namespace Amp.Core.Services.Extensions;

public sealed class AmpCoreServicesOptions
{
    public bool UseRedisCache { get; set; } = true;
    public bool RegisterSecretsService { get; set; } = true;
    public string DefaultHttpClientName { get; set; } = "default";
    public TimeSpan DefaultHttpTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> HttpClients { get; set; } = new()
    {
        ["default"] = string.Empty
    };
}
