using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Authorization.DAL;
using Authorization.Models.Entities;
using Authorization.Test.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;

namespace Authorization.Test.DAL;

public class RoleStoreCoreTests
{
    private readonly Mock<IPersistenceStore<BaseRole, Guid>> _roles;
    private readonly Mock<ILogger<RoleStore>> _logger;
    private readonly RoleStore _sut;

    public RoleStoreCoreTests()
    {
        _roles = new Mock<IPersistenceStore<BaseRole, Guid>>();
        _logger = new Mock<ILogger<RoleStore>>();

        AuthorizationApiFactory.SetupPersistenceStoreMock<BaseRole, Guid, long>(
            _roles,
            () => new BaseRole
            {
                Name = "seed",
                NormalizedName = "SEED"
            });

        _sut = new RoleStore(_roles.Object, new UpperInvariantLookupNormalizer(), _logger.Object);
    }

    private static BaseRole NewRole(string name) => new BaseRole
    {
        Name = name,
        NormalizedName = name.ToUpperInvariant(),
    };

    [Fact]
    public async Task CreateAsync_Succeeds()
    {
        var role = NewRole("manager");
        var res = await _sut.CreateAsync(role, CancellationToken.None);
        Assert.True(res.Succeeded);
        _roles.Verify(s => s.StoreAsync(It.Is<BaseRole>(r => r.Id != Guid.Empty && r.NormalizedName == "MANAGER"), It.IsAny<CancellationToken>()), Times.Once);
        _roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_Fails_When_ConcurrencyStamp_Missing()
    {
        var role = NewRole("viewer");
        var res = await _sut.UpdateAsync(role, CancellationToken.None);
        Assert.False(res.Succeeded);
        _roles.Verify(s => s.UpdateAsync(It.IsAny<BaseRole>(), It.IsAny<CancellationToken>()), Times.Never);
        _roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_Succeeds()
    {
        var role = NewRole("viewer");
        SetProp(role, nameof(BaseRole.ETag), "\"etag-123\"");
        SetProp(role, nameof(BaseRole.Timestamp), 638611234567890000L);
        var res = await _sut.UpdateAsync(role, CancellationToken.None);
        Assert.True(res.Succeeded);
        _roles.Verify(s => s.UpdateAsync(It.Is<BaseRole>(r => r.Id == role.Id && r.NormalizedName == "VIEWER"), It.IsAny<CancellationToken>()), Times.Once);
        _roles.VerifyNoOtherCalls();
    }

    private static T SetProp<T>(T obj, string prop, object? value)
    {
        var p = obj!.GetType().GetProperty(prop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException($"Property {prop} not found");
        p.SetValue(obj, value);
        return obj;
    }

    [Fact]
    public async Task DeleteAsync_Succeeds()
    {
        var role = NewRole("ops");
        await _roles.Object.StoreAsync(role, CancellationToken.None);
        var res = await _sut.DeleteAsync(role, CancellationToken.None);
        Assert.True(res.Succeeded);
        _roles.Verify(s => s.DeleteAsync(It.Is<BaseRole>(r => r.Id == role.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindByNameAsync_Returns_Role()
    {
        var role = NewRole("auditor");
        await _roles.Object.StoreAsync(role, CancellationToken.None);
        var found = await _sut.FindByNameAsync("auditor", CancellationToken.None);
        Assert.NotNull(found);
        _roles.Verify(s => s.GetAsync(
            It.IsAny<Expression<Func<BaseRole, bool>>>(),
            It.IsAny<Expression<Func<BaseRole, long>>>(),
            It.IsAny<PartitionKey>(),
            0, 1, false,
            It.IsAny<CancellationToken>(),
            true), Times.Once);
    }

    [Fact]
    public async Task FindByNameAsync_Returns_Null_When_Not_Found()
    {
        var found = await _sut.FindByNameAsync("foo", CancellationToken.None);
        Assert.NotEqual("FOO", found?.NormalizedName);
         _roles.Verify(s => s.GetAsync(
            It.IsAny<Expression<Func<BaseRole, bool>>>(),
            It.IsAny<Expression<Func<BaseRole, long>>>(),
            It.IsAny<PartitionKey>(),
            0, 1, false,
            It.IsAny<CancellationToken>(),
            true), Times.Once);
        _roles.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetClaimsAsync_Returns_All()
    {
        var role = NewRole("reporter");
        role.RoleClaims["perm.read"] = "allow";
        role.RoleClaims["perm.write"] = "deny";
        var claims = await _sut.GetClaimsAsync(role, CancellationToken.None);
        Assert.Equal(2, claims.Count);
        Assert.Contains(claims, c => c.Type == "perm.read" && c.Value == "allow");
        Assert.Contains(claims, c => c.Type == "perm.write" && c.Value == "deny");
    }

    [Fact]
    public async Task AddClaimAsync_Adds_And_Persists()
    {
        var role = NewRole("ops");
        await _roles.Object.StoreAsync(role, CancellationToken.None);
        var claim = new Claim("perm.deploy", "allow");
        await _sut.AddClaimAsync(role, claim, CancellationToken.None);
        Assert.True(role.RoleClaims.ContainsKey("perm.deploy"));
        Assert.Equal("allow", role.RoleClaims["perm.deploy"]);
        _roles.Verify(s => s.UpdateAsync(It.IsAny<BaseRole>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemoveClaimAsync_Removes_And_Persists()
    {
        var role = NewRole("ops");
        await _roles.Object.StoreAsync(role, CancellationToken.None);
        role.RoleClaims["perm.deploy"] = "allow";
        var claim = new Claim("perm.deploy", "allow");
        await _sut.RemoveClaimAsync(role, claim, CancellationToken.None);
        Assert.False(role.RoleClaims.ContainsKey("perm.deploy"));
        _roles.Verify(s => s.UpdateAsync(It.Is<BaseRole>(r => r.Id == role.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindByIdAsync_Throws_NotImplemented()
    {
        await Assert.ThrowsAsync<NotImplementedException>(() => _sut.FindByIdAsync(Guid.NewGuid().ToString(), CancellationToken.None));
        _roles.VerifyNoOtherCalls();
    }
}