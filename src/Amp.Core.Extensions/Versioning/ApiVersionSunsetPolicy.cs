namespace Amp.Core.Extensions.Versioning;

/// <summary>
/// Maps deprecated API versions to the date they will be removed (RFC 8594 Sunset).
/// Dates are emitted as a <c>Sunset</c> response header by <see cref="ApiVersionHeaderMiddleware"/>.
///
/// Register via <c>AddCoreApiVersioning</c>:
/// <code>
///   builder.Services.AddSingleton(new ApiVersionSunsetPolicy
///   {
///       ["1.0"] = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
///   });
/// </code>
/// </summary>
public sealed class ApiVersionSunsetPolicy : Dictionary<string, DateTimeOffset>
{
    /// <summary>Returns the sunset date for <paramref name="version"/>, or null if not configured.</summary>
    public DateTimeOffset? GetSunset(string version) =>
        TryGetValue(version, out var date) ? date : null;
}
