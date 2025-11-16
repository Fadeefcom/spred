using System.Text.Json;
using System.Text.Json.Serialization;
using Spred.Bus.DTOs;

namespace TrackService.Helpers;

/// <summary>
/// Custom JSON converter for the <see cref="SourceType"/> enum that supports both integer and string representations during deserialization.
/// </summary>
public class SourceTypeConverter : JsonConverter<SourceType>
{
    /// <summary>
    /// Reads and converts the JSON to a <see cref="SourceType"/> enum value.
    /// Accepts both numeric (int) and textual (string) representations.
    /// </summary>
    /// <param name="reader">The reader to read from.</param>
    /// <param name="typeToConvert">The type to convert.</param>
    /// <param name="options">Serialization options to use.</param>
    /// <returns>The deserialized <see cref="SourceType"/> value.</returns>
    /// <exception cref="JsonException">Thrown when the value is not a valid int or string for <see cref="SourceType"/>.</exception>
    public override SourceType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var intValue))
        {
            return Enum.IsDefined(typeof(SourceType), intValue)
                ? (SourceType)intValue
                : throw new JsonException($"Invalid SourceType value: {intValue}");
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var strValue = reader.GetString();
            if (Enum.TryParse<SourceType>(strValue, ignoreCase: true, out var result))
                return result;

            throw new JsonException($"Invalid SourceType string: {strValue}");
        }

        throw new JsonException("Unexpected token type for SourceType.");
    }

    /// <summary>
    /// Writes a <see cref="SourceType"/> value as a string to JSON.
    /// </summary>
    /// <param name="writer">The writer to write to.</param>
    /// <param name="value">The <see cref="SourceType"/> value to convert.</param>
    /// <param name="options">Serialization options to use.</param>
    public override void Write(Utf8JsonWriter writer, SourceType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}