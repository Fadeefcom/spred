using System.Linq.Expressions;
using Exception.Exceptions;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using TrackService.Components.Services;
using TrackService.Models.Entities;

public class TrackManagerTests
{
    private readonly Mock<IPersistenceStore<TrackMetadata, Guid>> _storeMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly Mock<ILogger<TrackManager>> _loggerMock;
    private readonly TrackManager _manager;

    public TrackManagerTests()
    {
        _storeMock = new Mock<IPersistenceStore<TrackMetadata, Guid>>();
        _loggerMock = new Mock<ILogger<TrackManager>>();
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _manager = new TrackManager(_storeMock.Object, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task AddAsync_Success_ReturnsId()
    {
        var track = new TrackMetadata( );
        var success = new PersistenceResult<bool>(true, false, null);

        _storeMock.Setup(s => s.StoreAsync(track, default))
            .ReturnsAsync(success);

        var result = await _manager.AddAsync(track, Guid.NewGuid());

        Assert.Equal(track.Id, result);
    }

    [Fact]
    public async Task AddAsync_Failure_ThrowsBaseException()
    {
        var track = new TrackMetadata( );
        var exception = new BaseException("fail", 409, "title", "type", "details");
        var failure = new PersistenceResult<bool>(false, false, exception);

        _storeMock.Setup(s => s.StoreAsync(track, default))
            .ReturnsAsync(failure);

        await Assert.ThrowsAsync<BaseException>(() => _manager.AddAsync(track, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteAsync_SoftDelete_Success()
    {
        var track = new TrackMetadata( );
        var getResult = new PersistenceResult<TrackMetadata>(track, false, null);
        var updateResult = new PersistenceResult<bool>(true, false, null);

        _storeMock.Setup(s => s.GetAsync(track.Id, It.IsAny<PartitionKey>(), default, false))
            .ReturnsAsync(getResult);
        _storeMock.Setup(s => s.UpdateAsync(track, default))
            .ReturnsAsync(updateResult);

        var result = await _manager.DeleteAsync(track.Id, track.SpredUserId, default, "bucket");

        Assert.True(result);
        Assert.True(track.IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ReturnsTrue()
    {
        var track = new TrackMetadata( );
        var getResult = new PersistenceResult<TrackMetadata>(track, false, null);

        _storeMock.Setup(s => s.GetAsync(track.Id, It.IsAny<PartitionKey>(), default, false))
            .ReturnsAsync(getResult);
        _storeMock.Setup(s => s.UpdateAsync(It.IsAny<TrackMetadata>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        var result = await _manager.DeleteAsync(track.Id, track.SpredUserId, default, "bucket");

        Assert.True(result);
    }

    [Fact]
    public async Task GetAsync_Success_ReturnsItems()
    {
        var track = new TrackMetadata( );
        var success = new PersistenceResult<IEnumerable<TrackMetadata>>(new[] { track }, false, null);

        _storeMock.Setup(s => s.GetAsync(
            It.IsAny<Expression<Func<TrackMetadata, bool>>>(),
            It.IsAny<Expression<Func<TrackMetadata, long>>>(),
            It.IsAny<PartitionKey>(),
            0, 10, true, default, false))
            .ReturnsAsync(success);

        var result = await _manager.GetAsync(new(), Guid.NewGuid(), default, "bucket");

        Assert.Single(result);
    }

    [Fact]
    public async Task GetByIdAsync_Failure_ReturnsNull()
    {
        var exception = new BaseException("not found", 404, "title", "type", "details");
        var failure = new PersistenceResult<TrackMetadata>(null, false, exception);

        _storeMock.Setup(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), default, true))
            .ReturnsAsync(failure);

        var result = await _manager.GetByIdAsync(Guid.NewGuid(), Guid.NewGuid(), default, "bucket");

        Assert.Null(result);
    }

    [Fact]
    public async Task GetTotalAsync_Success_ReturnsCount()
    {
        var success = new PersistenceResult<int>(5, false, null);

        _storeMock.Setup(s => s.CountAsync(It.IsAny<Expression<Func<TrackMetadata, bool>>>(),
            It.IsAny<PartitionKey>(), default, false))
            .ReturnsAsync(success);

        var result = await _manager.GetTotalAsync(new(), Guid.NewGuid(), default, "bucket");

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task UpdateAsync_Failure_ThrowsException()
    {
        var track = new TrackMetadata();
        var exception = new BaseException("update fail", 500, "title", "type", "details");
        var failure = new PersistenceResult<bool>(false, false, exception);

        _storeMock.Setup(s => s.UpdateAsync(track, default))
            .ReturnsAsync(failure);

        await Assert.ThrowsAsync<BaseException>(() => _manager.UpdateAsync(track, default));
    }

    [Fact]
    public async Task IfExistsByPrimaryId_Success_ReturnsId()
    {
        var track = new TrackMetadata( );
        var success = new PersistenceResult<IEnumerable<TrackMetadata>>(new[] { track }, false, null);

        _storeMock.Setup(s => s.GetAsync(
            It.IsAny<Expression<Func<TrackMetadata, bool>>>(),
            It.IsAny<Expression<Func<TrackMetadata, long>>>(),
            It.IsAny<PartitionKey>(),
            0, 1, false, default, false))
            .ReturnsAsync(success);

        var result = await _manager.IfExistsByPrimaryId("abc", Guid.NewGuid(), default);

        Assert.Equal(track.Id, result);
    }
}
