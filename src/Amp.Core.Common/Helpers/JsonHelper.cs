using System.Text.Json;
using System.Text.Json.Serialization;

namespace Amp.Core.Common.Helpers;

/// <summary>
/// Centralised JSON serialisation settings and convenience wrappers.
/// Use these throughout AMP services to ensure consistent serialisation behaviour.
/// </summary>
public static class JsonHelper
{
    /// <summary>
    /// Default options: camelCase, ignore nulls, allow trailing commas,
    /// enum strings, and read comments.
    /// </summary>
    public static readonly JsonSerializerOptions DefaultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>Serialises <paramref name="value"/> to a JSON string using <see cref="DefaultOptions"/>.</summary>
    public static string Serialize<T>(T value) =>
        JsonSerializer.Serialize(value, DefaultOptions);

    /// <summary>Deserialises a JSON string to <typeparamref name="T"/>. Returns <c>default</c> on failure.</summary>
    public static T? Deserialize<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(json, DefaultOptions);
        }
        catch
        {
            return default;
        }
    }

    /// <summary>Attempts deserialisation and returns whether it succeeded.</summary>
    public static bool TryDeserialize<T>(string json, out T? result)
    {
        result = Deserialize<T>(json);
        return result is not null;
    }
}
