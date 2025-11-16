using Extensions.Configuration;
using Extensions.Extensions;

namespace Authorization.Helpers;

/// <summary>
/// Helper class for managing cookies related to JWT tokens.
/// </summary>
public class CookieHelper
{
    private readonly CookieOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="CookieHelper"/> class.
    /// </summary>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="environment"></param>
    public CookieHelper(IConfiguration configuration, IHostEnvironment environment)
    {
        if (environment.IsDevelopment() || environment.EnvironmentName == "Test")
        {
            _options = new CookieOptions
            {
                Path = "/",
                Domain = configuration["Domain:UiDomain"],
                Secure = true,
                HttpOnly = false,
                SameSite = SameSiteMode.Strict,
                MaxAge = TokenLifeTime.GetTimeSpan(TokenPurposes.ExternalUserToken),
            };
        }
        else
        {
            _options = new CookieOptions
            {
                Path = "/",
                Domain = ".spred.io",
                Secure = true,
                HttpOnly = true,
                SameSite = SameSiteMode.None,
                MaxAge = TokenLifeTime.GetTimeSpan(TokenPurposes.ExternalUserToken),
            };
        }
    }

    /// <summary>
    /// Adds a JWT token to the response cookies with encryption.
    /// </summary>
    /// <param name="cookies">The response cookies collection.</param>
    /// <param name="token">The JWT token to be added.</param>
    public void AddSpredAccess(IResponseCookies cookies, string token)
    {
        cookies.Append("Spred.Access", token, _options);
    }
}
