using System.Text.Encodings.Web;
using System.Text.Json;
using Extensions.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Authorization.Extensions;

/// <summary>
/// Custom OAuth handler for Yandex with code reuse protection.
/// Prevents multiple exchanges of the same authorization code.
/// </summary>
public class CustomYandexHandler : OAuthHandler<OAuthOptions>
{
    private readonly IDatabase _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomYandexHandler"/> class.
    /// </summary>
    /// <param name="options">Monitored options for the OAuth handler.</param>
    /// <param name="logger">Logger factory instance.</param>
    /// <param name="encoder">URL encoder.</param>
    /// <param name="clock">System clock instance.</param>
    /// <param name="connection">Memory cache for tracking used codes.</param>
    [Obsolete("Obsolete")]
    public CustomYandexHandler(IOptionsMonitor<OAuthOptions> options, ILoggerFactory logger, UrlEncoder encoder,
        ISystemClock clock, IConnectionMultiplexer connection)
        : base(options, logger, encoder, clock)
    {
        _cache = connection.GetDatabase();
    }

    /// <summary>
    /// Overrides the authorization code exchange logic to prevent duplicate processing.
    /// </summary>
    /// <param name="context">The code exchange context containing the authorization code and redirect URI.</param>
    /// <returns>The token response or an error if the code was already used.</returns>
    protected override async Task<OAuthTokenResponse> ExchangeCodeAsync(OAuthCodeExchangeContext context)
    {
        Logger.LogSpredInformation("ExchangeCodeAsync",$"ExchangeCodeAsync triggered: code = {context.Code}, redirect_uri = {context.RedirectUri}");
        var cachedJson = await _cache.StringGetAsync(context.Code);
        if (cachedJson is { HasValue: true, IsNullOrEmpty: false })
        {
            Logger.LogSpredWarning("ExchangeCodeAsync", $"Code {context.Code} already used, skipping exchange");
            var doc = JsonDocument.Parse(cachedJson.ToString());
            return OAuthTokenResponse.Success(doc);
        }

        var response = await base.ExchangeCodeAsync(context);
        await _cache.StringSetAsync(context.Code, response.Response!.RootElement.GetRawText(), TimeSpan.FromMinutes(1));
        return response;
    }
}