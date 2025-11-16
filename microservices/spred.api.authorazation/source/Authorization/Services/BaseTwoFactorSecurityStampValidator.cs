using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace Authorization.Services;

/// <summary>
/// A custom implementation of the <see cref="TwoFactorSecurityStampValidator{TUser}"/> designed to validate
/// the security stamp of users in two-factor authentication scenarios within the system.
/// </summary>
/// <remarks>
/// This class is intended for use with a custom user type, <see cref="BaseUser"/>, which extends the
/// <see cref="IdentityUser{TKey}"/> with an additional identifier based on <see cref="Guid"/>.
/// </remarks>
public class BaseTwoFactorSecurityStampValidator : TwoFactorSecurityStampValidator<BaseUser>
{
    /// <summary>
    /// .ctor
    /// </summary>
    /// <param name="options"></param>
    /// <param name="signInManager"></param>
    /// <param name="logger"></param>
    public BaseTwoFactorSecurityStampValidator(IOptions<SecurityStampValidatorOptions> options, 
        BaseSignInManager<BaseUser> signInManager, ILoggerFactory logger) : base(options, signInManager, logger)
    { }
}