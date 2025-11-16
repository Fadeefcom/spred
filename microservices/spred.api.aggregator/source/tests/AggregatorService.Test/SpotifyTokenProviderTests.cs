using System.Net;
using AggregatorService.Abstractions;
using AggregatorService.Components;
using AggregatorService.Configurations;
using AggregatorService.Models.Dto;
using Microsoft.Extensions.Options;
using Moq;
using Refit;

namespace AggregatorService.Test;

public class SpotifyTokenProviderTests
{
    private readonly Mock<ISpotifyAuthApi> _authMock = new();

    private static IOptions<SpotifyCredentialsList> CreateOptions(params (string clientId, string secret)[] creds)
    {
        var list = new SpotifyCredentialsList
        {
            Credentials = new List<SpotifyCredential>()
        };
        foreach (var (id, secret) in creds)
        {
            list.Credentials.Add(new SpotifyCredential
            {
                ClientId = id,
                ClientSecret = secret
            });
        }
        return Options.Create(list);
    }

    private static IApiResponse<TokenResponse> SuccessResponse(string token)
    {
        var content = new TokenResponse { AccessToken = token };
        return new ApiResponse<TokenResponse>(
            new HttpResponseMessage(HttpStatusCode.OK),
            content,
            new RefitSettings());
    }

    private static IApiResponse<TokenResponse> FailResponse(HttpStatusCode code)
    {
        return new ApiResponse<TokenResponse>(
            new HttpResponseMessage(code),
            null,
            new RefitSettings());
    }

    [Fact]
    public async Task AcquireAsync_ShouldReturnBearer_WhenFirstCredentialSucceeds()
    {
        var options = CreateOptions(("id1", "sec1"));
        _authMock.Setup(a => a.GetToken(It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(SuccessResponse("abc123"));

        var provider = new SpotifyTokenProvider(_authMock.Object, options);

        var (bearer, idx) = await provider.AcquireAsync();

        Assert.Equal("Bearer abc123", bearer);
        Assert.Equal(0, idx);
    }

    [Fact]
    public async Task RotateAsync_ShouldSkipFailed_AndReturnFromNextCredential()
    {
        var options = CreateOptions(("id1", "sec1"), ("id2", "sec2"));
        _authMock.SetupSequence(a => a.GetToken(It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(FailResponse(HttpStatusCode.BadRequest))
            .ReturnsAsync(SuccessResponse("zzz999"));

        var provider = new SpotifyTokenProvider(_authMock.Object, options);

        var (bearer, idx) = await provider.RotateAsync(0);

        Assert.Equal("Bearer zzz999", bearer);
        Assert.Equal(0, idx);
    }

    [Fact]
    public async Task AcquireAsync_ShouldThrow_WhenAllCredentialsFail()
    {
        var options = CreateOptions(("id1", "sec1"), ("id2", "sec2"));
        _authMock.Setup(a => a.GetToken(It.IsAny<Dictionary<string, string>>()))
            .ReturnsAsync(FailResponse(HttpStatusCode.Unauthorized));

        var provider = new SpotifyTokenProvider(_authMock.Object, options);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.AcquireAsync());
    }
}