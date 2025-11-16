using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text.Json;
using ActivityService.Models;
using ActivityService.Test.Helpers;
using Extensions.Configuration;
using Extensions.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Moq;
using Repository.Abstractions.Interfaces;
using Repository.Abstractions.Interfaces.BaseEntity;
using Repository.Abstractions.Models;
using Spred.Bus.Abstractions;
using Spred.Bus.Contracts;
using StackExchange.Redis;

namespace ActivityService.Test.Factory;

public class ActivityApiFactory : WebApplicationFactory<Program>
{
    public Mock<IGetToken> GetTokenMock { get; } = new();
    public Mock<IConnectionMultiplexer> ConnectionMultiplexerMock { get; } = new();
    public Mock<IThumbprintService> ThumbServiceMock { get; } = new();
    public Mock<IDatabase> RedisDbMock { get; } = new();
    
    public Mock<IPersistenceStore<ActivityEntity, Guid>> ActivityPersistenceMock { get; } = new();
    
    public Mock<IActivityWriter> ActivityWriterMock { get; } = new();
    
    public bool EnableTestAuth { get; set; } = true;

    private readonly string _signingInternalKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string _decryptionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
    private readonly Dictionary<RedisKey, RedisValue> _store = new();
    private readonly Dictionary<string, Dictionary<string, string>> _hashStore = new();

    protected override IHost CreateHost(IHostBuilder builder)
    {
        SetupPersistenceStoreMock<ActivityEntity, Guid, long>(
            ActivityPersistenceMock,
            () => new ActivityEntity
            {
                Id = Guid.NewGuid(),
                ActorUserId = Guid.NewGuid(),
                OtherPartyUserId = Guid.NewGuid(),
                OwnerUserId = Guid.NewGuid(),
                ObjectType = "track",
                ObjectId = Guid.NewGuid(),
                Verb = "created",
                MessageKey = "activity.track.created",
                Args = new Dictionary<string, object?>
                {
                    ["TrackName"] = "Demo Track",
                    ["Duration"] = 210,
                    ["Genre"] = "Electronic",
                    ["IsPublic"] = true
                },
                Before = JsonDocument.Parse("{\"name\":\"Old Track\"}").RootElement,
                After = JsonDocument.Parse("{\"name\":\"New Track\"}").RootElement,
                CorrelationId = Guid.NewGuid().ToString(),
                Service = "TrackService",
                Importance = ActivityImportance.Normal,
                Audience = "public",
                Sequence = 1,
                Tags = new[] { "music", "update", "demo" },
                CreatedAt = DateTimeOffset.UtcNow,
            });

        SetUpRedisDatabase();

        builder.UseEnvironment("Test");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IActivityWriter>();
            services.AddScoped<IActivityWriter>(_ => ActivityWriterMock.Object);
            
            services.RemoveAll<IGetToken>();
            services.AddSingleton(GetTokenMock.Object);

            services.RemoveAll<IConnectionMultiplexer>();
            services.AddSingleton(_ =>
            {
                ConnectionMultiplexerMock
                    .Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                    .Returns(RedisDbMock.Object);
                return ConnectionMultiplexerMock.Object;
            });

            services.RemoveAll<IThumbprintService>();
            services.AddSingleton(_ =>
            {
                ThumbServiceMock.Setup(x => x.Generate(It.IsAny<HttpContext>()))
                    .Returns("thumb");
                ThumbServiceMock.Setup(x => x.Validate(It.IsAny<HttpContext>(), It.IsAny<string>()))
                    .Returns(true);
                return ThumbServiceMock.Object;
            });

            if (EnableTestAuth)
            {
                services.AddAuthentication("TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, AuthHandlerHelper>("TestScheme", _ => { });

                services.AddAuthorization(options =>
                {
                    options.DefaultPolicy = new AuthorizationPolicyBuilder("TestScheme")
                        .RequireAuthenticatedUser()
                        .Build();

                    options.AddPolicy(JwtSpredPolicy.JwtServicePolicy, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(JwtSpredPolicy.JwtUserPolicy, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistCreate, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistDeleteOwn, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistEditOwn, p => p.RequireAssertion(_ => true));
                    options.AddPolicy(Policies.PlaylistOwnPrivateRead, p => p.RequireAssertion(_ => true));

                    options.AddPolicy("COOKIE_OR_OAUTH", p =>
                    {
                        p.AddAuthenticationSchemes("TestScheme");
                        p.RequireAuthenticatedUser();
                    });

                    options.AddPolicy(CookieAuthenticationDefaults.AuthenticationScheme, p =>
                    {
                        p.AddAuthenticationSchemes("TestScheme");
                        p.RequireAuthenticatedUser();
                    });
                });
            }

            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestScheme";
                options.DefaultChallengeScheme = "TestScheme";
            });

            services.RemoveAll<IPersistenceStore<ActivityEntity, Guid>>();
            services.AddScoped(_ => ActivityPersistenceMock.Object);
        });

        return base.CreateHost(builder);
    }

    private void SetUpRedisDatabase()
    {
        RedisDbMock.Setup(db => db.StringSetAsync(
                It.IsAny<RedisKey>(), It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(true)
            .Callback<RedisKey, RedisValue, TimeSpan?, When, CommandFlags>((key, value, _, _, _) =>
            {
                _store[key] = value;
            });

        RedisDbMock.Setup(db => db.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) =>
            {
                _store.TryGetValue(key, out var value);
                return value;
            });

        RedisDbMock.Setup(db => db.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, CommandFlags _) => _store.Remove(key));

        RedisDbMock.Setup(db => db.HashSetAsync(It.IsAny<RedisKey>(), It.IsAny<HashEntry[]>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true))
            .Callback<RedisKey, HashEntry[], CommandFlags>((key, entries, _) =>
            {
                var keyStr = key.ToString();
                if (!_hashStore.ContainsKey(keyStr))
                    _hashStore[keyStr] = new();

                foreach (var entry in entries)
                    _hashStore[keyStr][entry.Name!] = entry.Value!;
            });

        RedisDbMock.Setup(db => db.HashGetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();

                if (keyStr.Contains("signing"))
                    return _signingInternalKey;
                if (keyStr.Contains("decrypt"))
                    return _decryptionKey;

                return _hashStore.TryGetValue(keyStr, out var fields) &&
                       fields.TryGetValue(fieldStr, out var val)
                    ? (RedisValue)val
                    : RedisValue.Null;
            });

        RedisDbMock.Setup(db => db.HashExistsAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<CommandFlags>()))
            .Returns((RedisKey key, RedisValue field, CommandFlags _) =>
            {
                var keyStr = key.ToString();
                var fieldStr = field.ToString();
                var exists = _hashStore.TryGetValue(keyStr, out var fields) && fields.ContainsKey(fieldStr);
                return Task.FromResult(exists);
            });
    }

    public void SetupPersistenceStoreMock<T, TKey, TSort>(
        Mock<IPersistenceStore<T, TKey>> mock,
        Func<T> seedData
    ) where T : class, IBaseEntity<TKey>
    {
        mock.Setup(x => x.StoreAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.UpdateAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.DeleteAsync(It.IsAny<T>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PersistenceResult<bool>(true, false, null));

        mock.Setup(x => x.GetAsync(It.IsAny<TKey>(), It.IsAny<PartitionKey>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((TKey id, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                var result = Equals(((IBaseEntity<TKey>)obj).Id, id) ? obj : null;
                return new PersistenceResult<T>(result!, false, null);
            });

        mock.Setup(x => x.CountAsync(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter, PartitionKey _, CancellationToken _, bool _) =>
            {
                var obj = seedData();
                var data = new List<T> { obj };
                var compiled = filter?.Compile();
                var count = compiled != null ? data.Count(compiled) : data.Count;
                return new PersistenceResult<int>(count, false, null);
            });
        
        mock.Setup(x => x.GetAsync<TSort>(
                It.IsAny<Expression<Func<T, bool>>>(),
                It.IsAny<Expression<Func<T, TSort>>>(),
                It.IsAny<PartitionKey>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<CancellationToken>(), 
                It.IsAny<bool>()))
            .ReturnsAsync((Expression<Func<T, bool>> filter,
                Expression<Func<T, TSort>> _,
                PartitionKey _,
                int _,
                int _,
                bool _,
                CancellationToken _,
                bool _) =>
            {
                var obj = seedData();
                List<T> mockData = [obj];
                var compiled = filter?.Compile();
                var result = compiled != null ? mockData.Where(compiled).ToList() : mockData;
                return new PersistenceResult<IEnumerable<T>>(result, false, null);
            });
    }
}
