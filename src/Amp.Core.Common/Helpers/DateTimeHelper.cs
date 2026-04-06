namespace Amp.Core.Common.Helpers;

/// <summary>
/// Date/time utilities consistent across all AMP services.
/// All timestamps stored in the database and sent in API responses use UTC.
/// </summary>
public static class DateTimeHelper
{
    /// <summary>Returns the current UTC timestamp.</summary>
    public static DateTime UtcNow => DateTime.UtcNow;

    /// <summary>Converts a local <see cref="DateTime"/> to UTC, handling unspecified kind safely.</summary>
    public static DateTime ToUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc)
    };

    /// <summary>Returns the start of the day (00:00:00) in UTC for the given date.</summary>
    public static DateTime StartOfDay(DateTime date) =>
        new(date.Year, date.Month, date.Day, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Returns the end of the day (23:59:59.999) in UTC for the given date.</summary>
    public static DateTime EndOfDay(DateTime date) =>
        new(date.Year, date.Month, date.Day, 23, 59, 59, 999, DateTimeKind.Utc);

    /// <summary>Returns midnight UTC for today minus <paramref name="days"/> days.</summary>
    public static DateTime DaysAgo(int days) => StartOfDay(UtcNow.AddDays(-days));

    /// <summary>Formats a UTC timestamp as an ISO-8601 string.</summary>
    public static string ToIso8601(DateTime dt) => dt.ToString("yyyy-MM-ddTHH:mm:ssZ");

    /// <summary>Unix epoch (seconds) → UTC DateTime.</summary>
    public static DateTime FromUnixSeconds(long seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;

    /// <summary>UTC DateTime → Unix epoch seconds.</summary>
    public static long ToUnixSeconds(DateTime dt) =>
        new DateTimeOffset(ToUtc(dt)).ToUnixTimeSeconds();
}
