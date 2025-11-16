using AutoMapper;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using System.Linq.Expressions;
using Exception.Exceptions;
using InferenceService.Abstractions;
using InferenceService.Components;
using InferenceService.Models.Dto;
using InferenceService.Models.Entities;
using Microsoft.Azure.Cosmos;

namespace InferenceService.Test;

public class InferenceManagerTests
{
    private readonly Mock<IPersistenceStore<InferenceResult, Guid>> _storeMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly IInferenceAccessService _inferenceAccessServiceMock;
    private readonly Mock<IMapper> _mapperMock = new();
    private readonly InferenceManager _manager;

    public InferenceManagerTests()
    {
        
        var loggerMock = new Mock<ILogger<InferenceManager>>();
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);
        _inferenceAccessServiceMock = new InferenceAccessService(_loggerFactoryMock.Object, _mapperMock.Object)
            { };
        _manager = new InferenceManager(_storeMock.Object, _loggerFactoryMock.Object, _mapperMock.Object, _inferenceAccessServiceMock);
    }

    [Fact]
    public async Task SaveInference_ShouldStoreEntity_WhenSuccess()
    {
        // Arrange
        var data = new List<InferenceMetadata>();
        var trackId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var version = "v1";

        _storeMock.Setup(x => x.StoreAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        // Act
        var result = await _manager.SaveInference(data, trackId, userId, version, It.IsAny<CancellationToken>());

        // Assert
        Assert.NotNull(result);
        _storeMock.Verify(x => x.StoreAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SaveInference_ShouldThrow_WhenStoreFails()
    {
        // Arrange
        _storeMock.Setup(x => x.StoreAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(false, false, new BaseException(500, "store failed")));

        // Act + Assert
        await Assert.ThrowsAsync<BaseException>(() =>
            _manager.SaveInference([], Guid.NewGuid(), Guid.NewGuid(), "v1", It.IsAny<CancellationToken>()));
    }

    [Fact]
    public async Task GetInference_ShouldReturnEmpty_WhenNotFound()
    {
        _storeMock.Setup(x => x.GetAsync(It.IsAny<Expression<Func<InferenceResult, bool>>>(),
                                         It.IsAny<Expression<Func<InferenceResult, long>>>(),
                                         It.IsAny<PartitionKey>(), 0, 1, false, CancellationToken.None, It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<IEnumerable<InferenceResult>>([], false, null));

        var (version, id, data) = await _manager.GetInference(Guid.NewGuid(), Guid.NewGuid(), true, "v1", default);

        Assert.Equal("v1", version);
        Assert.Equal(Guid.Empty, id);
        Assert.Empty(data!);
    }

    [Fact]
    public async Task GetInference_ShouldReturnMappedData_WhenFound()
    {
        var entity = new InferenceResult
        {
            ModelVersion = "v1",
            Metadata = [new InferenceMetadata
                {
                    Score = 1.0f,
                    Reaction = new ReactionStatus()
                }
            ]
        };

        _storeMock.Setup(x => x.GetAsync(It.IsAny<Expression<Func<InferenceResult, bool>>>(),
                                         It.IsAny<Expression<Func<InferenceResult, long>>>(),
                                         It.IsAny<PartitionKey>(), 0, 1, false, CancellationToken.None, It.IsAny<bool>()))
            
            .ReturnsAsync(new PersistenceResult<IEnumerable<InferenceResult>>([entity], false, null));

        _mapperMock.Setup(x => x.Map<List<InferenceMetadataDto>>(It.IsAny<IEnumerable<InferenceMetadata>>()))
            .Returns([new InferenceMetadataDto()]);

        var (version, id, data) = await _manager.GetInference(Guid.NewGuid(), Guid.NewGuid(), true, "v1", default);

        Assert.Equal("v1", version);
        Assert.Equal(entity.Id, id);
        Assert.Single(data);
    }

    [Fact]
    public async Task UpdateInference_ShouldUpdateEntity_WhenFound()
    {
        var trackId = Guid.NewGuid();
        var owner = Guid.NewGuid();
        var id = Guid.NewGuid();

        var inference = new InferenceResult
        {
            Metadata = [],
            UpdatedAt = DateTime.MinValue
        };

        _storeMock.Setup(x => x.GetAsync(It.IsAny<Expression<Func<InferenceResult, bool>>>(),
                                         It.IsAny<Expression<Func<InferenceResult, long>>>(),
                                         It.IsAny<PartitionKey>(), 0, 1, false, CancellationToken.None, It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<IEnumerable<InferenceResult>>([inference], false, null));

        _storeMock.Setup(x => x.UpdateAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new PersistenceResult<bool>(true, false, null)));

        var dict = new Dictionary<string, (string, float)>
        {
            [Guid.NewGuid().ToString()] = (owner.ToString(), 0.8f)
        };

        await _manager.UpdateInference(dict, trackId, owner, "v1", It.IsAny<CancellationToken>());
        _storeMock.Verify(x => x.UpdateAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddRateToPlaylist_ShouldUpdateReaction_WhenPlaylistExists()
    {
        var playlistId = Guid.NewGuid();
        var metadata = new InferenceMetadata
        {
            MetadataId = playlistId,
            Reaction = new ReactionStatus()
        };

        var inference = new InferenceResult
        {
            Metadata = [metadata]
        };

        _storeMock.Setup(x => x.GetAsync(It.IsAny<Expression<Func<InferenceResult, bool>>>(),
                                         It.IsAny<Expression<Func<InferenceResult, long>>>(),
                                         It.IsAny<PartitionKey>(), 0, 1, false, CancellationToken.None, It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<IEnumerable<InferenceResult>>([inference], false, null));

        _storeMock.Setup(x => x.UpdateAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new PersistenceResult<bool>(true, false, null)));

        await _manager.AddRateToPlaylist(playlistId, Guid.NewGuid(), Guid.NewGuid(), "v1",
            new ReactionStatus { IsLiked = true }, It.IsAny<CancellationToken>());

        Assert.True(metadata.Reaction.IsLiked);
        _storeMock.Verify(x => x.UpdateAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateInferenceById_ShouldUpdateSimilarTracks()
    {
        var playlistId = Guid.NewGuid();
        var trackId = Guid.NewGuid();
        var similarTracks = new List<SimilarTrack>
        {
            new() { SimilarTrackId = Guid.NewGuid(), TrackOwner = Guid.NewGuid(), Similarity = 0.9f }
        };

        var inference = new InferenceResult
        {
            Metadata = [new InferenceMetadata
                {
                    MetadataId = playlistId,
                    Reaction = new ReactionStatus()
                }
            ]
        };

        _storeMock.Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), CancellationToken.None, It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<InferenceResult>(inference, false, null));

        _storeMock.Setup(x => x.UpdateAsync(It.IsAny<InferenceResult>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new PersistenceResult<bool>(true, false, null)));

        var dict = new Dictionary<TrackMetadataPair, List<SimilarTrack>>
        {
            [new TrackMetadataPair { MetadataId = playlistId }] = similarTracks
        };

        await _manager.UpdateInference(Guid.NewGuid(), trackId, dict, It.IsAny<CancellationToken>());

        Assert.Equal(similarTracks, inference.Metadata[0].SimilarTracks);
    }
}