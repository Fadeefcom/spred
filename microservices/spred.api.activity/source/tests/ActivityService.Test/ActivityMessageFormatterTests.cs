using System.Text.Json;
using ActivityService.Components.Services;
using ActivityService.Models;
using Spred.Bus.Contracts;

public class ActivityMessageFormatterTests
{
    private readonly ActivityMessageFormatter _formatter = new();

    private static ActivityEntity CreateBaseEntity(string messageKey, IDictionary<string, object?>? args = null, JsonElement? before = null, JsonElement? after = null)
    {
        return new ActivityEntity
        {
            Id = Guid.NewGuid(),
            ActorUserId = Guid.NewGuid(),
            OtherPartyUserId = null,
            OwnerUserId = Guid.NewGuid(),
            ObjectType = "track",
            ObjectId = Guid.NewGuid(),
            Verb = "created",
            MessageKey = messageKey,
            Args = args ?? new Dictionary<string, object?>(),
            Before = before,
            After = after,
            CorrelationId = Guid.NewGuid().ToString(),
            Service = "ActivityService",
            Importance = ActivityImportance.Normal,
            Audience = "public",
            Sequence = 1,
            Tags = new[] { "tag1" },
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    [Fact]
    public void Format_ShouldReturn_SubmissionCreated_Message()
    {
        var entity = CreateBaseEntity(
            "submission.created",
            new Dictionary<string, object?>
            {
                ["trackName"] = "Dream On",
                ["catalogName"] = "Rock Legends"
            }
        );

        var result = _formatter.Format(entity);

        Assert.Equal("You submitted track Dream On to catalog Rock Legends.", result);
    }

    [Fact]
    public void Format_ShouldReturn_DisplayNameChanged_Message()
    {
        var entity = CreateBaseEntity(
            "user.display_name_changed",
            new Dictionary<string, object?>
            {
                ["oldName"] = "John",
                ["newName"] = "Johnny"
            }
        );

        var result = _formatter.Format(entity);

        Assert.Equal("Your display name was updated from John to Johnny.", result);
    }

    [Fact]
    public void Format_ShouldReturn_Default_Message_WhenUnknownKey()
    {
        var entity = CreateBaseEntity("unknown.key");
        var result = _formatter.Format(entity);

        Assert.Equal($"[created] track {entity.ObjectId}", result);
    }
}
