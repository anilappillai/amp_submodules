namespace Amp.Core.Extensions.ServiceCollection;

public sealed record AppVersionInfo(
    string ApplicationName,
    string Version,
    string BuildSha,
    DateTime StartedAt);
