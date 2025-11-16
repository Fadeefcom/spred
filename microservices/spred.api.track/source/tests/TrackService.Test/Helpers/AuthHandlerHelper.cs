using System.Globalization;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TrackService.Test.Helpers;

public class AuthHandlerHelper : AuthenticationHandler<AuthenticationSchemeOptions>
{
    [Obsolete]
    public AuthHandlerHelper(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock)
        : base(options, logger, encoder, clock) { }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var expires = DateTimeOffset.UtcNow.AddDays(30).ToUnixTimeSeconds();
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.Empty.ToString()),
            new Claim(ClaimTypes.Email, "test@example.com"),
            new Claim(ClaimTypes.Name, "TestUser"),
            new Claim("Premium", "true"),
            new Claim("Premium_exp", expires.ToString(CultureInfo.InvariantCulture))
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}