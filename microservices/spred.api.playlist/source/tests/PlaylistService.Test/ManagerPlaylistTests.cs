using System.Linq.Expressions;
using Exception.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using PlaylistService.Components.Services;
using PlaylistService.Models;
using PlaylistService.Models.Entities;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;

namespace PlaylistService.Test;

public class ManagerPlaylistTests
{
    private readonly Mock<IPersistenceStore<CatalogMetadata, Guid>> _playlistStoreMock = new();
    private readonly Mock<IPersistenceStore<CatalogStatistics, Guid>> _statisticsStoreMock = new();
    private readonly ManagerPlaylist _manager;

    public ManagerPlaylistTests()
    {
        _manager = new ManagerPlaylist(
            _playlistStoreMock.Object,
            _statisticsStoreMock.Object,
            new LoggerFactory(new[] { new Microsoft.Extensions.Logging.Debug.DebugLoggerProvider() }));
    }

    [Fact]
    public async Task AddAsync_ShouldReturnTrue_WhenSuccess()
    {
        _playlistStoreMock
            .Setup(x => x.StoreAsync(It.IsAny<CatalogMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        var result = await _manager.AddAsync(new CatalogMetadata(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task AddAsync_ShouldReturnFalse_WhenFailed()
    {
        _playlistStoreMock
            .Setup(x => x.StoreAsync(It.IsAny<CatalogMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(false, false, new BaseException("fail")));

        var result = await _manager.AddAsync(new CatalogMetadata(), CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenEntityDoesNotExist()
    {
        _playlistStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<CatalogMetadata, bool>>>(),
                                   It.IsAny<Expression<Func<CatalogMetadata, long>>>(),
                                   It.IsAny<PartitionKey>(), 0, 1, false, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new PersistenceResult<IEnumerable<CatalogMetadata>>(null, false, null));

        var result = await _manager.DeleteAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task DeleteAsync_ShouldReturnTrue_WhenDeletedSuccessfully()
    {
        var entity = new CatalogMetadata();

        _playlistStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<CatalogMetadata, bool>>>(),
                                   It.IsAny<Expression<Func<CatalogMetadata, long>>>(),
                                   It.IsAny<PartitionKey>(), 0, 1, false, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new PersistenceResult<IEnumerable<CatalogMetadata>>(new[] { entity }, false, null));

        _playlistStoreMock
            .Setup(x => x.DeleteAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        var result = await _manager.DeleteAsync(entity.Id, Guid.NewGuid(), CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task UpdateAsync_ShouldReturnTrue_WhenSuccess()
    {
        var entity = new CatalogMetadata();

        _playlistStoreMock
            .Setup(x => x.UpdateAsync(entity, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        var result = await _manager.UpdateAsync(entity, CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task FindByIdAsync_ShouldReturnEntity_WhenSuccess()
    {
        var entity = new CatalogMetadata();

        _playlistStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), 
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<CatalogMetadata>(entity, false, null));

        var result = await _manager.FindByIdAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExistsByPrimaryIdAsync_ShouldSearchDefaultBucket_WhenUserIsNotEmpty()
    {
        var entity = new CatalogMetadata();

        _playlistStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<CatalogMetadata, bool>>>(),
                                   It.IsAny<Expression<Func<CatalogMetadata, long>>>(),
                                   It.IsAny<PartitionKey>(), 0, 1, false, It.IsAny<CancellationToken>(), false))
            .ReturnsAsync(new PersistenceResult<IEnumerable<CatalogMetadata>>(new[] { entity }, false, null));

        var result = await _manager.ExistsByPrimaryIdAsync(new PrimaryId( "test", "test", "track-id"), Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(entity.Id, result);
    }

    [Fact]
    public async Task GetStatisticDifference_ShouldReturnDifference_WhenSuccess()
    {
        var list = new List<CatalogStatistics>
        {
            new() { Date = DateTime.UtcNow.AddDays(-30), Followers = 50 },
            new() { Date = DateTime.UtcNow.Date, Followers = 20 }
        };

        _statisticsStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Expression<Func<CatalogStatistics, bool>>>(),
                                   It.IsAny<Expression<Func<CatalogStatistics, long>>>(),
                                   It.IsAny<PartitionKey>(), 0, 31, false, It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<IEnumerable<CatalogStatistics>>(list, false, null));

        var result = await _manager.GetStatisticDifference(Guid.NewGuid(), CancellationToken.None);

        Assert.Equal(30, result);
    }

    [Fact]
    public async Task GetPlaylistsByIdsAsync_ShouldReturnList()
    {
        var ownerMetadataIds = new Dictionary<Guid, List<Guid>>
        {
            [Guid.NewGuid()] = new() { Guid.NewGuid(), Guid.NewGuid() }
        };

        var resultList = new List<CatalogMetadata> { new(), new() };

        _playlistStoreMock
            .Setup(x => x.ReadManyItemsAsync(It.IsAny<Expression<Func<CatalogMetadata, bool>>>(),
                                             It.IsAny<Expression<Func<CatalogMetadata, long>>>(),
                                             It.IsAny<List<(string, PartitionKey)>>(),
                                             true,
                                             It.IsAny<CancellationToken>()))
            .ReturnsAsync(resultList);

        var result = await _manager.GetPlaylistsByIdsAsync(ownerMetadataIds, "playlist", CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}