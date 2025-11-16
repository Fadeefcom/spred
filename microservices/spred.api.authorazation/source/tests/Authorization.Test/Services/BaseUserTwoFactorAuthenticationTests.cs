using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Services;
using Authorization.Test.Mocks;
using Extensions.Interfaces;
using Extensions.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace Authorization.Test.Services;

public class BaseUserTwoFactorAuthenticationTests
{
    private readonly Mock<JwtSecurityTokenHandler> _tokenHandlerMock = new();
    private readonly Mock<IJwtKeyProvider> _jwtKeyProviderMock = new();
    private readonly Mock<IServiceProvider> _serviceProviderMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<IOptions<JwtSettings>> _jwtSettingsMock = new();
    private readonly Mock<IUserBaseClaimsPrincipalFactory> _claimsFactoryMock = new();
    
    private readonly JwtSettings _jwtSettings = new JwtSettings
    {
        ValidInternalAudience = "test-aud",
        ValidInternalIssuer = "test-issuer",
        ValidExternalAudience = "test-aud",
        ValidExternalIssuer = "test-issuer"
    };

    public BaseUserTwoFactorAuthenticationTests()
    {
        _jwtSettingsMock.Setup(x => x.Value).Returns(_jwtSettings);
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(Mock.Of<ILogger<BaseUserTwoFactorAuthentication>>());
    }

    [Fact]
    public async Task CanGenerateTwoFactorTokenAsync_ShouldReturnTrue_IfUserAndManagerNotNull()
    {
        var auth = CreateAuthService();
        var userManagerMock = MockUserManagerHelper.CreateMock<BaseUser>();
        var result = await auth.CanGenerateTwoFactorTokenAsync(userManagerMock.Object, new BaseUser());
        Assert.True(result);
    }

    [Fact]
    public async Task CanGenerateTwoFactorTokenAsync_ShouldReturnFalse_IfUserOrManagerNull()
    {
        var auth = CreateAuthService();
        var userManagerMock = MockUserManagerHelper.CreateMock<BaseUser>();
        Assert.False(await auth.CanGenerateTwoFactorTokenAsync(null, new BaseUser()));
        Assert.False(await auth.CanGenerateTwoFactorTokenAsync(userManagerMock.Object, null));
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnToken()
    {
        var user = new object();
        var claims = new List<Claim> { new("sub", "user-id") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<object>>();
        claimsFactoryMock.Setup(x => x.CreateAsync(user))
            .ReturnsAsync(claimsPrincipal);

        _serviceProviderMock.Setup(x => x.GetService(typeof(IUserClaimsPrincipalFactory<object>)))
            .Returns(claimsFactoryMock.Object);

        _jwtKeyProviderMock.Setup(x => x.DecryptionKeyExistsAsync()).ReturnsAsync(true);
        _jwtKeyProviderMock.Setup(x => x.SigningKeyExistsAsync(It.IsAny<bool>())).ReturnsAsync(true);
        _jwtKeyProviderMock.Setup(x => x.GetSigningKeyAsync(It.IsAny<bool>())).ReturnsAsync(new SymmetricSecurityKey(new byte[32]));
        _jwtKeyProviderMock.Setup(x => x.GetDecryptionKeyAsync(false)).ReturnsAsync(new SymmetricSecurityKey(new byte[32]));

        _tokenHandlerMock.Setup(x => x.CreateToken(It.IsAny<SecurityTokenDescriptor>()))
            .Returns(new JwtSecurityToken());
        _tokenHandlerMock.Setup(x => x.WriteToken(It.IsAny<SecurityToken>())).Returns("mocked-token");

        var auth = CreateAuthService();
        var userManagerMock = MockUserManagerHelper.CreateMock<BaseUser>();
        var result = await auth.CanGenerateTwoFactorTokenAsync(userManagerMock.Object, new BaseUser());

        Assert.True(result);
    }

    [Fact]
    public async Task GenerateAsync_ShouldReturnToken_ForAccessTokenPurpose()
    {
        var user = new BaseUser();
        var claims = new List<Claim> { new("sub", "user-id") };
        var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims));

        _claimsFactoryMock
            .Setup(x => x.CreateAsync(It.IsAny<BaseUser>(), It.IsAny<string>()))
            .ReturnsAsync(claimsPrincipal);

        _jwtKeyProviderMock.Setup(x => x.DecryptionKeyExistsAsync()).ReturnsAsync(true);
        _jwtKeyProviderMock.Setup(x => x.SigningKeyExistsAsync(It.IsAny<bool>())).ReturnsAsync(true);
        _jwtKeyProviderMock.Setup(x => x.GetSigningKeyAsync(It.IsAny<bool>()))
            .ReturnsAsync(new SymmetricSecurityKey(new byte[32]));
        _jwtKeyProviderMock.Setup(x => x.GetDecryptionKeyAsync(true))
            .ReturnsAsync(new SymmetricSecurityKey(new byte[32]));

        _tokenHandlerMock.Setup(x => x.CreateToken(It.IsAny<SecurityTokenDescriptor>()))
            .Returns(new JwtSecurityToken());
        _tokenHandlerMock.Setup(x => x.WriteToken(It.IsAny<SecurityToken>()))
            .Returns("mocked-token");

        var userManager = MockUserManagerHelper.CreateMock<BaseUser>();
        userManager
            .Setup(x => x.FindByIdAsync(user.Id.ToString()))
            .ReturnsAsync(user);
        
        var auth = CreateAuthService();

        var token = await auth.GenerateAsync("AccessToken", userManager.Object, user);

        Assert.Equal("mocked-token", token);
    }

    [Fact]
    public async Task ValidateAsync_ShouldThrowNotImplementedException()
    {
        // Arrange
        var auth = CreateAuthService();
        var userManager = MockUserManagerHelper.CreateMock<BaseUser>();
        var user = new BaseUser();

        // Act + Assert
        await Assert.ThrowsAsync<NotImplementedException>(() =>
            auth.ValidateAsync("AccessToken", "token", userManager.Object, user));
    }

    private BaseUserTwoFactorAuthentication CreateAuthService()
    {
        return new BaseUserTwoFactorAuthentication(
            _tokenHandlerMock.Object,
            _loggerFactoryMock.Object,
            _claimsFactoryMock.Object,
            _jwtSettingsMock.Object,
            _jwtKeyProviderMock.Object);
    }
}

