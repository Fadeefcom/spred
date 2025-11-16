using Extensions.Utilities;
using MediatR;
using Spred.Bus.DTOs;

namespace PlaylistService.Models.Commands;

/// <summary>
/// Command to create new playlist metadata.
/// </summary>
public record CreateMetadataCommand : IRequest<Guid>
{
    public Guid Id { get; init; }
    public required PrimaryId PrimaryId { get; set; }
    public Guid SpredUserId { get; set; }

    public string Bucket { get; set; } = "00";

    public string? Name { get; init; }
    public string? Description { get; init; }
    public Dictionary<string, string>? ListenUrls { get; init; }
    public Dictionary<string, string>? SubmitUrls { get; init; }
    public string? ImageUrl { get; init; }
    public uint? TracksTotal { get; init; }
    public uint? Followers { get; init; }
    public bool IsPublic { get; init; }
    public bool Collaborative { get; init; }
    public string? SubmitEmail { get; init; }
    public string? Type { get; set; }
    public string? Href { get; set; }
    public FetchStatus Status { get; set; } = FetchStatus.Init;

    public string? ChartmetricsId { get; set; }
    public string? SoundChartsId { get; set; }

    public List<string>? Tags { get; set; }
    public string? OwnerPlatformName { get; set; }
    public string? OwnerPlatformId { get; set; }
    public string? UserPlatformId { get; set; }
    public string? CatalogType { get; set; }

    public double? ActiveRatio { get; set; }
    public double? SuspicionScore { get; set; }

    public Dictionary<int, string>? Moods { get; set; }
    public Dictionary<int, string>? Activities { get; set; }

    public List<Guid> Tracks { get; set; } = [];
    
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
    public bool SubmissionFriendly { get; set; }

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