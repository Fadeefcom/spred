using Authorization.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Authorization.Test.Helpers;

public class CookieHelperTests
{
    [Fact]
    public void AddSpredAccess_ShouldUseDevelopmentOptions()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Domain:UiDomain"]).Returns("example.dev");

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Development");

        var mockCookies = new Mock<IResponseCookies>();

        CookieOptions capturedOptions = null;
        mockCookies
            .Setup(c => c.Append("Spred.Access", "dev-token", It.IsAny<CookieOptions>()))
            .Callback<string, string, CookieOptions>((_, _, opt) => capturedOptions = opt);

        var helper = new CookieHelper(mockConfig.Object, mockEnv.Object);
        helper.AddSpredAccess(mockCookies.Object, "dev-token");

        Assert.NotNull(capturedOptions);
        Assert.Equal("/", capturedOptions.Path);
        Assert.Equal("example.dev", capturedOptions.Domain);
        Assert.True(capturedOptions.Secure);
        Assert.False(capturedOptions.HttpOnly);
        Assert.Equal(SameSiteMode.Strict, capturedOptions.SameSite);
        Assert.NotNull(capturedOptions.MaxAge);

        mockCookies.Verify(c => c.Append(
            "Spred.Access",
            "dev-token",
            It.IsAny<CookieOptions>()), Times.Once);
    }

    [Fact]
    public void AddSpredAccess_ShouldUseProductionOptions()
    {
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["Domain:UiDomain"]).Returns("example.com");

        var mockEnv = new Mock<IHostEnvironment>();
        mockEnv.Setup(e => e.EnvironmentName).Returns("Production");

        var mockCookies = new Mock<IResponseCookies>();

        CookieOptions capturedOptions = null;
        mockCookies
            .Setup(c => c.Append("Spred.Access", "prod-token", It.IsAny<CookieOptions>()))
            .Callback<string, string, CookieOptions>((_, _, opt) => capturedOptions = opt);

        var helper = new CookieHelper(mockConfig.Object, mockEnv.Object);
        helper.AddSpredAccess(mockCookies.Object, "prod-token");

        Assert.NotNull(capturedOptions);
        Assert.Equal("/", capturedOptions.Path);
        Assert.Equal(".spred.io", capturedOptions.Domain);
        Assert.True(capturedOptions.Secure);
        Assert.True(capturedOptions.HttpOnly);
        Assert.Equal(SameSiteMode.None, capturedOptions.SameSite);
        Assert.NotNull(capturedOptions.MaxAge);

        mockCookies.Verify(c => c.Append(
            "Spred.Access",
            "prod-token",
            It.IsAny<CookieOptions>()), Times.Once);
    }
}

