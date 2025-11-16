using System.Text.Json;
using AggregatorService.Abstractions;
using AggregatorService.Components.Consumers;
using AggregatorService.Configurations;
using AggregatorService.Models;
using AggregatorService.Models.Dto;
using AutoMapper;
using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;

namespace AggregatorService.Test;

public class CatalogEnrichmentRequestConsumerTests
{
    private readonly Mock<ICatalogProvider> _providerMock = new();
    private readonly Mock<IPublishEndpoint> _publishMock = new();
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(_ => { });

    private CatalogEnrichmentRequestConsumer CreateConsumer() =>
        new(_providerMock.Object, _loggerFactory, _publishMock.Object);

    [Fact]
    public async Task Consume_Should_Handle_Playlist_Success()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "spotify:playlist:abc123",
            Platform = "Spotify",
            Type = "playlistMetadata",
            SpredUserId = Guid.NewGuid()
        };

        _providerMock.Setup(x => x.ResolvePlaylistIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("resolved123");
        _providerMock.Setup(x => x.GetPlaylistMetadataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new MetadataDto { Name = "Playlist1", PrimaryId = "test:test:test"});
        _providerMock.Setup(x => x.GetPlaylistStatsAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
            .ReturnsAsync(new HashSet<StatInfo> { new() { Value = 10 } });
        _providerMock.Setup(x => x.GetPlaylistTracksSnapshotAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(new List<TrackDtoWithPlatformIds> { new()
                {
                    Id = Guid.NewGuid(),
                    Title = null
                }
            });

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_Should_Skip_If_PlaylistId_NotResolved()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "spotify:playlist:missing",
            Platform = "Spotify",
            Type = "playlistMetadata",
            SpredUserId = Guid.Empty
        };

        _providerMock.Setup(x => x.ResolvePlaylistIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Should_Skip_If_Metadata_NotFound()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "spotify:playlist:abc",
            Platform = "Spotify",
            Type = "playlistMetadata",
            SpredUserId = Guid.Empty
        };

        _providerMock.Setup(x => x.ResolvePlaylistIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("resolved");
        _providerMock.Setup(x => x.GetPlaylistMetadataAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((MetadataDto?)null);

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Should_Handle_Radio_Success()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "slug:radio:test-radio",
            Platform = "radio",
            Type = "radioMetadata",
            SoundChartsApi = "edge-radio",
            SpredUserId = Guid.Empty
        };

        var radioInfo = new RadioInfo
        {
            Name = "Edge Radio",
            CountryName = "UK",
            CountryCode = "GB",
            CityName = "London",
            Reach = 5000,
            TimeZone = "Europe/London",
            ImageUrl = "url"
        };

        _providerMock.Setup(x => x.GetRadioMetadataAsync("edge-radio"))
            .ReturnsAsync(radioInfo);
        _providerMock.Setup(x => x.GetRadioTracksSnapshotAsync("edge-radio", 100))
            .ReturnsAsync((new List<TrackDtoWithPlatformIds> { new()
                {
                    Id = Guid.NewGuid(),
                    Title = null
                }
            }, 1));
        _providerMock.Setup(x => x.GetRadioPlatforms("edge-radio"))
            .ReturnsAsync(new List<(string, string, string)>
            {
                ("Platform", "id", "url")
            });

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_Should_Skip_When_RadioId_Empty()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "slug:radio:empty",
            Platform = "radio",
            Type = "radioMetadata",
            SoundChartsApi = "",
            SpredUserId = Guid.Empty
        };

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Should_Skip_When_RadioMetadata_NotFound()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "slug:radio:notfound",
            Platform = "radio",
            Type = "radioMetadata",
            SoundChartsApi = "edge-radio",
            SpredUserId = Guid.Empty
        };

        _providerMock.Setup(x => x.GetRadioMetadataAsync("edge-radio"))
            .ReturnsAsync((RadioInfo?)null);

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await consumer.Consume(context);

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Consume_Should_LogError_When_Exception_Thrown()
    {
        var request = new CatalogEnrichmentRequest
        {
            Id = Guid.NewGuid(),
            PrimaryId = "spotify:playlist:err",
            Platform = "Spotify",
            Type = "playlistMetadata",
            SpredUserId = Guid.Empty
        };

        _providerMock.Setup(x => x.ResolvePlaylistIdAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new InvalidOperationException("Simulated error"));

        var context = Mock.Of<ConsumeContext<CatalogEnrichmentRequest>>(c => c.Message == request);
        var consumer = CreateConsumer();

        await Assert.ThrowsAsync<InvalidOperationException>(() => consumer.Consume(context));

        _publishMock.Verify(x => x.Publish(It.IsAny<CatalogEnrichmentUpdateOrCreate>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
