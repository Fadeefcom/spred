using Newtonsoft.Json;
using PlaylistService.Models;

namespace PlaylistService.Configuration;

/// <summary>
/// Provides JSON serialization and deserialization logic for the PrimaryId type.
/// </summary>
/// <remarks>
/// PrimaryIdConverter is a custom implementation of Newtonsoft.Json.JsonConverter
/// for handling serialization and deserialization of the PrimaryId struct.
/// It ensures that the PrimaryId values are properly transformed to and from their string representation.
/// </remarks>
public class PrimaryIdConverter : JsonConverter<PrimaryId>
{
    /// Writes the JSON representation of the specified PrimaryId value.
    /// <param name="writer">The JsonWriter used to write the JSON representation.</param>
    /// <param name="value">The PrimaryId value to write as JSON.</param>
    /// <param name="serializer">The JsonSerializer to use for serialization.</param>
    public override void WriteJson(JsonWriter writer, PrimaryId value, JsonSerializer serializer)
    {
        writer.WriteValue(value.ToString());
    }

    /// Reads a JSON value and converts it into the corresponding PrimaryId object.
    /// <param name="reader">The JsonReader to read the value from.</param>
    /// <param name="objectType">The type of the object to be converted.</param>
    /// <param name="existingValue">The existing value of the object being read, used during deserialization, if applicable.</param>
    /// <param name="hasExistingValue">A boolean indicating whether an existing value is provided.</param>
    /// <param name="serializer">The JsonSerializer instance used to deserialize the JSON value.</param>
    /// <return>Returns a PrimaryId object created from the JSON value.</return>
    public override PrimaryId ReadJson(JsonReader reader, Type objectType, PrimaryId existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        var str = reader.Value?.ToString();
        return string.IsNullOrWhiteSpace(str) ? default : PrimaryId.Parse(str);
    }
}