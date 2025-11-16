using System.ComponentModel.DataAnnotations;
using PlaylistService.Models.Commands;

namespace PlaylistService.Models.Entities;

/// <summary>
/// Represents metadata specific to command playlists. Inherits from the <see cref="CatalogMetadata"/> class to
/// provide additional or differentiated behavior and properties for command-based configurations.
/// </summary>
public class RadioMetadata : CatalogMetadata
{
    /// <summary>
    /// Default .ctor
    /// </summary>
    public RadioMetadata() : base()
    {
        Type = "radio";
    }
    
    /// <summary>
    /// Gets the name of the country associated with the metadata entry.
    /// </summary>
    [StringLength(100)]
    public string Country { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the name of the city associated with this object.
    /// </summary>
    [StringLength(100)]
    public string CityName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the code representing the country associated with the metadata.
    /// </summary>
    [StringLength(10)]
    public string CountryCode { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the time zone associated with the entity.
    /// </summary>
    [StringLength(100)]
    public string TimeZone { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the URL to the submission form associated with the metadata.
    /// </summary>
    [StringLength(1000)]
    public string SubmissionFormUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Provides detailed instructions for submitting content or materials.
    /// </summary>
    [StringLength(1000)]
    public string SubmissionInstructions { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the specified requirements or guidelines for music submissions.
    /// </summary>
    [StringLength(1000)]
    public string MusicRequirements { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the URL containing additional information related to the music submission process.
    /// </summary>
    [StringLength(1000)]
    public string SubmissionInfoUrl { get; private set; } = string.Empty;

    /// <summary>
    /// Indicates whether submissions are welcomed or encouraged.
    /// </summary>
    public bool SubmissionFriendly { get; private set; }

    /// <summary>
    /// Gets the type of payment associated with the entity.
    /// </summary>
    [StringLength(100)]
    public string PaymentType { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the price associated with a payment for a submission or service.
    /// </summary>
    [StringLength(100)]
    public string PaymentPrice { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the details related to the payment process, such as additional information or terms.
    /// </summary>
    [StringLength(1000)]
    public string PaymentDetails { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the scope of localization for the entity, representing context information
    /// that defines localized content or behavior.
    /// </summary>
    [StringLength(1000)]
    public string LocalizationScope { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the region associated with localization for the entity.
    /// </summary>
    [StringLength(1000)]
    public string LocalizationRegion { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the priority level used for localization purposes, indicating the significance or precedence
    /// of the localization in the context of content or feature targeting.
    /// </summary>
    [StringLength(100)]
    public string LocalizationPriority { get; private set; } = string.Empty;

    /// <summary>
    /// Defines the scope or range of the intended audience.
    /// </summary>
    [StringLength(100)]
    public string AudienceScope { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the broadcast format associated with the metadata.
    /// </summary>
    [StringLength(100)]
    public string BroadcastFormat { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the frequency at which the curated content is updated or refreshed.
    /// </summary>
    [StringLength(100)]
    public string CurationFrequency { get; private set; } = string.Empty;

    /// <summary>
    /// Gets the rate of feedback provided, typically represented as a percentage or ratio.
    /// </summary>
    public double? FeedbackRate { get; private set; }

    /// <summary>
    /// Gets the estimated number of people or audience that the playlist or content can potentially reach.
    /// </summary>
    public int? Reach { get; private set; }
    
    /// <inheritdoc />
    public override void Create(CreateMetadataCommand command)
    {
        base.Create(command);
        
        Country = command.Country ?? string.Empty;
        CityName = command.CityName ?? string.Empty;
        CountryCode = command.CountryCode ?? string.Empty;
        TimeZone = command.TimeZone ?? string.Empty;
        SubmissionFormUrl = command.SubmissionFormUrl ?? string.Empty;
        SubmissionInstructions = command.SubmissionInstructions ?? string.Empty;
        MusicRequirements = command.MusicRequirements ?? string.Empty;
        SubmissionInfoUrl = command.SubmissionInfoUrl ?? string.Empty;
        SubmissionFriendly = command.SubmissionFriendly;
        PaymentType = command.PaymentType ?? string.Empty;
        PaymentPrice = command.PaymentPrice ?? string.Empty;
        PaymentDetails = command.PaymentDetails ?? string.Empty;
        LocalizationScope = command.LocalizationScope ?? string.Empty;
        LocalizationRegion = command.LocalizationRegion ?? string.Empty;
        LocalizationPriority = command.LocalizationPriority ?? string.Empty;
        AudienceScope = command.AudienceScope ?? string.Empty;
        BroadcastFormat = command.BroadcastFormat ?? string.Empty;
        CurationFrequency = command.CurationFrequency ?? string.Empty;
        FeedbackRate = command.FeedbackRate;
        Reach = command.Reach;
    }
    
    /// <inheritdoc />
    public override void Update(UpdateMetadataCommand metadata)
    {
        base.Update(metadata);
        
        if (!string.IsNullOrWhiteSpace(metadata.Country)) Country = metadata.Country;
        if (!string.IsNullOrWhiteSpace(metadata.CityName)) CityName = metadata.CityName;
        if (!string.IsNullOrWhiteSpace(metadata.CountryCode)) CountryCode = metadata.CountryCode;
        if (!string.IsNullOrWhiteSpace(metadata.TimeZone)) TimeZone = metadata.TimeZone;
        if (!string.IsNullOrWhiteSpace(metadata.SubmissionFormUrl)) SubmissionFormUrl = metadata.SubmissionFormUrl;
        if (!string.IsNullOrWhiteSpace(metadata.SubmissionInstructions)) SubmissionInstructions = metadata.SubmissionInstructions;
        if (!string.IsNullOrWhiteSpace(metadata.MusicRequirements)) MusicRequirements = metadata.MusicRequirements;
        if (!string.IsNullOrWhiteSpace(metadata.SubmissionInfoUrl)) SubmissionInfoUrl = metadata.SubmissionInfoUrl;
        if (metadata.SubmissionFriendly.HasValue) SubmissionFriendly = metadata.SubmissionFriendly.Value;
        if (!string.IsNullOrWhiteSpace(metadata.PaymentType)) PaymentType = metadata.PaymentType;
        if (!string.IsNullOrWhiteSpace(metadata.PaymentPrice)) PaymentPrice = metadata.PaymentPrice;
        if (!string.IsNullOrWhiteSpace(metadata.PaymentDetails)) PaymentDetails = metadata.PaymentDetails;
        if (!string.IsNullOrWhiteSpace(metadata.LocalizationScope)) LocalizationScope = metadata.LocalizationScope;
        if (!string.IsNullOrWhiteSpace(metadata.LocalizationRegion)) LocalizationRegion = metadata.LocalizationRegion;
        if (!string.IsNullOrWhiteSpace(metadata.LocalizationPriority)) LocalizationPriority = metadata.LocalizationPriority;
        if (!string.IsNullOrWhiteSpace(metadata.AudienceScope)) AudienceScope = metadata.AudienceScope;
        if (!string.IsNullOrWhiteSpace(metadata.BroadcastFormat)) BroadcastFormat = metadata.BroadcastFormat;
        if (!string.IsNullOrWhiteSpace(metadata.CurationFrequency)) CurationFrequency = metadata.CurationFrequency;
        if (metadata.FeedbackRate.HasValue) FeedbackRate = metadata.FeedbackRate;
        if (metadata.Reach.HasValue) Reach = metadata.Reach;
    }

}