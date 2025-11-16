using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Authorization.Abstractions;
using Authorization.Models.Entities;
using Authorization.Services;
using Authorization.Services.Consumers;
using Authorization.Test.Mocks;
using Extensions.Interfaces;
using MassTransit;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using Spred.Bus.Contracts;

namespace Authorization.Test;

public class VerifyAccountResultConsumerTests
{
    private readonly Mock<ILinkedAccountEventStore> _storeMock = new();
    private readonly Mock<ILoggerFactory> _loggerFactoryMock = new();
    private readonly Mock<ILogger<VerifyAccountResultConsumer>> _loggerMock = new();
    private readonly BaseManagerServices _manager;
    private readonly Mock<IUserPlusStore> _userStoreMock = new();
    private readonly Mock<IGetToken> _getTokenMock = new();
    private readonly Mock<IConfiguration> _configurationMock = new();

    public VerifyAccountResultConsumerTests()
    {
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(_loggerMock.Object);

        _storeMock.Setup(s => s.AppendAsync(
                It.IsAny<string>(),
                It.IsAny<Guid>(),
                It.IsAny<AccountPlatform>(),
                It.IsAny<LinkedAccountEventType>(),
                It.IsAny<JObject>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);
        
        _manager = MockBaseManagerServices.CreateMock(
            _userStoreMock,
            _getTokenMock,
            _configurationMock
        );
    }

    private VerifyAccountResultConsumer CreateConsumer()
        => new VerifyAccountResultConsumer(_storeMock.Object, _manager, _loggerFactoryMock.Object);

    private static ConsumeContext<VerifyAccountResult> CreateContext(VerifyAccountResult msg)
    {
        var ctx = new Mock<ConsumeContext<VerifyAccountResult>>();
        ctx.SetupGet(c => c.Message).Returns(msg);
        return ctx.Object;
    }

    [Fact]
    public async Task Consume_ShouldReturn_WhenUserNotFound()
    {
        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(Guid.NewGuid(), "acc", true, null);

        await consumer.Consume(CreateContext(msg));

        _storeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Consume_ShouldReturn_WhenAccountNotFound()
    {
        var user = new BaseUser { UserAccounts = new List<UserAccountRef>() };
        _userStoreMock.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(Guid.NewGuid(), "missing", true, null);

        await consumer.Consume(CreateContext(msg));

        _storeMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Consume_ShouldReturn_WhenStateIsVerified()
    {
        var user = new BaseUser
        {
            UserAccounts = new List<UserAccountRef> { new(AccountPlatform.Spotify, "acc", "url") }
        };
        _userStoreMock.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _storeMock.Setup(s => s.GetCurrentState("acc", AccountPlatform.Spotify, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedAccountState
            {
                AccountId = "acc",
                UserId = Guid.NewGuid(),
                Platform = AccountPlatform.Spotify
            });

        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(Guid.NewGuid(), "acc", true, null);

        await consumer.Consume(CreateContext(msg));

        _storeMock.Verify(s => s.AppendAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<AccountPlatform>(),
            It.IsAny<LinkedAccountEventType>(), It.IsAny<JObject>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task Consume_ShouldAppendProofAttached_WhenProofProvided()
    {
        var user = new BaseUser
        {
            UserAccounts = new List<UserAccountRef> { new(AccountPlatform.Spotify, "acc", "url") }
        };
        _userStoreMock.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _storeMock.Setup(s => s.GetCurrentState("acc", AccountPlatform.Spotify, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedAccountState
            {
                AccountId = "acc",
                UserId = user.Id,
                Platform = AccountPlatform.Spotify
            });

        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(user.Id, "acc", true, "proof");

        await consumer.Consume(CreateContext(msg));

        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.ProofAttached, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldAppendVerifiedAndLinked_WhenVerifiedTrue()
    {
        var user = new BaseUser
        {
            UserAccounts = new List<UserAccountRef> { new(AccountPlatform.Spotify, "acc", "url") }
        };
        _userStoreMock.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _storeMock.Setup(s => s.GetCurrentState("acc", AccountPlatform.Spotify, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedAccountState
            {
                AccountId = "acc",
                UserId = user.Id,
                Platform = AccountPlatform.Spotify
            });

        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(user.Id, "acc", true, null);

        await consumer.Consume(CreateContext(msg));

        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.AccountVerified, null, It.IsAny<CancellationToken>()), Times.Once);
        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.AccountLinked, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Consume_ShouldAppendProofInvalid_WhenVerifiedFalse()
    {
        var user = new BaseUser
        {
            UserAccounts = new List<UserAccountRef> { new(AccountPlatform.Spotify, "acc", "url") }
        };
        _userStoreMock.Setup(s => s.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _storeMock.Setup(s => s.GetCurrentState("acc", AccountPlatform.Spotify, user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LinkedAccountState
            {
                AccountId = "acc",
                UserId = user.Id,
                Platform = AccountPlatform.Spotify
            });

        var consumer = CreateConsumer();
        var msg = new VerifyAccountResult(user.Id, "acc", false, null, "Invalid proof");

        await consumer.Consume(CreateContext(msg));

        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.ProofInvalid, null, It.IsAny<CancellationToken>()), Times.Once);

        // Проверка, что не добавлены Verified/Linked
        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.AccountVerified, null, It.IsAny<CancellationToken>()), Times.Never);
        _storeMock.Verify(s => s.AppendAsync("acc", msg.UserId, AccountPlatform.Spotify,
            LinkedAccountEventType.AccountLinked, null, It.IsAny<CancellationToken>()), Times.Never);
    }
}
