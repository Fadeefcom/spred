using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Authorization.Configuration;
using Authorization.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Authorization.Test.DiExtensions;

public class RoleSeedTests
{
    private static Mock<RoleManager<BaseRole>> CreateRoleManagerMock()
    {
        var storeMock = new Mock<IRoleStore<BaseRole>>();
        var roleManagerMock = new Mock<RoleManager<BaseRole>>(
            storeMock.Object,
            Array.Empty<IRoleValidator<BaseRole>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            Mock.Of<ILogger<RoleManager<BaseRole>>>()
        );
        return roleManagerMock;
    }

    private static IServiceScope CreateScopeReturning(RoleManager<BaseRole> roleManager)
    {
        var sp = new Mock<IServiceProvider>();
        sp.Setup(x => x.GetService(typeof(RoleManager<BaseRole>))).Returns(roleManager);

        var scope = new Mock<IServiceScope>();
        scope.Setup(x => x.ServiceProvider).Returns(sp.Object);
        return scope.Object;
    }

    [Fact]
    public async Task InitRoles_CreatesMissingRoles()
    {
        var rm = CreateRoleManagerMock();

        rm.Setup(x => x.FindByNameAsync("Artist")).ReturnsAsync((BaseRole?)null);
        rm.Setup(x => x.FindByNameAsync("Curator")).ReturnsAsync((BaseRole?)null);

        var created = new List<BaseRole>();
        rm.Setup(x => x.CreateAsync(It.IsAny<BaseRole>()))
          .Callback<BaseRole>(r => created.Add(r))
          .ReturnsAsync(IdentityResult.Success);

        var scope = CreateScopeReturning(rm.Object);

        await RoleSeed.InitRoles(scope);

        rm.Verify(x => x.CreateAsync(It.IsAny<BaseRole>()), Times.Exactly(2));

        var artist = created.Single(r => r.Name == "Artist");
        Assert.Equal("own", artist.RoleClaims["permission:track:*"]);
        Assert.Equal("own", artist.RoleClaims["permission:playlist:read"]);

        var curator = created.Single(r => r.Name == "Curator");
        Assert.Equal("own", curator.RoleClaims["permission:track:*"]);
        Assert.Equal("own", curator.RoleClaims["permission:playlist:*"]);
        Assert.Equal("own", curator.RoleClaims["permission:analytics:view"]);
    }

    [Fact]
    public async Task InitRoles_AddsMissingClaimsForExistingRoles()
    {
        var rm = CreateRoleManagerMock();

        var artist = new BaseRole("Artist");
        var curator = new BaseRole("Curator");

        rm.Setup(x => x.FindByNameAsync("Artist")).ReturnsAsync(artist);
        rm.Setup(x => x.FindByNameAsync("Curator")).ReturnsAsync(curator);

        rm.Setup(x => x.GetClaimsAsync(artist))
          .ReturnsAsync(new List<Claim>
          {
              new("permission:track:*","own")
          });

        rm.Setup(x => x.GetClaimsAsync(curator))
          .ReturnsAsync(new List<Claim>
          {
              new("permission:track:*","own"),
              new("permission:playlist:*","own"),
              new("permission:analytics:view","own")
          });

        rm.Setup(x => x.AddClaimAsync(artist, It.Is<Claim>(c => c.Type == "permission:playlist:read" && c.Value == "own")))
          .ReturnsAsync(IdentityResult.Success);

        rm.Setup(x => x.AddClaimAsync(curator, It.IsAny<Claim>()))
          .ReturnsAsync(IdentityResult.Success);

        var scope = CreateScopeReturning(rm.Object);

        await RoleSeed.InitRoles(scope);

        rm.Verify(x => x.CreateAsync(It.IsAny<BaseRole>()), Times.Never);
        rm.Verify(x => x.AddClaimAsync(artist, It.Is<Claim>(c => c.Type == "permission:playlist:read" && c.Value == "own")), Times.Once);
        rm.Verify(x => x.AddClaimAsync(curator, It.IsAny<Claim>()), Times.Never);
        rm.Verify(x => x.FindByNameAsync("Artist"), Times.AtLeast(1));
        rm.Verify(x => x.FindByNameAsync("Curator"), Times.AtLeast(1));
    }
}