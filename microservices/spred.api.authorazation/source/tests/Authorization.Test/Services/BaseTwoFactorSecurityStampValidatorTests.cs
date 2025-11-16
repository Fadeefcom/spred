using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Services;
using Authorization.Test.Mocks;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace Authorization.Test.Services;

public class BaseTwoFactorSecurityStampValidatorTests
{
    private readonly BaseSignInManager<BaseUser> _signInManagerMock;
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<BaseTwoFactorSecurityStampValidator>> _loggerMock = new();
    private static readonly IOptions<SecurityStampValidatorOptions> _options =
        Microsoft.Extensions.Options.Options.Create(new SecurityStampValidatorOptions
        {
            ValidationInterval = TimeSpan.FromMinutes(30)
        });
    private readonly Mock<IUserPlusStore> _userStoreMock = new();
    private readonly Mock<IGetToken> _getTokenMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();
    private readonly Mock<IThumbprintService> _thumbprintServiceMock = new();
    private readonly Mock<IAuthenticationService> _authServiceMock = new();

    public BaseTwoFactorSecurityStampValidatorTests()
    {
        _loggerFactoryMock
            .Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _signInManagerMock = MockBaseSignInManager.CreateMock<BaseUser>(
            _userStoreMock,
            _getTokenMock,
            _configurationMock,
            _thumbprintServiceMock,
            _authServiceMock
        );
    }

    private static ClaimsPrincipal CreatePrincipal(string userId = null)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId ?? Guid.NewGuid().ToString())
        }, "test"));
    }

    private static CookieValidatePrincipalContext CreateContext(ClaimsPrincipal principal)
    {
        var context = new DefaultHttpContext();
        var ticket = new AuthenticationTicket(principal, "Cookies");

        return new CookieValidatePrincipalContext(
            context,
            new AuthenticationScheme("Cookies", null, typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            ticket
        );
    }

    private BaseTwoFactorSecurityStampValidator CreateValidator()
    {
        return new BaseTwoFactorSecurityStampValidator(_options, _signInManagerMock, _loggerFactoryMock.Object);
    }

    [Fact]
    public async Task ValidateAsync_ShouldRejectPrincipal_WhenStampInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal);

        // Act
        await validator.ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal);
    }
}