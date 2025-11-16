using System;
using System.Threading.Tasks;
using Authorization.Configuration;
using Authorization.DiExtensions;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Test.Fixtures;
using Extensions.DiExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Authorization.Test.DiExtensions;

public class DiExtensionTests : IClassFixture<AuthorizationApiFactory>
{
    private readonly AuthorizationApiFactory _factory;

    public DiExtensionTests(AuthorizationApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public void AddAuthorizationServices_ShouldNotThrow()
    {
        var services = new ServiceCollection();
        var result = services.AddAppAuthorization();
        Assert.NotNull(result);
    }

    [Fact]
    public void PostConfigure_ShouldSetTimeProvider()
    {
        var options = new SecurityStampValidatorOptions();
        var timeProvider = TimeProvider.System;

        var config = new PostConfigureSecurityStampValidatorOptions(timeProvider);
        config.PostConfigure("test", options);

        Assert.Equal(timeProvider, options.TimeProvider);
    }
}

