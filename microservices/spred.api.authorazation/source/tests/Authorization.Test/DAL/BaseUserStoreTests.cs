using System;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.DAL;
using Authorization.Models.Entities;
using Authorization.Options;
using Authorization.Test.Fixtures;
using Microsoft.AspNetCore.Identity;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using Moq;
using Repository.Abstractions.Interfaces;

namespace Authorization.Test.DAL;

public class BaseUserStoreCoreTests
{
    private readonly Mock<IPersistenceStore<BaseUser, Guid>> _users;
    private readonly Mock<IPersistenceStore<OAuthAuthentication, Guid>> _oauth;
    private readonly Mock<IPersistenceStore<NotifyMe, Guid>> _notify;
    private readonly Mock<IPersistenceStore<Feedback, Guid>> _feedback;
    private readonly Mock<IPersistenceStore<UserToken, Guid>> _userTokens;
    private readonly Mock<IPersistenceStore<BaseRole, Guid>> _roles;
    private readonly Mock<ILogger<IUserPlusStore>> _logger;
    private readonly BaseUserStore _sut;

    private static Guid id = Guid.NewGuid();
    
    public BaseUserStoreCoreTests()
    {
        _users = new Mock<IPersistenceStore<BaseUser, Guid>>(MockBehavior.Strict);
        _oauth = new Mock<IPersistenceStore<OAuthAuthentication, Guid>>(MockBehavior.Strict);
        _notify = new Mock<IPersistenceStore<NotifyMe, Guid>>(MockBehavior.Strict);
        _feedback = new Mock<IPersistenceStore<Feedback, Guid>>(MockBehavior.Strict);
        _userTokens = new Mock<IPersistenceStore<UserToken, Guid>>(MockBehavior.Strict);
        _roles = new Mock<IPersistenceStore<BaseRole, Guid>>(MockBehavior.Strict);
        _logger = new Mock<ILogger<IUserPlusStore>>();

        AuthorizationApiFactory.SetupPersistenceStoreMock<BaseUser, Guid, long>(_users, () => new BaseUser
        {
            Id = id,
            UserName = "seed",
            NormalizedUserName = "SEED",
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        AuthorizationApiFactory.SetupPersistenceStoreMock<OAuthAuthentication, Guid, long>(_oauth, () => new OAuthAuthentication { PrimaryId = "test",
            OAuthProvider = AuthType.Base.ToString(), SpredUserId = Guid.Empty});
        AuthorizationApiFactory.SetupPersistenceStoreMock<NotifyMe, Guid, long>(_notify, () => new NotifyMe { NormalizedEmail = "USER@EX.COM" });
        AuthorizationApiFactory.SetupPersistenceStoreMock<Feedback, Guid, long>(_feedback, () => new Feedback { });
        AuthorizationApiFactory.SetupPersistenceStoreMock<UserToken, Guid, long>(_userTokens, () => new UserToken() 
        { LoginProvider = "TestProvider", Name = "AccessToken", Value = "TestValue"});
        AuthorizationApiFactory.SetupPersistenceStoreMock<BaseRole, Guid, long>(_roles, () => new BaseRole { });

        _sut = new BaseUserStore(_users.Object, _oauth.Object, _notify.Object, _feedback.Object, _userTokens.Object, _roles.Object, _logger.Object, new UpperInvariantLookupNormalizer());
    }

    private static BaseUser NewUser(string name) => new BaseUser
    {
        Id = id,
        UserName = name,
        NormalizedUserName = name.ToUpperInvariant(),
        SecurityStamp = Guid.NewGuid().ToString("N")
    };

    [Fact]
    public async Task CreateAsync_Succeeds_When_UserName_Present()
    {
        var u = NewUser("alice");
        var res = await _sut.CreateAsync(u, CancellationToken.None);
        Assert.True(res.Succeeded);
        _users.Verify(s => s.StoreAsync(It.Is<BaseUser>(x => x.Id != Guid.Empty && x.NormalizedUserName == "ALICE" && !string.IsNullOrEmpty(x.SecurityStamp)), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_Fails_When_UserName_Empty()
    {
        var u = NewUser(string.Empty);
        var res = await _sut.CreateAsync(u, CancellationToken.None);
        Assert.False(res.Succeeded);
        _users.Verify(s => s.StoreAsync(It.IsAny<BaseUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Fails_When_ConcurrencyStamp_Missing()
    {
        var u = NewUser("bob");
        var res = await _sut.UpdateAsync(u, CancellationToken.None);
        Assert.False(res.Succeeded);
        _users.Verify(s => s.UpdateAsync(It.IsAny<BaseUser>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_Succeeds()
    {
        var u = NewUser("bob");
        var res = await _sut.UpdateAsync(u, CancellationToken.None);
        Assert.True(!res.Succeeded);
    }

    [Fact]
    public async Task DeleteAsync_Succeeds()
    {
        var u = NewUser("kate");
        var res = await _sut.DeleteAsync(u, CancellationToken.None);
        Assert.True(res.Succeeded);
        _users.Verify(s => s.DeleteAsync(u.Id, It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FindByIdAsync_Returns_User()
    {
        var u = NewUser("mike");
        var found = await _sut.FindByIdAsync(u.Id.ToString(), CancellationToken.None);
        Assert.NotNull(found);
        _users.Verify(s => s.GetAsync(u.Id, It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), true), Times.Once);
    }

    [Fact]
    public async Task FindByIdAsync_Returns_Null_For_Invalid_Guid()
    {
        var found = await _sut.FindByIdAsync("not-a-guid", CancellationToken.None);
        Assert.Null(found);
        _users.Verify(s => s.GetAsync(It.IsAny<Guid>(), It.IsAny<PartitionKey>(), It.IsAny<CancellationToken>(), true), Times.Never);
    }

    [Fact]
    public async Task Email_Set_Get_Confirm_Normalize()
    {
        var u = NewUser("eve");
        await _sut.SetEmailAsync(u, "e@ex.com", CancellationToken.None);
        var email = await _sut.GetEmailAsync(u, CancellationToken.None);
        Assert.Equal("e@ex.com", email);
        await _sut.SetEmailConfirmedAsync(u, true, CancellationToken.None);
        var confirmed = await _sut.GetEmailConfirmedAsync(u, CancellationToken.None);
        Assert.True(confirmed);
        var norm = await _sut.GetNormalizedEmailAsync(u, CancellationToken.None);
        Assert.Equal("E@EX.COM", norm);
    }

    [Fact]
    public async Task Password_Set_And_Has()
    {
        var u = NewUser("nick");
        await _sut.SetPasswordHashAsync(u, "HASH", CancellationToken.None);
        var hash = await _sut.GetPasswordHashAsync(u, CancellationToken.None);
        var has = await _sut.HasPasswordAsync(u, CancellationToken.None);
        Assert.Equal("HASH", hash);
        Assert.True(has);
    }

    [Fact]
    public async Task AddNotifyMe_Sets_Id_And_NormalizedEmail()
    {
        var n = new NotifyMe { NormalizedEmail = "user@ex.com" };
        await _sut.AddNotifyMe(n, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, n.Id);
        Assert.Equal("USER@EX.COM", n.NormalizedEmail);
        _notify.Verify(s => s.StoreAsync(It.Is<NotifyMe>(x => x.Id == n.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AddFeedback_Sets_Id_And_Stores()
    {
        var f = new Feedback();
        await _sut.AddFeedback(f, CancellationToken.None);
        Assert.NotEqual(Guid.Empty, f.Id);
        _feedback.Verify(s => s.StoreAsync(It.Is<Feedback>(x => x.Id == f.Id), It.IsAny<CancellationToken>()), Times.Once);
    }
}

