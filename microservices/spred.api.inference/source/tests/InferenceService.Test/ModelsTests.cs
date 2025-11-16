using FluentAssertions;
using InferenceService.Models;
using InferenceService.Models.Dto;
using Newtonsoft.Json;

namespace InferenceService.Test;

public class ModelsTests
{
    [Fact]
    public void Should_Serialize_And_Deserialize_Correctly()
    {
        var original = new SimilarityResultDto
        {
            Id = Guid.NewGuid(),
            SpredUserId = Guid.NewGuid(),
            TrackId = Guid.NewGuid(),
            Similarity = 0.89f
        };

        var json = JsonConvert.SerializeObject(original);
        var deserialized = JsonConvert.DeserializeObject<SimilarityResultDto>(json);

        deserialized.Should().BeEquivalentTo(original);
    }

    [Fact]
    public void Should_Have_Correct_Json_Properties()
    {
        var json = """
                   {
                       "id": "7fd6d303-b202-4b2e-b69a-85ea801099d2",
                       "SpredUserId": "c5b4e63c-1fcf-4c10-b1ee-6f84fa9d64a2",
                       "TrackId": "a1b1e63c-1fcf-4c10-b1ee-6f84fa9d64b2",
                       "Similarity": 0.75
                   }
                   """;

        var result = JsonConvert.DeserializeObject<SimilarityResultDto>(json);

        result.Id.Should().Be(Guid.Parse("7fd6d303-b202-4b2e-b69a-85ea801099d2"));
        result.SpredUserId.Should().Be(Guid.Parse("c5b4e63c-1fcf-4c10-b1ee-6f84fa9d64a2"));
        result.TrackId.Should().Be(Guid.Parse("a1b1e63c-1fcf-4c10-b1ee-6f84fa9d64b2"));
        result.Similarity.Should().Be(0.75f);
    }
    
    [Fact]
    public void Should_Create_Record_With_Correct_Values()
    {
        var stats = new RetryStats(5, 2);

        stats.Total.Should().Be(5);
        stats.Retry.Should().Be(2);
    }

    [Fact]
    public void Should_Equal_When_Same_Values()
    {
        var a = new RetryStats(3, 1);
        var b = new RetryStats(3, 1);

        a.Should().Be(b);
    }

    [Fact]
    public void Should_Not_Equal_When_Different_Values()
    {
        var a = new RetryStats(3, 1);
        var b = new RetryStats(4, 2);

        a.Should().NotBe(b);
    }
}