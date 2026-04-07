namespace Amp.Core.Services.Abstractions.Http;

/// <summary>
/// Factory for creating named <see cref="IHttpClientService"/> instances
/// (e.g. one per downstream service with its own base URL and auth scheme).
/// </summary>
public interface IAmpHttpClientFactory
{
    /// <summary>Returns a pre-configured <see cref="IHttpClientService"/> for the named client.</summary>
    IHttpClientService CreateClient(string name);
}
