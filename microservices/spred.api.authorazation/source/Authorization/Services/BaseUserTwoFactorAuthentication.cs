using System.IdentityModel.Tokens.Jwt;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Extensions.Configuration;
using Extensions.Extensions;
using Extensions.Interfaces;
using Extensions.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Authorization.Services;

/// <summary>
/// Two-Factor Authentication implementation
/// </summary>
public class BaseUserTwoFactorAuthentication : IUserTwoFactorTokenProvider<BaseUser>
{
    private readonly ILogger<BaseUserTwoFactorAuthentication> _logger;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly JwtSettings _jwtSettings;
    private readonly IJwtKeyProvider _jwtKeyProvider;
    private readonly IUserBaseClaimsPrincipalFactory _userClaimPrincipalFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseUserTwoFactorAuthentication"/> class.
    /// </summary>
    /// <param name="tokenHandler">The JWT security token handler.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="userClaimPrincipalFactory">Claim factory.</param>
    /// <param name="jwtSettings">The JWT settings.</param>
    /// <param name="jwtKeyProvider">The JWT key provide.</param>
    public BaseUserTwoFactorAuthentication(
        JwtSecurityTokenHandler tokenHandler, 
        ILoggerFactory loggerFactory,
        IUserBaseClaimsPrincipalFactory userClaimPrincipalFactory,
        IOptions<JwtSettings> jwtSettings, IJwtKeyProvider jwtKeyProvider)
    {
        _jwtKeyProvider = jwtKeyProvider;
        _jwtSettings = jwtSettings.Value;
        _tokenHandler = tokenHandler;
        _logger = loggerFactory.CreateLogger<BaseUserTwoFactorAuthentication>();
        _userClaimPrincipalFactory = userClaimPrincipalFactory;
    }

    /// <summary>
    /// Determines whether a two-factor token can be generated.
    /// </summary>
    /// <param name="manager">The user manager.</param>
    /// <param name="user">The user instance.</param>
    /// <returns>True if the token can be generated; otherwise, false.</returns>
    public Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<BaseUser>? manager, BaseUser? user)
    {
        return Task.FromResult(manager != null && user != null);
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string purpose, UserManager<BaseUser> manager, BaseUser user)
    {
        var scheme = JwtBearerDefaults.AuthenticationScheme;
        var userUpdated = await manager.FindByIdAsync(user.Id.ToString());
        
        if(userUpdated == null)
            throw new ArgumentException("User not found");
        
        var identity = await _userClaimPrincipalFactory.CreateAsync(userUpdated, scheme);

        var token = await GenerateToken(Enum.Parse<TokenPurposes>(purpose),     
            identity.Claims.ToDictionary(k => k.Type, v => (object)v.Value));
        var tokenStr = _tokenHandler.WriteToken(token);

        return tokenStr;
    }
    
    /// <inheritdoc />
    public Task<bool> ValidateAsync(string purpose, string token, UserManager<BaseUser> manager, BaseUser user)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Generates a JWT token.
    /// </summary>
    /// <param name="purpose">The purpose of the token.</param>
    /// <param name="authClaims">The authentication claims.</param>
    /// <returns>The generated security token.</returns>
    private async Task<SecurityToken> GenerateToken(TokenPurposes purpose, IDictionary<string, object> authClaims)
    {
        bool external = TokenPurposes.ExternalUserToken == purpose;
        
        if(!await _jwtKeyProvider.DecryptionKeyExistsAsync() || !await _jwtKeyProvider.SigningKeyExistsAsync(external))
            await _jwtKeyProvider.RotateKeys();

        var key = await _jwtKeyProvider.GetSigningKeyAsync(external);
        var decryptKey = await _jwtKeyProvider.GetDecryptionKeyAsync();

        var token = _tokenHandler.CreateToken(new SecurityTokenDescriptor()
        {
            Issuer = _jwtSettings.ValidExternalIssuer,
            Audience = _jwtSettings.ValidExternalAudience,
            Expires = TokenLifeTime.EndDate(purpose),
            Claims = authClaims,
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            EncryptingCredentials = new EncryptingCredentials(
            decryptKey,
            SecurityAlgorithms.Aes256KW,
            SecurityAlgorithms.Aes128CbcHmacSha256)
        });

        return token;
    }
}
