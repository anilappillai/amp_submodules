using System.Runtime.CompilerServices;

namespace Amp.Core.Common.Helpers;

/// <summary>
/// Lightweight guard clauses that throw standardised exceptions on invalid input.
/// </summary>
public static class Guard
{
    /// <summary>Throws <see cref="ArgumentNullException"/> when <paramref name="value"/> is null.</summary>
    public static T NotNull<T>(T? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : class
    {
        if (value is null)
            throw new ArgumentNullException(paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="value"/> is null or whitespace.</summary>
    public static string NotNullOrWhiteSpace(string? value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Value cannot be null or whitespace.", paramName);
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when <paramref name="value"/> is less than or equal to zero.</summary>
    public static T Positive<T>(T value, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : struct, IComparable<T>
    {
        if (value.CompareTo(default) <= 0)
            throw new ArgumentOutOfRangeException(paramName, value, "Value must be positive.");
        return value;
    }

    /// <summary>Throws <see cref="ArgumentOutOfRangeException"/> when value is outside [<paramref name="min"/>, <paramref name="max"/>].</summary>
    public static T InRange<T>(T value, T min, T max, [CallerArgumentExpression(nameof(value))] string? paramName = null)
        where T : IComparable<T>
    {
        if (value.CompareTo(min) < 0 || value.CompareTo(max) > 0)
            throw new ArgumentOutOfRangeException(paramName, value, $"Value must be between {min} and {max}.");
        return value;
    }

    /// <summary>Throws <see cref="ArgumentException"/> when <paramref name="collection"/> is null or empty.</summary>
    public static IEnumerable<T> NotEmpty<T>(IEnumerable<T>? collection, [CallerArgumentExpression(nameof(collection))] string? paramName = null)
    {
        var list = collection?.ToList() ?? throw new ArgumentNullException(paramName);
        if (list.Count == 0)
            throw new ArgumentException("Collection must not be empty.", paramName);
        return list;
    }
}
