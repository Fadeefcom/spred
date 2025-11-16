using System.Text.Json;

namespace AggregatorService.Extensions;

/// <summary>
/// Provides extension methods for <see cref="JsonElement"/>.
/// </summary>
public static class JsonElementExtensions
{
    /// <summary>
    /// Tries to get the value of a specified property from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="json">The <see cref="JsonElement"/> to search.</param>
    /// <param name="name">The name of the property to find.</param>
    /// <returns>
    /// The <see cref="JsonElement"/> value of the specified property if found; otherwise, the default <see cref="JsonElement"/>.
    /// </returns>
    public static JsonElement? TryGetValue(this JsonElement? json, string name)
    {
        if (!json.HasValue || json.Value.ValueKind != JsonValueKind.Object)
            return null;

        return json.Value.TryGetProperty(name, out var value) ? value : null;
    }

    /// <summary>
    /// Tries to get the value of a specified property from a <see cref="JsonElement"/>.
    /// </summary>
    /// <param name="json">The <see cref="JsonElement"/> to search.</param>
    /// <param name="name">The name of the property to find.</param>
    /// <returns>
    /// The <see cref="JsonElement"/> value of the specified property if found; otherwise, the default <see cref="JsonElement"/>.
    /// </returns>
    public static JsonElement? TryGetValue(this JsonElement json, string name)
    {
        return json.ValueKind == JsonValueKind.Object && json.TryGetProperty(name, out var value) ? value : null;
    }

    /// <summary>
    /// Retrieves the string representation of a <see cref="JsonElement"/> or null if the value is not present.
    /// </summary>
    /// <param name="json">The <see cref="JsonElement"/> to retrieve the value from.</param>
    /// <returns>
    /// A string representation of the <see cref="JsonElement"/> if it is a string, number, boolean, or other convertible type;
    /// otherwise, null if the value is undefined or null.
    /// </returns>
    public static string? GetStringOrNull(this JsonElement? json)
    {
        if(!json.HasValue)
            return null;
        return GetStringOrNull(json.Value);
    }

    public static string? GetStringOrNull(this JsonElement json)
    {
        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number => json.ToString(),
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Null => null,
            JsonValueKind.Undefined => null,
            _ => json.ToString()
        };
    }
    
    public static bool GetBoolOrFalse(this JsonElement? element) =>
        element?.ValueKind == JsonValueKind.True || element?.ValueKind != JsonValueKind.False && (bool.TryParse(element?.GetString(), out var parsed) && parsed);
    
    public static int GetIntOrDefault(this JsonElement? element) =>
        element.HasValue && element.Value.TryGetInt32(out var result) ? result : 0;
    
    public static double GetDoubleOrDefault(this JsonElement? element) =>
        element.HasValue && element.Value.TryGetDouble(out var result) ? result : 0;

    public static IEnumerable<JsonElement> EnumerateArraySafe(this JsonElement? element) => 
        element is { ValueKind: JsonValueKind.Array } e
            ? e.EnumerateArray()
            : Enumerable.Empty<JsonElement>();
    
    public static IEnumerable<JsonElement> EnumerateArraySafe(this JsonElement element) => 
        element is { ValueKind: JsonValueKind.Array } e
            ? e.EnumerateArray()
            : Enumerable.Empty<JsonElement>();
    
    public static uint GetUIntOrDefault(this JsonElement? element)
     => element is { ValueKind: JsonValueKind.Number } && element.Value.TryGetInt32(out var val)
                                                       && val >= 0 ? (uint)val : 0;
    
    public static DateTime GetDateTimeOrDefault(this JsonElement? element, DateTime fallback)
    {
        if (element.HasValue)
            return GetDateTimeOrDefault(element.Value, fallback);
        return fallback;
    }
    
    public static DateTime GetDateTimeOrDefault(this JsonElement element, DateTime fallback)
    {
        if (element is { ValueKind: JsonValueKind.String } && DateTime.TryParse(element.GetString(), out var dt))
            return dt;
        return fallback;
    }
}