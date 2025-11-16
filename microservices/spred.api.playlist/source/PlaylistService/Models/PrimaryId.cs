using Exception.Exceptions;
using Newtonsoft.Json;
using PlaylistService.Configuration;

namespace PlaylistService.Models;

/// <summary>
/// Represents a composite identifier uniquely defining an entity using its platform, type, and id components.
/// </summary>
/// <remarks>
/// This struct facilitates consistent identification of entities by employing the "platform:type:id" format.
/// It includes validation mechanisms to ensure correctness, supports string parsing for conversion,
/// and is designed as immutable to guarantee thread-safe operation.
/// </remarks>
/// <threadsafety>
/// The struct is immutable, ensuring its thread-safe nature.
/// </threadsafety>
/// <seealso cref="PrimaryIdConverter"/>
/// <seealso cref="PlaylistService.Models.Commands.CreateMetadataCommand"/>
[JsonConverter(typeof(PrimaryIdConverter))]
public readonly struct PrimaryId
{
    /// Gets the platform associated with the primary identifier.
    /// Represents the source or platform where the identifier originates.
    /// This property is mandatory and is validated to ensure it is not null, empty, or whitespace.
    public string Platform { get; }

    public string Type { get; }

    /// Gets the unique identifier associated with this PrimaryId instance.
    /// Represents the specific ID value within the overall platform and type context.
    /// This property is mandatory and cannot be null, empty, or whitespace.
    /// Throws a validation exception if the value is improperly initialized.
    public string Id { get; }


    /// <summary>
    /// Represents a unique identifier consisting of three components: platform, type, and id.
    /// </summary>
    /// <remarks>
    /// This structure enforces a specific format for identifying entities across different platforms and types.
    /// </remarks>
    /// <exception cref="BaseException">
    /// Thrown when any of the components (platform, type, or id) are null, empty, or whitespace-only during initialization.
    /// </exception>
    public PrimaryId(string platform, string type, string id)
    {
        if (string.IsNullOrWhiteSpace(platform))
            throw new BaseException("Platform cannot be empty.",  400, "Validation error",nameof(platform), "PrimaryId");
        if (string.IsNullOrWhiteSpace(type))
            throw new BaseException("Type cannot be empty.",  400, "Validation error",nameof(type), "PrimaryId");
        if (string.IsNullOrWhiteSpace(id))
            throw new BaseException("Id cannot be empty.",  400, "Validation error",nameof(id), "PrimaryId");

        Platform = platform.Trim();
        Type = type.Trim();
        Id = id.Trim();
    }

    /// <summary>
    /// Parses a string value into a <see cref="PrimaryId"/> instance.
    /// </summary>
    /// <param name="value">The string value to parse, expected in the format "platform:type:id".</param>
    /// <returns>A <see cref="PrimaryId"/> created from the parsed components of the input string.</returns>
    /// <exception cref="BaseException">
    /// Thrown when the input <paramref name="value"/> is null, empty, consists only of whitespace,
    /// or does not follow the required format "platform:type:id".
    /// </exception>
    public static PrimaryId Parse(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new BaseException("PrimaryId cannot be empty.",  400, "Validation error",nameof(value), "PrimaryId");

        var parts = value.Split(':');
        if (parts.Length != 3)
            throw new BaseException("PrimaryId must follow format 'platform:type:id'.",  400, "Validation error",nameof(value), "PrimaryId");

        return new PrimaryId(parts[0], parts[1], parts[2]);
    }

    /// <summary>
    /// Converts the current PrimaryId instance into its string representation.
    /// </summary>
    /// <returns>
    /// A string that concatenates the Platform, Type, and Id components of the PrimaryId, separated by colons.
    /// </returns>
    public override string ToString() => $"{Platform}:{Type}:{Id}";

    /// <summary>
    /// Defines an operator that implicitly converts a PrimaryId instance to its string representation.
    /// </summary>
    /// <param name="primaryId">
    /// The PrimaryId instance to be converted to a string.
    /// </param>
    /// <returns>
    /// A string representation of the PrimaryId, formatted as "platform:type:id".
    /// </returns>
    public static implicit operator string(PrimaryId primaryId) => primaryId.ToString();

    /// <summary>
    /// Defines an explicit conversion operator to convert a string value to a PrimaryId instance.
    /// </summary>
    /// <param name="value">The string value to be converted. It must follow the format 'platform:type:id'.</param>
    /// <returns>A new PrimaryId instance created based on the input string value.</returns>
    /// <exception cref="BaseException">
    /// Thrown if the input string is null, empty, or does not follow the 'platform:type:id' format.
    /// </exception>
    public static explicit operator PrimaryId(string value) => Parse(value);
}