using System.ComponentModel.DataAnnotations;

namespace TrackService.Configuration;

/// <summary>
/// Represents the configuration options for Blob storage.
/// </summary>
public sealed record BlobOptions
{
    /// <summary>
    /// Section name
    /// </summary>
    public const string SectionName = "BlobOptions";

    /// <summary>
    /// Gets the private connection string for the Blob storage.
    /// </summary>
    [RegularExpression(@"^[\S]{100,250}$")]
    public required string BlobConnectString { get; init; }

    /// <summary>
    /// Gets the name of the Blob container.
    /// </summary>
    [RegularExpression(@"^[\S]{1,40}$")]
    public required string ContainerName { get; init; }
}
