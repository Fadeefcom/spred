using System;
using Authorization.Configuration;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using Authorization.Options;
using AutoMapper;
using Xunit;

namespace Authorization.Test.Helpers;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_OAuthAuthenticationDto_To_Entity_And_Back()
    {
        var dto = new OAuthAuthenticationDto
        {
            OAuthProvider = "Google",
            PrimaryId = "abc123",
            SpredUserId = Guid.NewGuid(),
        };

        var entity = _mapper.Map<OAuthAuthentication>(dto);
        var mappedBack = _mapper.Map<OAuthAuthenticationDto>(entity);

        Assert.Equal(dto.PrimaryId, mappedBack.PrimaryId);
        Assert.Equal(dto.OAuthProvider, mappedBack.OAuthProvider);
        Assert.Equal(dto.SpredUserId, mappedBack.SpredUserId);
    }

    [Fact]
    public void Should_Map_BaseUser_To_UserDto()
    {
        var user = new BaseUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            UserName = "tester"
        };

        var dto = _mapper.Map<UserDto>(user);

        Assert.Equal(user.Id, dto.Id);
        Assert.Equal(user.Email, dto.Email);
        Assert.Equal(user.UserName, dto.Username);
    }
}