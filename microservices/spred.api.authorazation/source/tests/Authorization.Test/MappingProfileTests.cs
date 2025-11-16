using System;
using Authorization.Configuration;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using AutoMapper;

namespace Authorization.Test;

public class MappingProfileTests
{
    private readonly IMapper _mapper;

    public MappingProfileTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        config.AssertConfigurationIsValid(); // Проверка конфигурации
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void Should_Map_OAuthAuthenticationDto_To_OAuthAuthentication_And_Back()
    {
        // Arrange
        var dto = new OAuthAuthenticationDto
        {
            SpredUserId = Guid.NewGuid(),
            PrimaryId = "new-test-id",
            Id = Guid.NewGuid(),
            DateAdded = DateTime.UtcNow,
            AccessToken = "token123",
            OAuthProvider = "test-provider",
        };

        // Act
        var entity = _mapper.Map<OAuthAuthentication>(dto);
        var mappedBack = _mapper.Map<OAuthAuthenticationDto>(entity);

        // Assert - field-by-field
        Assert.Equal(dto.SpredUserId, mappedBack.SpredUserId);
        Assert.Equal(dto.PrimaryId, mappedBack.PrimaryId);
        Assert.Equal(dto.DateAdded.ToUniversalTime(), mappedBack.DateAdded.ToUniversalTime(), TimeSpan.FromSeconds(1)); // tolerate precision
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