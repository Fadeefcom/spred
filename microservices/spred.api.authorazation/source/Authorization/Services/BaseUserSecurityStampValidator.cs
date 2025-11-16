using System.Security.Claims;
using Extensions.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Authorization.Services;

/// <summary>
/// User Security stamp validator implementation
/// </summary>
/// <typeparam name="TUser"></typeparam>
public class BaseUserSecurityStampValidator<TUser> : SecurityStampValidator<TUser> where TUser : class
{
    private readonly BaseSignInManager<TUser> _signInManager;
    private readonly ILogger<BaseUserSecurityStampValidator<TUser>> _logger;

    /// <summary>
    /// Obsolete constructor for <see cref="BaseUserSecurityStampValidator{TUser}"/>.
    /// </summary>
    /// <param name="options">Security stamp validator options.</param>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="clock">System clock.</param>
    /// <param name="logger">Logger factory.</param>
    [Obsolete("Obsolete")]
    public BaseUserSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options,
        BaseSignInManager<TUser> signInManager,
        ISystemClock clock, ILoggerFactory logger) : base(options, signInManager, clock, logger)
    {
        _signInManager = signInManager;
        _logger = logger.CreateLogger<BaseUserSecurityStampValidator<TUser>>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseUserSecurityStampValidator{TUser}"/> class.
    /// </summary>
    /// <param name="options">Security stamp validator options.</param>
    /// <param name="signInManager">The sign-in manager.</param>
    /// <param name="logger">Logger factory.</param>
    public BaseUserSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options,
        BaseSignInManager<TUser> signInManager, ILoggerFactory logger)
        : base(options, signInManager, logger)
    {
        _signInManager = signInManager;
        _logger = logger.CreateLogger<BaseUserSecurityStampValidator<TUser>>();
    }

    /// <summary>
    /// Validates the security stamp for the current user.
    /// </summary>
    /// <param name="context">The cookie validation context.</param>
    public override async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var principal = context.Principal;

        if (ShouldValidate(context.Properties.IssuedUtc))
        {
            var user = await VerifySecurityStamp(principal);
            if (user == null)
            {
                await Reject(context);
                return;
            }
        }

        await _signInManager.RenewPrincipal(context.Principal!);
        //if(ShouldRenew(context.Properties.IssuedUtc))
            
    }

    /// <summary>
    /// Determines whether the security stamp should be validated based on the issued time.
    /// </summary>
    /// <param name="issuedUtc">The time the cookie was issued.</param>
    /// <returns>True if validation is required; otherwise, false.</returns>
    private bool ShouldValidate(DateTimeOffset? issuedUtc)
    {
        if (issuedUtc == null) return true;

        var timeElapsed = TimeProvider.GetUtcNow().Subtract(issuedUtc.Value);
        return timeElapsed > Options.ValidationInterval;
    }

    private bool ShouldRenew(DateTimeOffset? issuedUtc)
    {
        if (issuedUtc == null) return true;
        
        var timeElapsed = TimeProvider.GetUtcNow().Subtract(issuedUtc.Value);
        return timeElapsed > TimeSpan.FromDays(1);
    }

    /// <summary>
    /// Rejects the current principal and signs out the user.
    /// </summary>
    /// <param name="context">The cookie validation context.</param>
    private async Task Reject(CookieValidatePrincipalContext context)
    {
        _logger.LogSpredDebug("SecurityStampValidationFailed", "Security stamp validation failed, rejecting cookie.");
        await _signInManager.SignOutAsync();
        context.RejectPrincipal();
    }

    /// <summary>
    /// Verifies the security stamp for a given principal.
    /// </summary>
    /// <param name="principal">The claims principal to validate.</param>
    /// <returns>The validated user if valid; otherwise, null.</returns>
    protected override Task<TUser?> VerifySecurityStamp(ClaimsPrincipal? principal)
     => _signInManager.ValidateSecurityStampAsync(principal);
}