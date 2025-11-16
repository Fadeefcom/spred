using System;
using System.Text.Encodings.Web;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Services;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;

namespace Authorization.Test.Mocks;

public static class MockBaseSignInManager
{
    public static BaseSignInManager<TUser> CreateMock<TUser>(
    Mock<IUserPlusStore> userStoreMock,
    Mock<IGetToken> getTokenMock,
    Mock<IConfiguration> configurationMock,
    Mock<IThumbprintService> thumbServiceMock,
    Mock<IAuthenticationService> authServiceMock
) where TUser : BaseUser
    {
        var baseManagerServices = MockBaseManagerServices.CreateMock(userStoreMock, getTokenMock, configurationMock);

        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddSingleton(UrlEncoder.Default);
        services.AddSingleton(authServiceMock.Object);
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme);

        httpContext.RequestServices = services.BuildServiceProvider();

        var httpContextAccessor = new Mock<IHttpContextAccessor>();
        httpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);
        
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("true"));
        dbMock.Setup(x => x.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromHours(5));

        var muxMock = new Mock<IConnectionMultiplexer>();
        muxMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);

        var baseClaimsFactory = new UserBaseClaimsPrincipalFactory(Mock.Of<IRoleStore<BaseRole>>(), muxMock.Object);
        var claimsFactory = (IUserBaseClaimsPrincipalFactory)baseClaimsFactory;

        var identityOptions = Microsoft.Extensions.Options.Options.Create(new IdentityOptions());
        var logger = Mock.Of<ILogger<SignInManager<TUser>>>();
        var schemeProvider = Mock.Of<IAuthenticationSchemeProvider>();
        var confirmation = Mock.Of<IUserConfirmation<TUser>>();

        return new BaseSignInManager<TUser>(
           baseManagerServices,
           httpContextAccessor.Object,
           claimsFactory,
           identityOptions,
           logger,
           schemeProvider,
           confirmation,
           configurationMock.Object);
    }
}
