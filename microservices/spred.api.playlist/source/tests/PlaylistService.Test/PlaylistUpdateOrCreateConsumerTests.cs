using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text.Json;
using AutoMapper;
using Extensions.Models;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using PlaylistService.Test.Fixtures;
using PlaylistService.Abstractions;
using PlaylistService.Components.Consumers;
using PlaylistService.Configuration;
using PlaylistService.Models;
using PlaylistService.Models.Commands;
using PlaylistService.Models.Entities;
using Refit;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using Spred.Bus.Contracts;
using Spred.Bus.DTOs;
using Headers = MassTransit.Headers;

namespace PlaylistService.Test;


public class PlaylistUpdateOrCreateConsumerTests
{
    [Fact]
    public async Task Should_Consume_PlaylistUpdate()
    {
        var services = new ServiceCollection();

        services.AddMassTransitTestHarness(cfg =>
        {
            cfg.AddConsumer<PlaylistUpdateOrCreateConsumer>();
        });

        await using var provider = services.BuildServiceProvider(true);
        var harness = provider.GetRequiredService<ITestHarness>();

        await harness.Start();
        try
        {
            var bus = provider.GetRequiredService<IBus>();
            
            var playlistUpdate = new CatalogEnrichmentUpdateOrCreate()
            {
                Snapshot = new()
                {
                    Id = Guid.NewGuid(),
                    SpredUserId = Guid.Empty,
                    PrimaryId = ""
                }, 
                Stats = [],
                Tracks = [new ()
                {
                    Id= Guid.NewGuid(),
                    Title = string.Empty
                }]
            };

            await bus.Publish(playlistUpdate);

            Assert.True(await harness.Consumed.Any<CatalogEnrichmentUpdateOrCreate>());
        }
        finally
        {
            await harness.Stop();
        }
    }
    
    [Fact]
    public async Task Consume_Should_Map_And_Publish_Command()
    {
        // Arrange
        var playlistUpdate = new CatalogEnrichmentUpdateOrCreate()
        {
            Snapshot = new()
            {
                Id = Guid.NewGuid(),
                SpredUserId = Guid.Empty,
                 PrimaryId = "test:test:empty"
            }, 
            Stats = [],
            Tracks = [new ()
            {
                Id= Guid.NewGuid(),
                Title = string.Empty
            }]
        };

        var mappedCommand = new UpdateMetadataCommand()
        {
            Id = Guid.NewGuid(),
            PrimaryId = PrimaryId.Parse("test:test:empty"),
            SpredUserId = Guid.Empty
        };
        
        var mediatorMock = new Mock<IMediator>();
        var contextMock = new Mock<ConsumeContext<CatalogEnrichmentUpdateOrCreate>>();
        var statisticsStoreMock = new Mock<IPersistenceStore<CatalogStatistics, Guid>>();
        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<PlaylistUpdateOrCreateConsumer>>();

        var factory = new PlaylistApiFactory();
        factory.SetupPersistenceStoreMock<CatalogStatistics, Guid, long>(statisticsStoreMock, () => new CatalogStatistics()
        {
            Date = DateTime.Now,
            Followers = 2,
            FollowersDailyDiff = 2,
            MetadataId = Guid.Empty
        });
        
        loggerFactoryMock
            .Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(loggerMock.Object);

        var servicesOptionsMock = new Mock<IOptions<ServicesOuterOptions>>();
        servicesOptionsMock
            .Setup(o => o.Value)
            .Returns(new ServicesOuterOptions
            {
                AggregatorService = "http://localhost",
                TrackService = "http://localhost",
                AuthorizationService = "http://localhost",
                InferenceService = "http://localhost",
                PlaylistService = "http://localhost",
                UiEndpoint = "http://localhost", 
                VectorService = "http://localhost",
                SubscriptionService = "http://localhost"
            });

        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<MappingProfile>();
        });

        var mapper = config.CreateMapper();

        mediatorMock
            .Setup(m => m.Publish(mappedCommand, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        contextMock.SetupGet(c => c.Message).Returns(playlistUpdate);
        var headersMock = new Mock<Headers>();
        object value = "Some DLQ reason";
        headersMock
            .Setup(h => h.TryGetHeader("x-dlq-reason", out value))
            .Returns(true);
        contextMock.SetupGet(c => c.Headers).Returns(headersMock.Object);
        
        var trackServiceMock = new Mock<ITrackServiceApi>();
        trackServiceMock.Setup(x => x.AddTrack(It.IsAny<string>(), It.IsAny<TrackDtoWithPlatformIds>()))
            .ReturnsAsync(new ApiResponse<JsonElement>(
                new HttpResponseMessage(HttpStatusCode.OK),
                JsonDocument.Parse("{\"id\": \"" + Guid.NewGuid() + "\"}").RootElement,
                new RefitSettings()
            ));

        var consumer = new PlaylistUpdateOrCreateConsumer(
            mapper,
            mediatorMock.Object,
            loggerFactoryMock.Object,
            statisticsStoreMock.Object,
            servicesOptionsMock.Object
        );
        
        typeof(PlaylistUpdateOrCreateConsumer)
            .GetField("_trackService", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(consumer, trackServiceMock.Object);

        // Act
        await consumer.Consume(contextMock.Object);

        // Assert
        mediatorMock.Verify(m =>
                m.Publish(It.Is<object>(cmd => cmd is UpdateMetadataCommand),
                    It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task AddStatistics_Should_Merge_Stats_And_Store_Unique_Only()
    {
        // Arrange
        var metadataId = Guid.NewGuid();
        var existingDate = DateTime.UtcNow.Date.AddDays(-1);
        var newDate = DateTime.UtcNow.Date;

        var statInfoSet = new HashSet<StatInfo>
        {
            new() { Timestamp = existingDate, DailyDiff = 100, Value = 300 },
            new() { Timestamp = newDate, DailyDiff = 200, Value = 200},
        };

        var statsFromDb = new List<CatalogStatistics>
        {
            new()
            {
                MetadataId = metadataId,
                Date = existingDate,
                Followers = 100,
                FollowersDailyDiff = 50
            }
        };

        var statisticsStoreMock = new Mock<IPersistenceStore<CatalogStatistics, Guid>>();
        statisticsStoreMock.Setup(s => s.GetAsync(
            It.IsAny<Expression<Func<CatalogStatistics, bool>>>(),
            It.IsAny<Expression<Func<CatalogStatistics, long>>>(),
            It.IsAny<PartitionKey>(),
            0,
            10,
            true,
            It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new PersistenceResult<IEnumerable<CatalogStatistics>>(statsFromDb, false, null));

        var storedStats = new List<CatalogStatistics>();
        statisticsStoreMock.Setup(s => s.StoreAsync(It.IsAny<CatalogStatistics>(), It.IsAny<CancellationToken>()))
            .Callback<CatalogStatistics, CancellationToken>((stat, _) => storedStats.Add(stat))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        var loggerFactoryMock = new Mock<ILoggerFactory>();
        var loggerMock = new Mock<ILogger<PlaylistUpdateOrCreateConsumer>>();
        loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(loggerMock.Object);

        var mediatorMock = new Mock<IMediator>();
        var servicesOptionsMock = new Mock<IOptions<ServicesOuterOptions>>();
        servicesOptionsMock.Setup(o => o.Value).Returns(new ServicesOuterOptions
        {
            AggregatorService = "http://localhost",
            TrackService = "http://localhost",
            AuthorizationService = "http://localhost",
            InferenceService = "http://localhost",
            PlaylistService = "http://localhost",
            UiEndpoint = "http://localhost",
            VectorService = "http://localhost",
            SubscriptionService = "http://localhost"
        });

        var mapperMock = new Mock<IMapper>();

        var consumer = new PlaylistUpdateOrCreateConsumer(
            mapperMock.Object,
            mediatorMock.Object,
            loggerFactoryMock.Object,
            statisticsStoreMock.Object,
            servicesOptionsMock.Object
        );

        var method = typeof(PlaylistUpdateOrCreateConsumer)
            .GetMethod("AddStatistics", BindingFlags.NonPublic | BindingFlags.Instance)!;

        // Act
        await (Task)method.Invoke(consumer, new object[] { statInfoSet, metadataId })!;

        // Assert
        Assert.Single(storedStats); // existing + new
        Assert.Contains(storedStats, s => s.Date == newDate);
    }
}
