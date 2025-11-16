using AutoMapper;
using PlaylistService.Models;
using PlaylistService.Models.Commands;
using PlaylistService.Models.DTO;
using PlaylistService.Models.Entities;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace PlaylistService.Configuration;

/// <summary>
/// Represents the AutoMapper profile for configuring object-object mapping within the PlaylistService application.
/// </summary>
/// <remarks>
/// This class inherits from the <see cref="AutoMapper.Profile"/> base class and provides a centralized location
/// for defining mapping configurations. It is registered in the dependency injection container and used by
/// AutoMapper during runtime to perform mapping between source and destination types.
/// The configuration defined here is applied to classes involved in various services, ensuring standardized
/// and maintainable object transformations.
/// This profile is applied within tests and the application's startup to support functionalities including:
/// - Object mapping in consumer use cases.
/// - Mapping configuration in related tests.
/// - Dependency injection for the AutoMapper library.
/// </remarks>
public class MappingProfile : Profile
{
    private readonly char[] _splitter = [':'];

    /// <summary>
    /// Represents a configuration class that defines object-object mapping rules.
    /// Inherits from the AutoMapper Profile class, allowing the creation of
    /// mappings between different types.
    /// </summary>
    public MappingProfile()
    {
        // MetadataDto → CatalogMetadata
        CreateMap<MetadataDto, CatalogMetadata>()
            .ForMember(d => d.CreatedAt, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.ETag, opt => opt.Ignore())
            .ForMember(d => d.Timestamp, opt => opt.Ignore())
            .ForMember(d => d.NeedUpdateStatInfo, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.Bucket, opt => opt.Ignore())
            .ForMember(d => d.Type, s => s.MapFrom(src =>
                src.Type == "playlist" || src.Type == "playlistMetadata" ? "playlist" : "record_label"))
            .ForMember(d => d.PrimaryId, opt => opt.MapFrom(s => PrimaryId.Parse(s.PrimaryId!)));

        // CatalogMetadata → MetadataDto
        CreateMap<CatalogMetadata, MetadataDto>()
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Type, opt => opt.MapFrom(src =>
                src.Type == "playlist" || src.Type == "playlistMetadata" ? "playlist" : "record_label"))
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());
        
        CreateMap<CatalogMetadata, PrivateMetadataDto>()
            .ForMember(d => d.FollowerChange, opt => opt.Ignore()) // этого поля нет в CatalogMetadata
            .ForMember(d => d.Platform, opt => opt.MapFrom(s => s.PrimaryId.Platform))
            .ForMember(d => d.Type, opt => opt.MapFrom(s =>
                s.Type == "playlist" || s.Type == "playlistMetadata" ? "playlist" : "record_label"));

        // CatalogMetadata → PublicMetadataDto
        CreateMap<CatalogMetadata, PublicMetadataDto>()
            .ForMember(d => d.Type, s => s.MapFrom(src =>
                src.Type == "playlist" || src.Type == "playlistMetadata" ? "playlist" : "record_label"))
            .ForMember(d => d.FollowerChange, opt => opt.Ignore())
            .ForMember(d => d.Platform, opt => opt.MapFrom(s => s.PrimaryId.Platform));

        // MetadataDto → CreateMetadataCommand
        CreateMap<MetadataDto, CreateMetadataCommand>()
            .ForMember(d => d.PrimaryId,
                opt => opt.MapFrom(s => PrimaryId.Parse(s.PrimaryId!)))
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Bucket, opt => opt.Ignore());

        // MetadataDto → UpdateMetadataCommand
        CreateMap<MetadataDto, UpdateMetadataCommand>()
            .ForMember(d => d.PrimaryId,
                opt => opt.MapFrom(s => PrimaryId.Parse(s.PrimaryId!)))
            .ForMember(d => d.StatsUpdated, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore());

        // MetadataTracksDto → UpdateMetadataCommand
        CreateMap<MetadataTracksDto, UpdateMetadataCommand>()
            .ForMember(d => d.PrimaryId,
                opt => opt.MapFrom(s => PrimaryId.Parse(s.PrimaryId!)))
            .ForMember(d => d.StatsUpdated, opt => opt.Ignore())
            .ForMember(d => d.Tracks, opt => opt.MapFrom(s => s.Tracks.Select(t => t.Id)));

        // CatalogCreate → UpdateMetadataCommand
        CreateMap<CatalogCreate, UpdateMetadataCommand>()
            .ForMember(d => d.PrimaryId, opt => opt.Ignore())
            .ForMember(d => d.ListenUrls, opt => opt.Ignore())
            .ForMember(d => d.SubmitUrls, opt => opt.Ignore())
            .ForMember(d => d.Collaborative, opt => opt.Ignore())
            .ForMember(d => d.SubmitEmail, opt => opt.Ignore())
            .ForMember(d => d.StatsUpdated, opt => opt.MapFrom(_ => false))
            .ForMember(d => d.CatalogType, opt => opt.MapFrom(_ => ""))
            .ForMember(d => d.Tracks, opt => opt.MapFrom(s => (s.Tracks ?? new List<TrackDto>()).Select(t => t.Id)))
            .ForMember(d => d.OwnerPlatformName, opt => opt.Ignore())
            .ForMember(d => d.OwnerPlatformId, opt => opt.Ignore())
            .ForMember(d => d.UserPlatformId, opt => opt.Ignore())
            .ForMember(d => d.ActiveRatio, opt => opt.Ignore())
            .ForMember(d => d.SuspicionScore, opt => opt.Ignore())
            .ForMember(d => d.Moods, opt => opt.Ignore())
            .ForMember(d => d.Activities, opt => opt.Ignore())
            .ForMember(d => d.Tags, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.Href, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());

        // CatalogCreate → CreateMetadataCommand
        CreateMap<CatalogCreate, CreateMetadataCommand>()
            .ForMember(d => d.Tracks, opt =>
                opt.MapFrom(s => (s.Tracks ?? new List<TrackDto>()).Select(t => t.Id).ToList()))
            .ForMember(d => d.PrimaryId,
                opt => opt.Ignore())
            .ForMember(d => d.Href, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.ChartmetricsId, opt => opt.Ignore())
            .ForMember(d => d.SoundChartsId, opt => opt.Ignore())
            .ForMember(d => d.Tags, opt => opt.Ignore())
            .ForMember(d => d.OwnerPlatformName, opt => opt.Ignore())
            .ForMember(d => d.OwnerPlatformId, opt => opt.Ignore())
            .ForMember(d => d.UserPlatformId, opt => opt.Ignore())
            .ForMember(d => d.CatalogType, opt => opt.Ignore())
            .ForMember(d => d.ActiveRatio, opt => opt.Ignore())
            .ForMember(d => d.SuspicionScore, opt => opt.Ignore())
            .ForMember(d => d.Moods, opt => opt.Ignore())
            .ForMember(d => d.Activities, opt => opt.Ignore())
            .ForMember(d => d.Bucket, opt => opt.Ignore())
            .ForMember(d => d.Country, opt => opt.Ignore())
            .ForMember(d => d.CityName, opt => opt.Ignore())
            .ForMember(d => d.CountryCode, opt => opt.Ignore())
            .ForMember(d => d.TimeZone, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFormUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInstructions, opt => opt.Ignore())
            .ForMember(d => d.MusicRequirements, opt => opt.Ignore())
            .ForMember(d => d.SubmissionInfoUrl, opt => opt.Ignore())
            .ForMember(d => d.SubmissionFriendly, opt => opt.Ignore())
            .ForMember(d => d.PaymentType, opt => opt.Ignore())
            .ForMember(d => d.PaymentPrice, opt => opt.Ignore())
            .ForMember(d => d.PaymentDetails, opt => opt.Ignore())
            .ForMember(d => d.LocalizationScope, opt => opt.Ignore())
            .ForMember(d => d.LocalizationRegion, opt => opt.Ignore())
            .ForMember(d => d.LocalizationPriority, opt => opt.Ignore())
            .ForMember(d => d.AudienceScope, opt => opt.Ignore())
            .ForMember(d => d.BroadcastFormat, opt => opt.Ignore())
            .ForMember(d => d.CurationFrequency, opt => opt.Ignore())
            .ForMember(d => d.FeedbackRate, opt => opt.Ignore())
            .ForMember(d => d.Reach, opt => opt.Ignore());

        // MetadataDto → PublicMetadataDto
        CreateMap<MetadataDto, PublicMetadataDto>()
            .ForMember(d => d.FollowerChange, opt => opt.Ignore())
            .ForMember(d => d.UpdatedAt, opt => opt.Ignore())
            .ForMember(d => d.Platform, opt => opt.MapFrom(s => s.PrimaryId.Split(_splitter)[0]));
    }
}
