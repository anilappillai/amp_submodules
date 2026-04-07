using Amp.Core.Services.Abstractions.Http;
using Amp.Core.Services.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Amp.Core.Services.Extensions;

internal sealed class AmpHttpClientFactory(IServiceProvider sp) : IAmpHttpClientFactory
{
    public IHttpClientService CreateClient(string name)
    {
        var factory = sp.GetRequiredService<IHttpClientFactory>();
        var logger = sp.GetRequiredService<ILogger<HttpClientService>>();
        return new HttpClientService(factory.CreateClient(name), logger);
    }
}
