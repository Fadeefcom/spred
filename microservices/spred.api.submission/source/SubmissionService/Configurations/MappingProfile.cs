using AutoMapper;
using SubmissionService.Models.Entities;
using SubmissionService.Models.Models;

namespace SubmissionService.Configurations;

/// <summary>
/// Defines AutoMapper mappings for submission-related entities and DTOs.
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MappingProfile"/> class
    /// and configures entity-to-DTO mappings.
    /// </summary>
    public MappingProfile()
    {
        CreateMap<Submission, SubmissionDto>();
        CreateMap<ArtistInbox, SubmissionDto>();
    }
}