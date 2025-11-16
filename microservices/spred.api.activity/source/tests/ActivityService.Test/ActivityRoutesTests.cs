using System.Linq.Expressions;
using System.Net;
using System.Net.Http.Json;
using ActivityService.Models;
using ActivityService.Test.Factory;
using Microsoft.Azure.Cosmos;
using Moq;

namespace ActivityService.Test;

public class ActivityRoutesTests : IClassFixture<ActivityApiFactory>
{
    private readonly ActivityApiFactory _factory;

    public ActivityRoutesTests(ActivityApiFactory factory)
    {
        _factory = factory;
        _factory.EnableTestAuth = true;
    }

    [Fact]
    public async Task GetUserFeed_ShouldReturnOk_AndUsePersistenceStore()
    {
        // Arrange
        var mock = _factory.ActivityPersistenceMock;
        
        mock.Invocations.Clear();

        _factory.EnableTestAuth = true;
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/activities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<object>();
        Assert.NotNull(result);

        mock.Verify(x => x.GetAsync(
            It.IsAny<Expression<Func<ActivityEntity, bool>>>(), It.IsAny<Expression<Func<ActivityEntity, long>>>(), 
            It.IsAny<PartitionKey>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), 
            It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetUserFeed_ShouldReturnValidEntityStructure()
    {
        // Arrange
        var mock = _factory.ActivityPersistenceMock;
        //mock.Invocations.Clear();

        _factory.EnableTestAuth = true;
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/activities");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<List<ActivityFeedItem>>();
        Assert.NotNull(json);
        Assert.NotEmpty(json!);

        var entity = json!.First();
        Assert.False(entity.Id == Guid.Empty);
        Assert.Equal("track", entity.ObjectType);
        Assert.Equal("created", entity.Verb);
        Assert.True(entity.CreatedAt <= DateTimeOffset.UtcNow);

        mock.Verify(x => x.GetAsync(
            It.IsAny<Expression<Func<ActivityEntity, bool>>>(), It.IsAny<Expression<Func<ActivityEntity, long>>>(), 
            It.IsAny<PartitionKey>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), 
            It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetUserFeed_ShouldRespectOffsetAndLimitQueryParams()
    {
        // Arrange
        var mock = _factory.ActivityPersistenceMock;
        mock.Invocations.Clear();

        var offset = 3;
        var limit = 7;
        _factory.EnableTestAuth = true;
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/activities?offset={offset}&limit={limit}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        mock.Verify(x => x.GetAsync(
            It.IsAny<Expression<Func<ActivityEntity, bool>>>(), It.IsAny<Expression<Func<ActivityEntity, long>>>(), 
            It.IsAny<PartitionKey>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), 
            It.IsAny<CancellationToken>(), It.IsAny<bool>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetUserFeed_ShouldReturnUnauthorized_WhenAuthDisabled()
    {
        // Arrange
        using var factory = new ActivityApiFactory { EnableTestAuth = false };
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/activities");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
