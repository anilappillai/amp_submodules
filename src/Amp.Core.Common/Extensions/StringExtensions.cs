using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Amp.Core.Common.Extensions;

public static class StringExtensions
{
    /// <summary>Returns <c>true</c> if the string is null or whitespace.</summary>
    public static bool IsNullOrWhiteSpace(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>Returns <c>true</c> if the string has a non-whitespace value.</summary>
    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>Trims and returns the string, or null if it would be empty.</summary>
    public static string? TrimToNull(this string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Converts to camelCase (e.g. "MyProperty" → "myProperty").</summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    /// <summary>Converts to snake_case (e.g. "MyProperty" → "my_property").</summary>
    public static string ToSnakeCase(this string value) =>
        Regex.Replace(value, @"([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant();

    /// <summary>Truncates the string to at most <paramref name="maxLength"/> characters.</summary>
    public static string Truncate(this string value, int maxLength, string suffix = "...")
    {
        if (value.Length <= maxLength) return value;
        return string.Concat(value.AsSpan(0, maxLength - suffix.Length), suffix);
    }

    /// <summary>Converts to title case using the current culture.</summary>
    public static string ToTitleCase(this string value) =>
        CultureInfo.CurrentCulture.TextInfo.ToTitleCase(value.ToLowerInvariant());

    /// <summary>Removes all diacritical marks (accent characters) from the string.</summary>
    public static string RemoveDiacritics(this string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>Safe base-64 encode.</summary>
    public static string ToBase64(this string value) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    /// <summary>Safe base-64 decode.</summary>
    public static string FromBase64(this string value) =>
        Encoding.UTF8.GetString(Convert.FromBase64String(value));

    /// <summary>Masks a string leaving only the last <paramref name="visibleChars"/> characters visible.</summary>
    public static string Mask(this string value, int visibleChars = 4, char maskChar = '*')
    {
        if (value.Length <= visibleChars) return new string(maskChar, value.Length);
        return new string(maskChar, value.Length - visibleChars) + value[^visibleChars..];
    }
}
