using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Dto;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Services;
using Authorization.Test.Mocks;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Moq;
using Refit;

namespace Authorization.Test.Services;

public class BaseManagerServicesTests
{
    private readonly Mock<IUserPlusStore> _userStoreMock = new();
    private readonly Mock<IGetToken> _getTokenMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    private BaseManagerServices CreateService()
    {
        return MockBaseManagerServices.CreateMock(_userStoreMock, _getTokenMock, _configurationMock);
    }

    [Fact]
    public async Task FindByPrimaryId_ShouldReturnUser_IfTypeIsNotBase()
    {
        var user = new BaseUser();
        _userStoreMock
            .Setup(x => x.FindUserByPrimaryIdAsync("external123", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.FindByPrimaryId("external123", AuthType.Spotify);

        Assert.Equal(user, result);
    }

    [Fact]
    public async Task FindByPrimaryId_ShouldReturnNull_IfTypeIsBase()
    {
        var service = CreateService();
        var result = await service.FindByPrimaryId("id", AuthType.Base);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateAsyncByExternalIdAsync_IfUserExists()
    {
        _userStoreMock
            .Setup(x => x.CreateAsyncByExternalIdAsync(It.IsAny<BaseUser>(), "pid", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        var service = CreateService();
        var result = await service.CreateAsyncByExternalIdAsync(new BaseUser(), "pid", AuthType.Spotify);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AddOauthExternalIdAsync_ShouldFail_IfUserExists()
    {
        _userStoreMock
            .Setup(x => x.FindUserByPrimaryIdAsync("pid", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser());

        var service = CreateService();
        var result = await service.AddOauthExternalIdAsync(new BaseUser(), "pid", AuthType.Spotify);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Errors, e => e.Code == nameof(service.AddOauthExternalIdAsync));
    }

    [Fact]
    public async Task UpdateAccessToken_ShouldReturnEmpty_IfInvalid()
    {
        var service = CreateService();
        var result = await service.UpdateAccessToken("", "", AuthType.Spotify);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void GetUserId_ShouldReturnClaimId()
    {
        var userId = Guid.NewGuid().ToString();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        }));

        var service = CreateService();
        var result = service.GetUserId(principal);

        Assert.Equal(userId, result);
    }

    [Fact]
    public async Task AddFeedback_ShouldCallStore()
    {
        var feedback = new Feedback();
        var service = CreateService();

        _userStoreMock.Setup(x => x.AddFeedback(feedback, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await service.AddFeedback(feedback, CancellationToken.None);
        _userStoreMock.Verify();
    }
    
    [Fact]
    public async Task CreateAsyncByExternalIdAsync_ShouldQueuePlaylists_IfSucceededAndSpotify()
    {
        var user = new BaseUser { Id = Guid.NewGuid() };

        _userStoreMock.Setup(x => x.FindUserByPrimaryIdAsync("spotifyId", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaseUser)null);

        _userStoreMock.Setup(x => x.CreateAsyncByExternalIdAsync(user, "spotifyId", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        _getTokenMock.Setup(x => x.GetInternalTokenAsync(It.IsAny<Claim[]>()))
            .ReturnsAsync("mock_token");

        var aggregatorApiMock = new Mock<IAggregatorApi>();
        var response = new ApiResponse<object>(
            new HttpResponseMessage(HttpStatusCode.OK),
            new object(),
            new RefitSettings()
        );
        aggregatorApiMock.Setup(x => x.QueueUserPlaylists("Bearer mock_token", It.IsAny<QueueUserPlaylistsRequest>()))
            .Returns(Task.FromResult(response));

        var service = CreateService();
        service.GetType().GetField("_aggregatorApi", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, aggregatorApiMock.Object);

        var result = await service.CreateAsyncByExternalIdAsync(user, "spotifyId", AuthType.Spotify);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task AddOauthExternalIdAsync_ShouldQueuePlaylists_IfSucceededAndSpotify()
    {
        var user = new BaseUser { Id = Guid.NewGuid() };

        _userStoreMock.Setup(x => x.FindUserByPrimaryIdAsync("spotifyId", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BaseUser)null);

        _userStoreMock.Setup(x => x.AddOauthExternalIdAsync(user, "spotifyId", AuthType.Spotify, It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        _getTokenMock.Setup(x => x.GetInternalTokenAsync(It.IsAny<Claim[]>()))
            .ReturnsAsync("mock_token");

        var aggregatorApiMock = new Mock<IAggregatorApi>();
        var response = new ApiResponse<object>(
            new HttpResponseMessage(HttpStatusCode.OK),
            new object(),
            new RefitSettings()
        );

        aggregatorApiMock.Setup(x => x.QueueUserPlaylists("Bearer mock_token", It.IsAny<QueueUserPlaylistsRequest>()))
            .Returns(Task.FromResult(response));

        var service = CreateService();
        service.GetType().GetField("_aggregatorApi", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, aggregatorApiMock.Object);

        var result = await service.AddOauthExternalIdAsync(user, "spotifyId", AuthType.Spotify);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task UpdateAccessToken_ShouldReturnAccessToken_IfSpotifySuccess()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
    {
        {"OAuthOption:Spotify:ClientId", "cid"},
        {"OAuthOption:Spotify:ClientSecret", "csecret"}
    }).Build();

        var tokenResponse = new SpotifyTokenResponse { AccessToken = "access_token", ExpiresIn = 111, TokenType = "access_token" };

        var spotifyApiMock = new Mock<ISpotifyApi>();
        spotifyApiMock.Setup(x => x.GetAccessTokenAsync(It.IsAny<string>(), It.IsAny<TokenRequest>()))
            .Returns(Task.FromResult(new ApiResponse<SpotifyTokenResponse>(
                new HttpResponseMessage(HttpStatusCode.OK),
                tokenResponse,
                new RefitSettings())));

        var service = CreateService();
        service.GetType().GetField("_configuration", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, config);
        service.GetType().GetField("_spotifyApi", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(service, spotifyApiMock.Object);

        var result = await service.UpdateAccessToken("refresh", "primary", AuthType.Spotify);

        Assert.Equal("access_token", result);
    }

    [Fact]
    public async Task AddAnonymousNotifyMe_ShouldCallStore()
    {
        var notify = new NotifyMe { Email = "test@example.com" };
        var service = CreateService();

        _userStoreMock.Setup(x => x.AddNotifyMe(notify, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        await service.AddAnonymousNotifyMe(notify, CancellationToken.None);
        _userStoreMock.Verify();
    }

    [Fact]
    public async Task GetUserOAuthAuthentication_ShouldCallStore()
    {
        var userId = Guid.NewGuid();
        var service = CreateService();

        _userStoreMock.Setup(x => x.GetUserOAuthAuthentication(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OAuthAuthentication>())
            .Verifiable();

        var _ = await service.GetUserOAuthAuthentication(userId, CancellationToken.None);
        _userStoreMock.Verify();
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnUser_IfPrincipalValid()
    {
        var userId = Guid.NewGuid().ToString();
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
        new Claim(ClaimTypes.NameIdentifier, userId)
    }));

        var user = new BaseUser();
        _userStoreMock.Setup(x => x.FindByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var service = CreateService();
        var result = await service.GetUserAsync(principal);

        Assert.Equal(user, result);
    }

    [Fact]
    public async Task GetUserAsync_ShouldReturnNull_IfNoClaim()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity());

        var service = CreateService();
        var result = await service.GetUserAsync(principal);

        Assert.Null(result);
    }

}
