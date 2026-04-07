using Microsoft.Extensions.Configuration;

namespace Amp.Core.Extensions.Configuration;

internal sealed class AwsSecretsManagerSource(string secretName, string region) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder)
        => new AwsSecretsManagerProvider(secretName, region);
}
