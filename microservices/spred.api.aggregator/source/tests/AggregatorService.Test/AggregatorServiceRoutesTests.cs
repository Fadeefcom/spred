using System.Net;
using System.Net.Http.Headers;
using System.Text;
using AggregatorService.Test.Fixtures;
using FluentAssertions;
using Moq;

namespace AggregatorService.Test;

public class AggregatorServiceRoutesTests : IClassFixture<AggregateServiceApiFactory>
{
    private readonly HttpClient _client;
    private readonly AggregateServiceApiFactory _factory;

    public AggregatorServiceRoutesTests(AggregateServiceApiFactory factory)
    {
        factory.EnableTestAuth = true;
        _client = factory.CreateClient();
        _factory = factory;
    }

    [Theory]
    [InlineData("", HttpStatusCode.Unauthorized)]
    [InlineData("invalid", HttpStatusCode.Unauthorized)]
    [InlineData("test-access-key", HttpStatusCode.BadRequest)]
    public async Task GetParserCommand_BasedOnAccessKey(string accessKey, HttpStatusCode expectedStatus)
    {
        var response = await _client.GetAsync($"/aggregator/parser?accessKey={accessKey}");
        response.StatusCode.Should().Be(expectedStatus);
    }

    [Fact]
    public async Task PostParser_WithValidFile_ReturnsCreated()
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes("fake content"));
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
        content.Add(fileContent, "fromFile", "track.mp3");

        var id = Guid.NewGuid();
        var response = await _client.PostAsync($"/aggregator/parser/{id}?accessKey=test-access-key", content);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Theory]
    [InlineData("", HttpStatusCode.Unauthorized)]
    [InlineData("invalid", HttpStatusCode.Unauthorized)]
    public async Task PostParser_WithInvalidKey_ReturnsUnauthorized(string accessKey, HttpStatusCode expected)
    {
        var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(Encoding.UTF8.GetBytes("fake")), "fromFile", "file.mp3");

        var id = Guid.NewGuid();
        var response = await _client.PostAsync($"/aggregator/parser/{id}?accessKey={accessKey}", content);
        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task GetUnsuccessful_WithValidAccessKey_ReturnsOk()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/aggregator/parser/{id}/unsuccessful?accessKey=test-access-key");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetUnsuccessful_WithInvalidAccessKey_ReturnsUnauthorized()
    {
        var id = Guid.NewGuid();
        var response = await _client.GetAsync($"/aggregator/parser/{id}/unsuccessful?accessKey=bad-key");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TooManyInvalidRequests_ShouldBlockFurtherRequests()
    {
        var id = Guid.NewGuid();
        for (int i = 0; i < 6; i++)
        {
            await _client.GetAsync($"/aggregator/parser/{id}/unsuccessful?accessKey=invalid");
        }

        var final = await _client.GetAsync($"/aggregator/parser/{id}/unsuccessful?accessKey=test-key");
        final.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
    
    [Fact]
    public async Task GetParser_WhenBlocked_ReturnsUnauthorized()
    {
        _factory.ParserAccessGateMock.Reset();
        _factory.ParserAccessGateMock.Setup(x => x.IsBlocked()).Returns(true);

        var response = await _client.GetAsync("/aggregator/parser?accessKey=test-key");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetParser_WhenNotBlocked_AndKeyValid_ReturnsOk()
    {
        _factory.ParserAccessGateMock.Reset();
        _factory.ParserAccessGateMock.Setup(x => x.IsBlocked()).Returns(false);

        var response = await _client.GetAsync("/aggregator/parser?accessKey=test-access-key");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetParser_WhenNotBlocked_AndKeyInvalid_IncrementsFailure()
    {
        _factory.ParserAccessGateMock.Reset();
        _factory.ParserAccessGateMock.Setup(x => x.IsBlocked()).Returns(false);

        var response = await _client.GetAsync("/aggregator/parser?accessKey=wrong");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _factory.ParserAccessGateMock.Verify(x => x.RegisterFailure(), Times.Once);
    }
} 
