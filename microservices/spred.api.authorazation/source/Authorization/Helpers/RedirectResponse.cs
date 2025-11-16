using System.Text;
using Extensions.Models;
using Microsoft.Extensions.Options;

namespace Authorization.Helpers;

/// <summary>
/// Redirect response builder
/// </summary>
public class RedirectResponse
{
    private readonly string _uiRedirectUrl;

    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="serviceOuterOptions"></param>
    public RedirectResponse(IOptions<ServicesOuterOptions> serviceOuterOptions)
    {
        _uiRedirectUrl = serviceOuterOptions.Value.UiEndpoint;
    }
    
    /// <summary>
    /// Build callBack Url 
    /// </summary>
    /// <returns></returns>
    public string BuildCallback(bool justRegistered, string role)
    {
        StringBuilder redirectResult = new StringBuilder();
        redirectResult.Append(_uiRedirectUrl + $"/{role.ToLowerInvariant()}");
        
        if (justRegistered && role.Equals("artist", StringComparison.OrdinalIgnoreCase))
            redirectResult.Append("/upload");

        return redirectResult.ToString();
    }
}