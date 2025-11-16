using AutoMapper;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using Spred.Bus.Contracts;

namespace InferenceService.Configuration;

/// <summary>
/// AutoMapper profile for mapping between DTOs, entities, and events.
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingProfile"/> class.
    /// Configures the mappings between different models.
    /// </summary>
    public MappingProfile()
    {
        CreateMap<InferenceMetadata, InferenceMetadataDto>()
            .ForMember(dest => dest.Score,
                opt => opt.MapFrom(src =>
                    src.Score < 0.33f ? "Poor fit" :
                    src.Score < 0.66f ? "Moderate fit" :
                    "Strong fit"))
            .ForMember(dest => dest.SimilarTracks,
                opt => 
                    opt.MapFrom(src => src.SimilarTracks.Select(s => new TrackUserPair
                {
                    TrackId = s.SimilarTrackId,
                    TrackOwner = s.TrackOwner
                }).ToList()))
            .ForMember(d => d.Type, opt => 
                opt.MapFrom(src => string.IsNullOrWhiteSpace(src.Type) ? "playlist" : src.Type));
    }
}