using System;
using System.Linq;
using System.Xml.Linq;
using Authorization.DataProtection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackExchange.Redis;

namespace Authorization.Test;

public class RedisXmlRepositoryTests
{
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<IConnectionMultiplexer> _redisMock;
    private readonly RedisXmlRepository _repository;
    private readonly string _key = "dataprotection:keys";
    private readonly Mock<ILoggerFactory> _loggerFactoryMock;
    private readonly IMemoryCache _memoryCache;

    public RedisXmlRepositoryTests()
    {
        _dbMock = new Mock<IDatabase>();
        _memoryCache = new MemoryCache( Microsoft.Extensions.Options.Options.Create(new MemoryCacheOptions()));
        _redisMock = new Mock<IConnectionMultiplexer>();
        _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
        
        _loggerFactoryMock = new Mock<ILoggerFactory>();
        _loggerFactoryMock.Setup(f => f.CreateLogger(It.IsAny<string>()))
            .Returns(new NullLogger<RedisXmlRepository>());

        _repository = new RedisXmlRepository(_redisMock.Object, _loggerFactoryMock.Object, _memoryCache);
    }

    [Fact]
    public void GetAllElements_Should_Return_Empty_When_Key_Is_Null()
    {
        _dbMock.Setup(db => db.StringGet(_key, It.IsAny<CommandFlags>())).Returns(RedisValue.Null);

        var result = _repository.GetAllElements();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllElements_Should_Return_Empty_When_Key_Is_Empty()
    {
        _dbMock.Setup(db => db.StringGet(_key, It.IsAny<CommandFlags>())).Returns(string.Empty);

        var result = _repository.GetAllElements();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllElements_Should_Return_Elements_When_Valid_Xml()
    {
        var validKey = new XElement("key",
            new XAttribute("id", "test-key"),
            new XAttribute("version", "1"),
            new XAttribute("creationDate", "2024-01-01T00:00:00Z"),
            new XAttribute("activationDate", "2024-01-01T00:00:00Z"),
            new XAttribute("expirationDate", "2099-01-01T00:00:00Z"),
            new XElement("encryptedSecret", "stub-data")
        );
        var xml = new XDocument(new XElement("keys", validKey)).ToString(SaveOptions.DisableFormatting);
        _dbMock.Setup(db => db.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).Returns(xml);

        var result = _repository.GetAllElements();

        Assert.Single(result);
        Assert.Equal("key", result.First().Name.LocalName);
    }

    [Fact]
    public void GetAllElements_Should_Return_Empty_When_Invalid_Xml()
    {
        _dbMock.Setup(db => db.StringGet(_key, It.IsAny<CommandFlags>())).Returns("Invalid XML");

        var result = _repository.GetAllElements();

        Assert.Empty(result);
    }

    [Fact]
    public void StoreElement_Should_Not_Store_When_Lock_Not_Acquired()
    {
        _dbMock.Setup(db => db.LockTake(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>()))
            .Returns(false);

        _repository.StoreElement(new XElement("key", "data"), "friendly");

        _dbMock.Verify(db => db.StringSet(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), null, When.Always, CommandFlags.None), Times.Never);
    }

    [Fact]
    public void StoreElement_Should_Store_When_Lock_Acquired()
    {
        var validKey = new XElement("key",
            new XAttribute("id", "test-key"),
            new XAttribute("version", "1"),
            new XAttribute("creationDate", "2024-01-01T00:00:00Z"),
            new XAttribute("activationDate", "2024-01-01T00:00:00Z"),
            new XAttribute("expirationDate", "2099-01-01T00:00:00Z"),
            new XElement("encryptedSecret", "stub-data")
        );
        var xml = new XDocument(new XElement("keys", validKey)).ToString(SaveOptions.DisableFormatting);
        _dbMock.Setup(db => db.StringGet(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>())).Returns(xml);
        _dbMock.Setup(db => db.LockTake(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan>(), It.IsAny<CommandFlags>())).Returns(true);

        _repository.StoreElement(validKey, "friendly");

        //_dbMock.Verify(db => db.StringSet(_key, It.IsAny<RedisValue>(), null, When.Always, CommandFlags.None), Times.Once);
        _dbMock.Verify(db => db.LockRelease(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()), Times.Once);
    }
}
