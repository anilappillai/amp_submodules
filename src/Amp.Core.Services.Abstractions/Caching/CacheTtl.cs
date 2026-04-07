namespace Amp.Core.Services.Abstractions.Caching;

/// <summary>Default cache TTL constants used across AMP services.</summary>
public static class CacheTtl
{
    public static readonly TimeSpan Short = TimeSpan.FromMinutes(5);
    public static readonly TimeSpan Medium = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Long = TimeSpan.FromHours(1);
    public static readonly TimeSpan Day = TimeSpan.FromHours(24);
}
