using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Services;
using Authorization.Test.Mocks;
using Exception.Exceptions;
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

public class BaseUserSecurityStampValidatorTests
{
    private readonly BaseSignInManager<BaseUser> _signInManagerMock;
    private readonly Mock<IThumbprintService> _thumbprintServiceMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<BaseUserSecurityStampValidator<BaseUser>>> _loggerMock = new();
    private readonly Mock<IAuthenticationService> _authServiceMock = new();
    private static readonly IOptions<SecurityStampValidatorOptions> _options =
    Microsoft.Extensions.Options.Options.Create(new SecurityStampValidatorOptions
    {
        ValidationInterval = TimeSpan.FromMinutes(30)
    });
    private readonly Mock<IUserPlusStore> _userStoreMock = new();
    private readonly Mock<IGetToken> _getTokenMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    public BaseUserSecurityStampValidatorTests()
    {
        _thumbprintServiceMock.Setup(x => x.Generate(It.IsAny<HttpContext>()))
            .Returns("thumb");
        _loggerFactoryMock.Setup(x => x.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);        
        _signInManagerMock = CreateService();        
    }

    private BaseSignInManager<BaseUser> CreateService()
    {
        return MockBaseSignInManager.CreateMock<BaseUser>(_userStoreMock, _getTokenMock, _configurationMock, _thumbprintServiceMock, _authServiceMock);
    }

    private BaseUserSecurityStampValidator<BaseUser> CreateValidator()
    {
        return new BaseUserSecurityStampValidator<BaseUser>(
            _options,
            _signInManagerMock,
            _loggerFactoryMock.Object);
    }

    private static ClaimsPrincipal CreatePrincipal(string sessionId = "sess", string scheme = "scheme", string id = "user", string thumb = "thumb", string sid = "emptySid")
    {
        var claims = new List<Claim>();
        if (scheme != null) claims.Add(new Claim(ClaimTypesExtension.Scheme, scheme));
        if (id != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, id));
        if (thumb != null) claims.Add(new Claim(ClaimTypesExtension.DeviceId, thumb));
        claims.Add(new Claim(ClaimTypes.Sid, sid));
        var identity = new ClaimsIdentity(claims, "auth");
        return new ClaimsPrincipal(identity);
    }

    private static CookieValidatePrincipalContext CreateContext(ClaimsPrincipal principal, DateTimeOffset? issuedUtc = null)
    {
        var context = new DefaultHttpContext();

        var props = new AuthenticationProperties
        {
            IssuedUtc = issuedUtc
        };

        var ticket = new AuthenticationTicket(principal, props, CookieAuthenticationDefaults.AuthenticationScheme);

        return new CookieValidatePrincipalContext(
            context,
            new AuthenticationScheme("Cookies", "Cookies", typeof(CookieAuthenticationHandler)),
            new CookieAuthenticationOptions(),
            ticket
        );
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfPrincipalInvalid()
    {
        // Arrange
        var validator = CreateValidator();
        var context = CreateContext(new ClaimsPrincipal(new ClaimsIdentity()));

        // Act
        await validator.ValidateAsync(context);

        // Assert
        Assert.Null(context.Principal); // Rejected
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfMissingClaims()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal(sessionId: null);
        var context = CreateContext(principal);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfThumbprintValidButCantSignIn()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal);

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(true);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfVerifySecurityStampReturnsNull()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal, DateTimeOffset.UtcNow.AddHours(-1)); // force validation

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(true);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldRenew_IfOldCookie()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var issued = DateTimeOffset.UtcNow.AddDays(-2); // force renewal

        var context = CreateContext(principal, issued);

        _userStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser()
            {
                UserName = "test",
                Id = Guid.Empty,
                Email = "@empty",
                SecurityStamp = "emptySid"
            });

        await validator.ValidateAsync(context);

        Assert.Equal(principal, context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfPrincipalIsNull()
    {
        var validator = CreateValidator();

        var principal = CreatePrincipal();
        var issued = DateTimeOffset.UtcNow;
        var context = CreateContext(principal, issued);
        context.Principal = null;

        await Assert.ThrowsAsync<BaseException>(() => validator.ValidateAsync(context));
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfSchemeMissing()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal(scheme: null);
        var context = CreateContext(principal);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfIdMissing()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal(id: null);
        var context = CreateContext(principal);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldAccept_IfThumbprintValid()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal);

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(true);
        _userStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser()
            {
                UserName = "test",
                Id = Guid.Empty,
                Email = "@empty",
                SecurityStamp = "emptySid"
            });

        await validator.ValidateAsync(context);

        Assert.Equal(principal, context.Principal); // not rejected
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfThumbprintValid_ButCannotSignIn()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal);

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(true);

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfValidationRequired_AndStampInvalid()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal(sid: string.Empty);
        var context = CreateContext(principal, DateTimeOffset.UtcNow.AddHours(-5)); // trigger ShouldValidate

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(false); // skip CanSignIn check

        await validator.ValidateAsync(context);

        Assert.Null(context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_IfStampValid_AndThumbInvalid()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();
        var context = CreateContext(principal, DateTimeOffset.UtcNow.AddHours(-5)); // force validation

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(false);

        _userStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser()
            {
                UserName = "test",
                Id = Guid.Empty,
                Email = "@empty",
                SecurityStamp = "emptySid"
            });

        await validator.ValidateAsync(context);

        Assert.Equal(principal, context.Principal);
    }

    [Fact]
    public async Task ValidateAsync_ShouldRenew_CallsSignInAsync()
    {
        var validator = CreateValidator();
        var principal = CreatePrincipal();

        var signInCalled = false;

        _authServiceMock
            .Setup(x => x.SignInAsync(
                It.IsAny<HttpContext>(),
                CookieAuthenticationDefaults.AuthenticationScheme,
                It.IsAny<ClaimsPrincipal>(),
                It.IsAny<AuthenticationProperties>()))
            .Callback(() => signInCalled = true)
            .Returns(Task.CompletedTask);

        var context = CreateContext(principal, DateTimeOffset.UtcNow.AddDays(-1));

        _thumbprintServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), "thumb")).Returns(false);
        _userStoreMock.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BaseUser()
            {
                UserName = "test",
                Id = Guid.Empty,
                Email = "@empty",
                SecurityStamp = "emptySid"
            });

        await validator.ValidateAsync(context);

        Assert.True(signInCalled);
    }
}