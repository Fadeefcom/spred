using Extensions.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using PlaylistService.Abstractions;
using PlaylistService.DependencyExtensions;
using PlaylistService.Models.Entities;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Models;
using StackExchange.Redis;

namespace PlaylistService.Test;

public class DiExtensionsTests
{
    [Fact]
    public async Task InitTestPlaylistsAsync_StoresMetadataAndStats_WhenNotExists()
    {
        // Arrange
        var metadataStoreMock = new Mock<IPersistenceStore<CatalogMetadata, Guid>>();
        var statsStoreMock = new Mock<IPersistenceStore<CatalogStatistics, Guid>>();
        
        metadataStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new  PersistenceResult<CatalogMetadata>(null, false, null));

        var services = new ServiceCollection();
        services.AddSingleton(metadataStoreMock.Object);
        services.AddSingleton(statsStoreMock.Object);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        await DiExtensions.InitTestPlaylistsAsync(scope);

        // Assert
        metadataStoreMock.Verify(x => x.StoreAsync(It.IsAny<CatalogMetadata>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        statsStoreMock.Verify(x => x.StoreAsync(It.IsAny<CatalogStatistics>(), It.IsAny<CancellationToken>()), Times.Exactly(4));
    }

    [Fact]
    public async Task InitTestPlaylistsAsync_SkipsInsert_WhenAlreadyExists()
    {
        // Arrange
        var existingEntity = new PlaylistMetadata();
        var metadataStoreMock = new Mock<IPersistenceStore<CatalogMetadata, Guid>>();
        var statsStoreMock = new Mock<IPersistenceStore<CatalogStatistics, Guid>>();

        metadataStoreMock
            .Setup(x => x.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new  PersistenceResult<CatalogMetadata>(new CatalogMetadata(), false, null));

        var services = new ServiceCollection();
        services.AddSingleton(metadataStoreMock.Object);
        services.AddSingleton(statsStoreMock.Object);

        var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // Act
        await DiExtensions.InitTestPlaylistsAsync(scope);

        // Assert
        metadataStoreMock.Verify(x => x.StoreAsync(It.IsAny<CatalogMetadata>(), It.IsAny<CancellationToken>()), Times.Never);
        statsStoreMock.Verify(x => x.StoreAsync(It.IsAny<CatalogStatistics>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}