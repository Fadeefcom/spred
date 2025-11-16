using Authorization.Helpers;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using AutoMapper;

namespace Authorization.Configuration;

/// <summary>
/// Mapping profile
/// </summary>
public class MappingProfile : Profile
{
    /// <summary>
    /// .ctor
    /// </summary>
    public MappingProfile()
    {
        CreateMap<OAuthAuthenticationDto, OAuthAuthentication>()
            .ForMember(d => d.ETag, opt => opt.Ignore())
            .ForMember(d => d.Timestamp, opt => opt.Ignore())
            .ReverseMap();
        CreateMap<BaseUser, UserDto>()
            .ForMember(d => d.JustRegistered,
                opt => opt.MapFrom(s => UserExtension.JustRegistered(s)))
            .ForMember(d => d.Roles, opt => opt.MapFrom(s => s.UserRoles));
        CreateMap<LinkedAccountState, UserAccountDto>()
            .ForMember(d => d.Platform, opt => opt.MapFrom(s => AccountPlatformHelper.ReverseMap[s.Platform]))
            .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
            .ForMember(d => d.ConnectedAt, opt => opt.MapFrom(s => s.CreatedAt))
            .ForMember(d => d.ProfileUrl, opt => opt.Ignore());
    }
}