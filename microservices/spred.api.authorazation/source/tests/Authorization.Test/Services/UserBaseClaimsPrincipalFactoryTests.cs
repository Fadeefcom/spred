using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Authorization.Models.Entities;
using Authorization.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using StackExchange.Redis;

namespace Authorization.Test.Services;

public class UserBaseClaimsPrincipalFactoryTests
{
    private readonly Mock<IRoleStore<BaseRole> > _roleStoreMock; 
    private readonly DefaultHttpContext _httpContext = new();
    private readonly Mock<IHttpContextAccessor> _httpAccessorMock;
    private readonly Mock<IConnectionMultiplexer> _muxMock = new();

    public UserBaseClaimsPrincipalFactoryTests()
    {
        _roleStoreMock = new Mock<IRoleStore<BaseRole>>();
        _httpAccessorMock = new Mock<IHttpContextAccessor>(); 
        _httpAccessorMock.Setup(x => x.HttpContext).Returns(_httpContext);
        
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(new RedisValue("true"));
        dbMock.Setup(x => x.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(TimeSpan.FromHours(5));

        _muxMock.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
            .Returns(dbMock.Object);
    }

    private sealed class TestUser : BaseUser
    {
        public TestUser()
        {
            Id = Guid.NewGuid();
            UserName = "testuser";
            Email = "test@example.com";
            SecurityStamp = "sec123";
        }
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnClaimsPrincipal_WithExpectedClaims()
    {
        // Arrange
        var user = new TestUser();
        var scheme = "JwtBearer";
        var factory = new UserBaseClaimsPrincipalFactory(_roleStoreMock.Object, _muxMock.Object);

        // Act
        var principal = await factory.CreateAsync(user, scheme);

        // Assert
        var identity = (ClaimsIdentity)principal.Identity!;
        Assert.Equal(scheme, identity.AuthenticationType);
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }
    
    [Fact]
    public async Task CreateAsync_ShouldReturnClaimsPrincipal_WithExpectedClaims2()
    {
        // Arrange
        var user = new TestUser();
        var factory = new UserBaseClaimsPrincipalFactory(_roleStoreMock.Object, _muxMock.Object);

        // Act
        var principal = await factory.CreateAsync(user);

        // Assert
        var identity = (ClaimsIdentity)principal.Identity!;
        Assert.Equal("Cookies", identity.AuthenticationType);
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Name && c.Value == user.UserName);
        Assert.Contains(identity.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
    }
}
