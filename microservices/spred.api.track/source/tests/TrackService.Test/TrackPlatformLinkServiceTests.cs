using System.Linq.Expressions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using Spred.Bus.Contracts;
using TrackService.Components.Services;
using TrackService.Models.Entities;
using TrackService.Test.Fixtures;

public class TrackPlatformLinkServiceTests
{
    private readonly Mock<IPersistenceStore<TrackPlatformId, Guid>> _storeMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<TrackPlatformLinkService>> _loggerMock;
    private readonly TrackPlatformLinkService _service;

    public TrackPlatformLinkServiceTests()
    {
        _storeMock = new Mock<IPersistenceStore<TrackPlatformId, Guid>>();
        _loggerMock = new Mock<ILogger<TrackPlatformLinkService>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_loggerMock.Object);

        TrackServiceApiFactory.SetupPersistenceStoreMock<TrackPlatformId, Guid, long>(_storeMock, () => new TrackPlatformId
        {
            SpredUserId = Guid.Empty,
            TrackMetadataId = Guid.NewGuid(),
            Platform = Platform.Spotify,
            PlatformTrackId = "test123"
        });

        _service = new TrackPlatformLinkService(_storeMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public void TryMap_Should_Return_True_For_Known_Platforms()
    {
        Assert.True(TrackPlatformLinkService.TryMap("spotify", out var platform));
        Assert.Equal(Platform.Spotify, platform);
    }

    [Fact]
    public void TryMap_Should_Return_False_For_Unknown_Platforms()
    {
        Assert.False(TrackPlatformLinkService.TryMap("nonexistent", out _));
        Assert.False(TrackPlatformLinkService.TryMap("", out _));
        Assert.False(TrackPlatformLinkService.TryMap(null!, out _));
    }

    [Fact]
    public async Task GetLinkAsync_Should_Return_Matching_TrackId()
    {
        var seedTrackId = Guid.NewGuid();
        TrackServiceApiFactory.SetupPersistenceStoreMock<TrackPlatformId, Guid, long>(_storeMock, () => new TrackPlatformId
        {
            SpredUserId = Guid.NewGuid(),
            TrackMetadataId = seedTrackId,
            Platform = Platform.Spotify,
            PlatformTrackId = "spotify123"
        });

        var pairs = new List<PlatformIdPair> { new("spotify", "spotify123") };
        var result = await _service.GetLinkAsync(pairs, CancellationToken.None);

        Assert.Equal(seedTrackId, result);
    }

    [Fact]
    public async Task GetLinkAsync_Should_Return_Null_For_Unknown_Platform()
    {
        var pairs = new List<PlatformIdPair> { new("unknown", "xyz") };
        var result = await _service.GetLinkAsync(pairs, CancellationToken.None);
        Assert.Null(result);
    }

    [Fact]
    public async Task AddLinksAsync_Should_Store_Entities_For_Valid_Platforms()
    {
        var trackId = Guid.NewGuid();
        var pairs = new List<PlatformIdPair> { new("spotify", "x123"), new("youtube", "y456") };

        await _service.AddLinksAsync(pairs, Guid.NewGuid(), trackId, CancellationToken.None);

        _storeMock.Verify(s => s.StoreAsync(
            It.Is<TrackPlatformId>(x => x.Platform == Platform.Spotify && x.TrackMetadataId == trackId && x.PlatformTrackId == "x123"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        _storeMock.Verify(s => s.StoreAsync(
            It.Is<TrackPlatformId>(x => x.Platform == Platform.YouTube && x.TrackMetadataId == trackId && x.PlatformTrackId == "y456"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddLinksAsync_Should_Skip_Unknown_Platforms()
    {
        var pairs = new List<PlatformIdPair> { new("randommusic", "id1") };
        await _service.AddLinksAsync(pairs, Guid.NewGuid(), Guid.Empty, CancellationToken.None);
        _storeMock.Verify(s => s.StoreAsync(It.IsAny<TrackPlatformId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SetupPersistenceStoreMock<T, TKey, TSort>(
        Mock<IPersistenceStore<T, TKey>> mock,
        Func<T> seedData
    ) where T : class, IBaseEntity<TKey>
    {
        mock.Setup(x => x.StoreAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.GetAsync<TSort>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, TSort>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter,
                Expression<Func<T, TSort>> _,
                PartitionKey _,
                int _,
                int _,
                bool _,
                CancellationToken _,
                bool _) =>
            {
                var obj = seedData();
                var mockData = new List<T> { obj };
                var compiled = filter?.Compile();
                var result = compiled != null ? mockData.Where(compiled).ToList() : mockData;
                return new PersistenceResult<IEnumerable<T>>(result, false, null);
            });
    }
}
