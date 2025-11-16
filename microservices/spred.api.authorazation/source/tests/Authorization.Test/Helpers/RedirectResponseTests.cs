using Authorization.Helpers;
using Extensions.Models;

namespace Authorization.Test.Helpers;

public class RedirectResponseTests
{
    [Fact]
    public void BuildCallback_ShouldReturnAppUrl()
    {
        // Arrange
        var serviceOptions = Microsoft.Extensions.Options.Options.Create(new ServicesOuterOptions
        {
            AggregatorService = "https://example.com",
            PlaylistService = "https://example.com",
            AuthorizationService = "https://example.com",
            InferenceService = "https://example.com",
            TrackService = "https://example.com",
            UiEndpoint = "https://app.example.com",
            VectorService = "https://example.com",
            SubscriptionService = "https://example.com"
        });

        var helper = new RedirectResponse(serviceOptions);

        // Act
        var result = helper.BuildCallback(false, "artist");

        // Assert
        Assert.Equal("https://app.example.com/artist", result);
    }
}