using Authorization.Options;
using Authorization.Options.AuthenticationSchemes;
using Authorization.Services;
using System;
using Xunit;

namespace Authorization.Test.Services;

public class TokenPurposeHelperTests
{
    [Theory]
    [InlineData(AuthType.Base, "AccessTokenBase")]
    [InlineData(AuthType.Spotify, "AccessTokenSpotify")]
    [InlineData(AuthType.Google, "AccessTokenGoogle")]
    [InlineData(AuthType.Yandex, "AccessTokenYandex")]
    [InlineData(AuthType.Microsoft, "AccessTokenMicrosoft")]
    public void GetPurposeName_ShouldReturnCorrectValue(AuthType authType, string expected)
    {
        var result = TokenPurposeHelper.GetPurposeName(authType);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Base", AuthType.Base)]
    [InlineData("Spotify", AuthType.Spotify)]
    [InlineData("spotify", AuthType.Spotify)] // case-insensitive
    [InlineData("Google", AuthType.Google)]
    [InlineData("Yandex", AuthType.Yandex)]
    [InlineData("Microsoft", AuthType.Microsoft)]
    public void GetAuthType_ShouldParseValidScheme(string input, AuthType expected)
    {
        var result = TokenPurposeHelper.GetAuthType(input);
        Assert.NotNull(result);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetAuthType_ShouldReturnNull_ForInvalidScheme()
    {
        var result = TokenPurposeHelper.GetAuthType("InvalidType");
        Assert.Null(result);
    }

    [Theory]
    [InlineData(AuthType.Base, "Base")]
    [InlineData(AuthType.Spotify, SpotifyAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(AuthType.Google, GoogleAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(AuthType.Yandex, YandexAuthenticationDefaults.AuthenticationScheme)]
    [InlineData(AuthType.Microsoft, MicrosoftManagementDefaults.AuthenticationScheme)]
    public void GetSchemeName_ShouldReturnCorrectScheme(AuthType authType, string expected)
    {
        var result = TokenPurposeHelper.GetSchemeName(authType);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetSchemeName_ShouldThrow_ForInvalidEnumValue()
    {
        var invalidAuthType = (AuthType)999;
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            TokenPurposeHelper.GetSchemeName(invalidAuthType));
    }
}
