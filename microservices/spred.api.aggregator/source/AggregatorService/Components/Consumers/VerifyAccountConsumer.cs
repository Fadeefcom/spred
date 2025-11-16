using System.Text.Json;
using System.Text.RegularExpressions;
using AggregatorService.Abstractions;
using Extensions.Extensions;
using MassTransit;
using Spred.Bus.Contracts;

namespace AggregatorService.Components.Consumers;

/// <summary>
/// This consumer handles the verification of user accounts by implementing the
/// IConsumer interface for the <see cref="VerifyAccountCommand"/> message type.
/// </summary>
/// <remarks>
/// The VerifyAccountConsumer is designed to verify accounts by utilizing Spotify's API.
/// It consumes <see cref="VerifyAccountCommand"/> messages, attempts to verify the account,
/// and publishes a <see cref="VerifyAccountResult"/> upon completion. It also handles token
/// acquisition and rotation to support multiple attempts if necessary.
/// </remarks>
/// <example>
/// This consumer processes commands containing user and account information with a token,
/// validates the user credentials against an external service (e.g., Spotify),
/// and publishes the result of the verification process.
/// </example>
/// <seealso cref="IConsumer{T}"/>
public sealed class VerifyAccountConsumer : IConsumer<VerifyAccountCommand>
{
    private readonly ISpotifyApi _api;
    private readonly ISpotifyTokenProvider _tokens;
    private readonly IPublishEndpoint _publish;

    /// <summary>
    /// Represents a consumer that handles the processing of <see cref="VerifyAccountCommand"/> messages
    /// to verify user accounts via an external API.
    /// </summary>
    /// <remarks>
    /// This class utilizes injected dependencies to interact with the Spotify API, handle token
    /// management, and publish verification results. It serves as the entry point for consuming
    /// account verification requests in the message processing pipeline.
    /// </remarks>
    /// <seealso cref="IConsumer{T}"/>
    public VerifyAccountConsumer(ISpotifyApi api, ISpotifyTokenProvider tokens, IPublishEndpoint publish)
    {
        _api = api;
        _tokens = tokens;
        _publish = publish;
    }

    /// <summary>
    /// Handles the consumption of <see cref="VerifyAccountCommand"/> messages to verify user accounts
    /// by interacting with external APIs and managing tokens.
    /// </summary>
    /// <param name="context">
    /// The context containing the received <see cref="VerifyAccountCommand"/> message and metadata required for processing.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation of consuming and processing the message.
    /// The result of the operation is the publishing of a verification result message.
    /// </returns>
    public async Task Consume(ConsumeContext<VerifyAccountCommand> context)
    {
        var cmd = context.Message;
        var pattern = Regex.Escape(cmd.Token);
        var (bearer, credIndex) = await _tokens.AcquireAsync();

        for (var attempts = 0; attempts < 4; attempts++)
        {
            var (result, rotate) = await TryVerifyAsync(bearer, cmd, pattern);
            if (result is not null)
            {
                await _publish.Publish(result, context.CancellationToken);
                return;
            }

            if (rotate)
            {
                (bearer, credIndex) = await _tokens.RotateAsync(credIndex);
                continue;
            }

            break;
        }

        await _publish.Publish(new VerifyAccountResult(cmd.UserId, cmd.AccountId, false, null, string.Empty),
            context.CancellationToken);
    }

    private async Task<(VerifyAccountResult? result, bool rotateToken)> TryVerifyAsync(string bearer,
        VerifyAccountCommand cmd, string pattern)
    {
        var resp = await _api.GetUserPlaylists(bearer, cmd.AccountId, 50, 0);
        var code = (int)resp.StatusCode;

        if (code == 401 || code == 403)
            return (null, true);

        if (code == 404)
            return (
                new VerifyAccountResult(cmd.UserId, cmd.AccountId, false, null,
                    "Spotify user profile not found. Please make sure you entered a valid Spotify user ID or profile link."),
                false);

        if (!resp.IsSuccessStatusCode)
            return (new VerifyAccountResult(cmd.UserId, cmd.AccountId, false, null, string.Empty), false);

        if (!resp.Content.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array ||
            items.GetArrayLength() == 0)
            return (new VerifyAccountResult(cmd.UserId, cmd.AccountId, false, null, string.Empty), false);

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("id", out var idEl) || idEl.ValueKind != JsonValueKind.String)
                continue;

            var id = idEl.GetString();
            var name = item.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String
                ? nameEl.GetString() ?? string.Empty
                : string.Empty;
            var description =
                item.TryGetProperty("description", out var descEl) && descEl.ValueKind == JsonValueKind.String
                    ? descEl.GetString() ?? string.Empty
                    : string.Empty;

            var inName = Regex.IsMatch(name, pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            var inDesc = !inName && Regex.IsMatch(description, pattern,
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            if (!inName && !inDesc)
                continue;

            using var doc = JsonDocument.Parse("{}");
            var proof = new JsonObjectBuilder()
                .Add("playlistId", id)
                .Add("playlistName", name)
                .Add("matchedIn", inName ? "name" : "description")
                .Add("matchedToken", cmd.Token)
                .Build();

            return (new VerifyAccountResult(cmd.UserId, cmd.AccountId, true, proof, string.Empty), false);
        }

        return (new VerifyAccountResult(cmd.UserId, cmd.AccountId, false, null, string.Empty), false);
    }
}
