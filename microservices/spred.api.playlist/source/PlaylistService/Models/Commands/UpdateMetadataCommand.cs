using MediatR;
using PlaylistService.Models.DTO;
using Spred.Bus.DTOs;

namespace PlaylistService.Models.Commands;

/// <summary>
/// Represents a command for updating playlist metadata.
/// Used within the Playlist Service to synchronize public metadata updates
/// coming from API routes or external enrichment sources.
/// </summary>
public record UpdateMetadataCommand : INotification
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMetadataCommand"/> class.
    /// </summary>
    public UpdateMetadataCommand()
    {
        PrimaryId = new PrimaryId();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateMetadataCommand"/> class
    /// using data from a <see cref="PublicMetadataDto"/>.
    /// </summary>
    /// <param name="metadata">The source metadata DTO containing playlist information.</param>
    public UpdateMetadataCommand(PublicMetadataDto metadata)
    {
        Name = metadata.Name;
        Description = metadata.Description;
        ImageUrl = metadata.ImageUrl;
        ListenUrls = metadata.ListenUrls;
        SubmitUrls = metadata.SubmitUrls;
        SubmitEmail = metadata.SubmitEmail;
    }

    /// <summary>
    /// Gets or sets the unique identifier of the playlist metadata.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the primary identifier used to link metadata with external platforms.
    /// </summary>
    public PrimaryId PrimaryId { get; set; }

    /// <summary>
    /// Gets or sets the unique Spred user identifier associated with this metadata.
    /// </summary>
    public Guid SpredUserId { get; set; }

    /// <summary>
    /// Gets or sets the playlist name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the playlist description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of listen URLs by platform.
    /// </summary>
    public Dictionary<string, string>? ListenUrls { get; set; }

    /// <summary>
    /// Gets or sets the dictionary of submit URLs by platform.
    /// </summary>
    public Dictionary<string, string>? SubmitUrls { get; set; }

    /// <summary>
    /// Gets or sets the URL of the playlist image.
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the total number of tracks in the playlist.
    /// </summary>
    public uint? TracksTotal { get; set; }

    /// <summary>
    /// Gets or sets the number of followers of the playlist.
    /// </summary>
    public uint? Followers { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is public.
    /// </summary>
    public bool? IsPublic { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the playlist is collaborative.
    /// </summary>
    public bool? Collaborative { get; set; }

    /// <summary>
    /// Gets or sets the email address for track submissions.
    /// </summary>
    public string? SubmitEmail { get; set; }

    /// <summary>
    /// Gets or sets the internal metadata type (e.g., "playlistMetadata", "recordLabelMetadata").
    /// </summary>
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the canonical HREF link of the playlist metadata.
    /// </summary>
    public string? Href { get; set; }

    /// <summary>
    /// Gets or sets the current fetch status for the metadata lifecycle.
    /// </summary>
    public FetchStatus? Status { get; set; }

    /// <summary>
    /// Gets or sets the Chartmetric internal identifier, if available.
    /// </summary>
    public string? ChartmetricsId { get; set; }

    /// <summary>
    /// Gets or sets the SoundCharts internal identifier, if available.
    /// </summary>
    public string? SoundChartsId { get; set; }

    /// <summary>
    /// Gets or sets the list of metadata tags associated with the playlist.
    /// </summary>
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Gets or sets the platform display name of the playlist owner.
    /// </summary>
    public string? OwnerPlatformName { get; set; }

    /// <summary>
    /// Gets or sets the platform-specific owner identifier.
    /// </summary>
    public string? OwnerPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the linked user identifier within the platform.
    /// </summary>
    public string? UserPlatformId { get; set; }

    /// <summary>
    /// Gets or sets the catalog classification type (e.g., “playlist”, “record label”).
    /// </summary>
    public string? CatalogType { get; set; }

    /// <summary>
    /// Gets or sets the ratio of active tracks or engagement over the playlist lifetime.
    /// </summary>
    public double? ActiveRatio { get; set; }

    /// <summary>
    /// Gets or sets the suspicious activity score associated with the playlist.
    /// </summary>
    public double? SuspicionScore { get; set; }

    /// <summary>
    /// Gets or sets the ranked list of moods associated with the playlist.
    /// </summary>
    public Dictionary<int, string>? Moods { get; set; }

    /// <summary>
    /// Gets or sets the ranked list of activities associated with the playlist.
    /// </summary>
    public Dictionary<int, string>? Activities { get; set; }

    /// <summary>
    /// Gets or sets the list of track identifiers linked to this metadata.
    /// </summary>
    public List<Guid> Tracks { get; set; } = [];

    /// <summary>
    /// Gets or sets a flag indicating whether the statistics were updated.
    /// </summary>
    public bool StatsUpdated { get; set; }
    
    /// <summary>
    /// Gets the name of the country associated with the metadata entry.
    /// </summary>
    public string? Country { get; set; }

    /// <summary>
    /// Gets the name of the city associated with this object.
    /// </summary>
    public string? CityName { get; set; }

    /// <summary>
    /// Gets the code representing the country associated with the metadata.
    /// </summary>
    public string? CountryCode { get; set; }

    /// <summary>
    /// Gets the time zone associated with the entity.
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Gets the URL to the submission form associated with the metadata.
    /// </summary>
    public string? SubmissionFormUrl { get; set; }

    /// <summary>
    /// Provides detailed instructions for submitting content or materials.
    /// </summary>
    public string? SubmissionInstructions { get; set; }

    /// <summary>
    /// Gets the specified requirements or guidelines for music submissions.
    /// </summary>
    public string? MusicRequirements { get; set; }

    /// <summary>
    /// Gets the URL containing additional information related to the music submission process.
    /// </summary>
    public string? SubmissionInfoUrl { get; set; }

    /// <summary>
    /// Indicates whether submissions are welcomed or encouraged.
    /// </summary>
    public bool? SubmissionFriendly { get; set; }

    /// <summary>
    /// Gets the type of payment associated with the entity.
    /// </summary>
    public string? PaymentType { get; set; }

    /// <summary>
    /// Gets the price associated with a payment for a submission or service.
    /// </summary>
    public string? PaymentPrice { get; set; }

    /// <summary>
    /// Gets the details related to the payment process, such as additional information or terms.
    /// </summary>
    public string? PaymentDetails { get; set; }

    /// <summary>
    /// Gets the scope of localization for the entity, representing context information
    /// that defines localized content or behavior.
    /// </summary>
    public string? LocalizationScope { get; set; }

    /// <summary>
    /// Gets the region associated with localization for the entity.
    /// </summary>
    public string? LocalizationRegion { get; set; }

    /// <summary>
    /// Gets the priority level used for localization purposes, indicating the significance or precedence
    /// of the localization in the context of content or feature targeting.
    /// </summary>
    public string? LocalizationPriority { get; set; }

    /// <summary>
    /// Defines the scope or range of the intended audience.
    /// </summary>
    public string? AudienceScope { get; set; }

    /// <summary>
    /// Gets the broadcast format associated with the metadata.
    /// </summary>
    public string? BroadcastFormat { get; set; }

    /// <summary>
    /// Gets the frequency at which the curated content is updated or refreshed.
    /// </summary>
    public string? CurationFrequency { get; set; }

    /// <summary>
    /// Gets the rate of feedback provided, typically represented as a percentage or ratio.
    /// </summary>
    public double? FeedbackRate { get; set; }

    /// <summary>
    /// Gets the estimated number of people or audience that the playlist or content can potentially reach.
    /// </summary>
    public int? Reach { get; set; }
}