using System.Security.Claims;
using Authorization.Abstractions;
using Extensions.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Authorization.Services;

/// <summary>
/// Sign in Manager implementation
/// </summary>
/// <typeparam name="TUser"></typeparam>
public class BaseSignInManager<TUser> : SignInManager<TUser> where TUser : class
{
    private readonly BaseManagerServices _baseManagerServices;
    private readonly ILogger<SignInManager<TUser>> _logger;
    private readonly IConfiguration _configuration;
    private readonly IUserBaseClaimsPrincipalFactory? _claimsFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseSignInManager{TUser}"/> class.
    /// </summary>
    /// <param name="userManager">The user manager service.</param>
    /// <param name="contextAccessor">The HTTP context accessor.</param>
    /// <param name="claimsFactory">The claims principal factory.</param>
    /// <param name="optionsAccessor">The identity options accessor.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="schemes">The authentication scheme provider.</param>
    /// <param name="confirmation">The user confirmation service.</param>
    /// <param name="configuration">The application configuration.</param>
    public BaseSignInManager(BaseManagerServices userManager, IHttpContextAccessor contextAccessor,
        IUserBaseClaimsPrincipalFactory claimsFactory, IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<TUser>> logger, IAuthenticationSchemeProvider schemes,
        IUserConfirmation<TUser> confirmation, IConfiguration configuration)
        : base((userManager as UserManager<TUser>)!,
        contextAccessor, (claimsFactory as IUserClaimsPrincipalFactory<TUser>)!, optionsAccessor,
        logger, schemes, confirmation)
    {
        _baseManagerServices = userManager;
        _logger = logger;
        _configuration = configuration;
        _claimsFactory = claimsFactory;
    }

    /// <summary>
    /// Not implemented. Throws <see cref="NotImplementedException"/>.
    /// </summary>
    /// <param name="user">The user instance.</param>
    /// <returns>Throws NotImplementedException.</returns>  
    public override Task<bool> CanSignInAsync(TUser user)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Signs out the current user and removes related cookies.
    /// </summary>
    public override async Task SignOutAsync()
    {
        var httpContext = Context;

        httpContext.Response.Cookies.Delete("Spred.Access", new CookieOptions
        {
            Domain = _configuration["Domain:UiDomain"],
            Path = "/"
        });
        
        httpContext.Response.Cookies.Delete("Spred.Refresh", new CookieOptions
        {
            Domain = _configuration["Domain:ApiDomain"],
            Path = "/auth"
        });

        await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Validates the security stamp for a given principal.
    /// </summary>
    /// <param name="principal">The claims principal to validate.</param>
    /// <returns>The validated user if valid; otherwise, null.</returns>
    public override async Task<TUser?> ValidateSecurityStampAsync(ClaimsPrincipal? principal)
    {
        var id = _baseManagerServices.GetUserId(principal);

        if (principal != null && !string.IsNullOrWhiteSpace(id))
        {
            var user = await _baseManagerServices.FindByIdAsync(id, CancellationToken.None);
            if (await ValidateSecurityStampAsync(user as TUser, principal.FindFirstValue(ClaimTypes.Sid)))
            {
                return user as TUser;
            }
        }

        _logger.LogSpredDebug("SecurityStampValidationFailedId", "Failed to validate a security stamp.");
        return null;
    }

    /// <summary>
    /// Renews the principal with a new claims principal.
    /// </summary>
    /// <param name="principal"></param>
    /// <returns></returns>
    public async Task RenewPrincipal(ClaimsPrincipal principal)
    {
        var user = await _baseManagerServices.GetUserAsync(principal);
        var newPrincipal = await _claimsFactory!.CreateAsync(user!, CookieAuthenticationDefaults.AuthenticationScheme);
        await Context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, newPrincipal);
    }
}