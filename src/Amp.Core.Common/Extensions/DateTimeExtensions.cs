namespace Amp.Core.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>Returns true if the date is in the past (UTC).</summary>
    public static bool IsInThePast(this DateTime dt) => dt < DateTime.UtcNow;

    /// <summary>Returns true if the date is in the future (UTC).</summary>
    public static bool IsInTheFuture(this DateTime dt) => dt > DateTime.UtcNow;

    /// <summary>Returns true if the date is between start and end (inclusive).</summary>
    public static bool IsBetween(this DateTime dt, DateTime start, DateTime end) =>
        dt >= start && dt <= end;

    /// <summary>Returns the age in years from the date to now (UTC).</summary>
    public static int AgeInYears(this DateTime birthDate)
    {
        var today = DateTime.UtcNow;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }

    /// <summary>Returns the start of the week (Monday) for the given date.</summary>
    public static DateTime StartOfWeek(this DateTime dt) =>
        dt.AddDays(-(int)dt.DayOfWeek + (int)DayOfWeek.Monday).Date;

    /// <summary>Returns a UTC DateTimeOffset from a UTC DateTime.</summary>
    public static DateTimeOffset ToUtcOffset(this DateTime dt) =>
        new(DateTime.SpecifyKind(dt, DateTimeKind.Utc));

    /// <summary>Returns the relative time string (e.g. "3 hours ago").</summary>
    public static string ToRelativeTime(this DateTime dt)
    {
        var diff = DateTime.UtcNow - dt;
        return diff.TotalSeconds switch
        {
            < 60 => "just now",
            < 3600 => $"{(int)diff.TotalMinutes} minute{((int)diff.TotalMinutes == 1 ? "" : "s")} ago",
            < 86400 => $"{(int)diff.TotalHours} hour{((int)diff.TotalHours == 1 ? "" : "s")} ago",
            < 604800 => $"{(int)diff.TotalDays} day{((int)diff.TotalDays == 1 ? "" : "s")} ago",
            _ => dt.ToString("yyyy-MM-dd")
        };
    }
}
