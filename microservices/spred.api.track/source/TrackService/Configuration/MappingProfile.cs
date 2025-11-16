using AutoMapper;
using Spred.Bus.DTOs;
using TrackService.Models.Commands;
using TrackService.Models.DTOs;
using TrackService.Models.Entities;

namespace TrackService.Configuration;

/// <summary>
/// AutoMapper profile for mapping between DTOs, entities, and events.
/// </summary>
public class MappingProfile : Profile
{
    /// Provides configuration for mapping objects using AutoMapper.
    public MappingProfile()
    {
        // ------------------------------
        // AudioFeaturesDto ↔ AudioFeatures
        // ------------------------------
        CreateMap<AudioFeaturesDto, AudioFeatures>().ReverseMap();

        // ------------------------------
        // TrackDto ↔ TrackMetadata
        // ------------------------------
        CreateMap<TrackDto, TrackMetadata>()
            .ForMember(d => d.ContainerName, opt => opt.Ignore())
            .ForMember(d => d.IsDeleted, opt => opt.Ignore())
            .ForMember(d => d.Status, opt => opt.Ignore())
            .ForMember(d => d.ETag, opt => opt.Ignore())
            .ForMember(d => d.Timestamp, opt => opt.Ignore())
            .ForMember(d => d.SpredUserId, opt => opt.MapFrom(s => s.OwnerId))
            .ForMember(d => d.Bucket, opt => opt.Ignore())
            .ForMember(d => d.Audio, opt => opt.MapFrom(s => s.Audio))
            .ForMember(d => d.TrackLinks, opt => opt.MapFrom(s => s.TrackUrl))
            .ReverseMap()
            .ForMember(d => d.Audio, opt => opt.MapFrom(s => s.Audio))
            .ForMember(d => d.TrackUrl, opt => opt.MapFrom(s => s.TrackLinks));

        // ------------------------------
        // TrackLink ↔ PlatformUrl
        // ------------------------------
        CreateMap<TrackLink, PlatformUrl>()
            .ForMember(d => d.Platform, opt => opt.MapFrom(s => s.Platform))
            .ForMember(d => d.Value, opt => opt.MapFrom(s => s.Value))
            .ReverseMap();

        // ------------------------------
        // Album ↔ AlbumDto
        // ------------------------------
        CreateMap<Album, AlbumDto>().ReverseMap();

        // ------------------------------
        // Artist ↔ ArtistDto
        // ------------------------------
        CreateMap<Artist, ArtistDto>().ReverseMap();

        // ------------------------------
        // TrackMetadata → PublicTrackDto
        // ------------------------------
        CreateMap<TrackMetadata, PublicTrackDto>()
            .ForMember(d => d.TrackUrl, opt => opt.MapFrom(s => s.TrackLinks));

        // ------------------------------
        // TrackMetadata → PrivateTrackDto
        // ------------------------------
        CreateMap<TrackMetadata, PrivateTrackDto>()
            .ForMember(d => d.Id, s => s.MapFrom(w => w.Id.ToString()))
            .ForMember(d => d.Duration, s => s.MapFrom(w => w.Audio.Duration))
            .ForMember(d => d.Bitrate, s => s.MapFrom(w => w.Audio.Bitrate))
            .ForMember(d => d.SampleRate, s => s.MapFrom(w => w.Audio.SampleRate))
            .ForMember(d => d.Channels, s => s.MapFrom(w => w.Audio.Channels))
            .ForMember(d => d.Codec, s => s.MapFrom(w => w.Audio.Codec))
            .ForMember(d => d.Bpm, s => s.MapFrom(w => w.Audio.Bpm))
            .ForMember(d => d.Genre, s => s.MapFrom(w => w.Audio.Genre))
            .ForMember(d => d.Energy, s => s.MapFrom(w => w.Audio.Energy))
            .ForMember(d => d.Valence, s => s.MapFrom(w => w.Audio.Valence))
            .ForMember(d => d.TrackUrl, s => s.MapFrom(w => w.TrackLinks))
            .ForMember(d => d.Artists, s => s.MapFrom(w => w.Artists))
            .ForMember(d => d.Album, s => s.MapFrom(w => w.Album));

        // ------------------------------
        // Private DTO Mappings
        // ------------------------------
        CreateMap<Artist, PrivateArtistDto>()
            .ConstructUsing(a => new PrivateArtistDto(a.Name, a.ImageUrl));

        CreateMap<Album, PrivateAlbumDto>()
            .ConstructUsing(a => new PrivateAlbumDto(
                a.AlbumName,
                a.AlbumLabel,
                a.AlbumReleaseDate,
                a.ImageUrl
            ));

        // ------------------------------
        // TrackCreate → TrackDto
        // ------------------------------
        CreateMap<TrackCreate, TrackDto>(MemberList.None)
            .ForMember(d => d.Title, s => s.MapFrom(w => w.Title))
            .ForMember(d => d.Description, s => s.MapFrom(w => w.Description))
            .ForMember(d => d.TrackUrl, s => s.Ignore())
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        CreateMap<CreateTrackMetadataItemCommand, UpdateTrackMetadataItemCommand>()
            .ForMember(d => d.UpdatedTrackLinks, opt => opt.MapFrom(s => s.TrackLinks))
            .ForMember(d => d.Artists, opt => opt.MapFrom(s => s.Artists))
            .ForMember(d => d.Album, opt => opt.MapFrom(s => s.Album));
    }
}
